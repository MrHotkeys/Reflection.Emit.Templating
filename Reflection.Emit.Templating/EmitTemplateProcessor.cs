using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

using MrHotkeys.Reflection.Emit.Templating.Cil;
using MrHotkeys.Reflection.Emit.Templating.Cil.Instructions;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public sealed class EmitTemplateProcessor
    {
        private ILogger Logger { get; }

        private ICilParser CilParser { get; }

        private ICilWriter CilWriter { get; }

        public EmitTemplateProcessor(ILogger<EmitTemplateProcessor> logger, ICilParser cilParser, ICilWriter cilWriter)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CilParser = cilParser ?? throw new ArgumentNullException(nameof(cilParser));
            CilWriter = cilWriter ?? throw new ArgumentNullException(nameof(cilWriter));
        }

        public void Process(TypeBuilder typeBuilder, ILGenerator il, bool outIsStatic, Func<EmitTemplateSurrogate, Delegate> callback)
        {
            var template = callback(EmitTemplateSurrogate.Instance);
            var templateBody = CilParser.ParseMethodBody(template.Method);
            var tokens = new Queue<ICilToken>(templateBody.Tokens);
            var context = new Context(
                typeBuilder: typeBuilder,
                callback: callback,
                template: template,
                argumentIndexOffset: GetArgIndexOffset(template.Method.IsStatic, outIsStatic));

            if (callback.Target is not null)
                AddCaptures(callback.Target, context.Captures);
            if (template.Target is not null)
                AddCaptures(template.Target, context.Captures);

            ProcessTokens(context, tokens);

            templateBody.Tokens = context.Result;

            if (templateBody.Tokens[^1] is ICilInstruction lastInstruction && lastInstruction.InstructionType == CilInstructionType.Return)
                templateBody.Tokens.RemoveAt(templateBody.Tokens.Count - 1);

            CilWriter.Write(il, templateBody);
        }

        private void AddCaptures(object target, Dictionary<MemberInfo, object?> captures)
        {
            foreach (var member in target.GetType().GetMembers())
            {
                object? value;
                switch (member.MemberType)
                {
                    case MemberTypes.Field when member is FieldInfo field:
                        value = field.GetValue(target);
                        break;
                    case MemberTypes.Property when member is PropertyInfo property:
                        value = property.GetValue(target);
                        break;
                    default:
                        continue;
                }

                captures[member] = value;
            }
        }

        private int GetArgIndexOffset(bool templateIsStatic, bool destinationIsStatic)
        {
            if (templateIsStatic == destinationIsStatic)
                return 0;
            else if (templateIsStatic)
                return 1;
            else
                return -1;
        }

        private void ProcessTokens(Context context, Queue<ICilToken> tokens)
        {
            while (tokens.Count > 0)
            {
                var token = tokens.Dequeue();
                ProcessToken(context, tokens, token);
            }
        }

        private void ProcessToken(Context context, Queue<ICilToken> tokens, ICilToken token)
        {
            switch (token.TokenType)
            {
                case CilTokenType.Label:
                    {
                        if (token is not ICilLabel label)
                            throw new InvalidOperationException();
                        ProcessLabel(context, label);
                        break;
                    }
                case CilTokenType.Instruction:
                    {
                        if (token is not ICilInstruction instruction)
                            throw new InvalidOperationException();
                        ProcessInstruction(context, tokens, instruction);
                        break;
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        private void ProcessLabel(Context context, ICilLabel label)
        {
            context.Result.Add(label);
        }

        private void ProcessInstructions(Context context, Queue<ICilInstruction> instructions)
        {
            var tokens = new Queue<ICilToken>(instructions);
            while (tokens.Count > 0)
            {
                var instruction = (ICilInstruction)tokens.Dequeue();
                ProcessInstruction(context, tokens, instruction);
            }
            instructions.Clear();
        }

        private void ProcessInstruction(Context context, Queue<ICilToken> tokens, ICilInstruction instruction)
        {
            switch (instruction.InstructionType)
            {
                case CilInstructionType.LoadArgument when !context.Template.Method.IsStatic && instruction is CilLoadArgumentInstruction ldarg && ldarg.Index == 0:
                    {
                        ProcessCapturesAccess(context, tokens, ldarg);
                        break;
                    }
                default:
                    {
                        if (instruction.OperandType == CilOperandType.ArgumentIndex)
                        {
                            if (instruction is not IHasArgumentIndexOperand argInstruction)
                                throw new InvalidOperationException();

                            argInstruction.Index += context.ArugmentIndexOffset;
                        }

                        context.Result.Add(instruction);
                        break;
                    }
            }
        }

        private void ProcessCapturesAccess(Context context, Queue<ICilToken> tokens, CilLoadArgumentInstruction ldarg)
        {
            if (tokens.Dequeue() is not CilLoadFieldInstruction ldfld)
                throw new InvalidOperationException();

            if (ldfld.Field.FieldType == typeof(EmitTemplateSurrogate))
            {
                var surrogateInstructions = LookAheadToSurrogateMethodCall(tokens);
                var call = (CilCallInstruction)surrogateInstructions.Pop();
                var argInstructions = GroupArgInstructions(call.Method, surrogateInstructions);

                // Make sure all instructions we sent in got consumed, shouldn't have any extras since we already found the target for the call (ldfld)
                if (surrogateInstructions.Count > 0)
                    throw new InvalidOperationException();

                ProcessSurrogateMethod(context, (MethodInfo)call.Method, argInstructions);
            }
            else
            {
                // Check if we're in outer or inner capture
                if (ldfld.Field.FieldType == context.Callback.Target?.GetType())
                {
                    // Load inner capture from outer capture
                    if (tokens.Dequeue() is not CilLoadFieldInstruction ldfld_inner)
                        throw new InvalidOperationException();

                    ldfld = ldfld_inner;
                }

                if (ldfld.Field.DeclaringType != context.Callback.Target?.GetType())
                    throw new InvalidOperationException();

                var capture = context.Captures[ldfld.Field];
                ICilInstruction instruction = capture switch
                {
                    null => new CilRawInstruction(OpCodes.Ldnull),
                    bool b => new CilRawInstruction(b ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                    sbyte sb => new CilRawInstruction(OpCodes.Ldc_I4_S, sb),
                    byte b => new CilRawInstruction(OpCodes.Ldc_I4, b),
                    short s => new CilRawInstruction(OpCodes.Ldc_I4, s),
                    ushort us => new CilRawInstruction(OpCodes.Ldc_I4, us),
                    int i => new CilRawInstruction(OpCodes.Ldc_I4, i),
                    uint ui => new CilRawInstruction(OpCodes.Ldc_I4, ui),
                    long l => new CilRawInstruction(OpCodes.Ldc_I8, l),
                    ulong ul => new CilRawInstruction(OpCodes.Ldc_I8, ul),
                    float f => new CilRawInstruction(OpCodes.Ldc_R4, f),
                    double d => new CilRawInstruction(OpCodes.Ldc_R8, d),
                    string s => new CilRawInstruction(OpCodes.Ldstr, s),
                    _ => throw new NotSupportedException($"Captured value \"{ldfld.Field.Name}\" has unsupported non-primitive type {capture.GetType()}!"),
                };
                context.Result.Add(instruction);
            }
        }

        private void ProcessSurrogateMethod(Context context, MethodInfo surrogateMethod, Queue<Queue<ICilInstruction>> argsInstructions)
        {
            switch (surrogateMethod.Name)
            {
                case nameof(EmitTemplateSurrogate.Get):
                    ProcessSurrogateGetMethod(context, surrogateMethod, argsInstructions);
                    break;
                case nameof(EmitTemplateSurrogate.Set):
                    ProcessSurrogateSetMethod(context, surrogateMethod, argsInstructions);
                    break;
                case nameof(EmitTemplateSurrogate.Ref):
                    ProcessSurrogateRefMethod(context, surrogateMethod, argsInstructions);
                    break;
                case nameof(EmitTemplateSurrogate.Call):
                    ProcessSurrogateCallMethod(context, surrogateMethod, argsInstructions);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void ProcessSurrogateGetMethod(Context context, MethodInfo surrogateMethod, Queue<Queue<ICilInstruction>> argsInstructions)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length != 1)
                throw new InvalidOperationException();

            if (argsInstructions.Count != parameters.Length)
                throw new InvalidOperationException();

            var capture = GetCaptureArg(context, argsInstructions.Dequeue());
            if (capture is null)
                throw new InvalidOperationException();

            var variableType = parameters[0].ParameterType;
            if (variableType == typeof(FieldInfo))
                ProcessSurrogateGetFieldMethod(context, (FieldInfo)capture);
            else if (variableType == typeof(PropertyInfo))
                ProcessSurrogateGetProperty(context, (PropertyInfo)capture);
            else if (variableType == typeof(LocalVariableInfo))
                ProcessSurrogateGetLocalMethod(context, (LocalVariableInfo)capture);
            else
                throw new InvalidOperationException();
        }

        private void ProcessSurrogateGetFieldMethod(Context context, FieldInfo field)
        {
            if (!field.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            context.Result.Add(new CilLoadFieldInstruction(field));
        }

        private void ProcessSurrogateGetProperty(Context context, PropertyInfo property)
        {
            if (!property.GetMethod.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            context.Result.Add(new CilCallInstruction(property.GetMethod));
        }

        private void ProcessSurrogateGetLocalMethod(Context context, LocalVariableInfo local)
        {
            var ldloc = local.LocalIndex switch
            {
                0 => new CilRawInstruction(OpCodes.Ldloc_0),
                1 => new CilRawInstruction(OpCodes.Ldloc_1),
                2 => new CilRawInstruction(OpCodes.Ldloc_2),
                3 => new CilRawInstruction(OpCodes.Ldloc_3),
                _ when local.LocalIndex >= byte.MinValue && local.LocalIndex <= byte.MaxValue => new CilRawInstruction(OpCodes.Ldloc_S, local.LocalIndex),
                _ => new CilRawInstruction(OpCodes.Ldloc, local.LocalIndex),
            };

            context.Result.Add(ldloc);
        }

        private void ProcessSurrogateSetMethod(Context context, MethodInfo surrogateMethod, Queue<Queue<ICilInstruction>> argsInstructions)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length != 2)
                throw new InvalidOperationException();

            if (argsInstructions.Count != parameters.Length)
                throw new InvalidOperationException();

            var capture = GetCaptureArg(context, argsInstructions.Dequeue());
            if (capture is null)
                throw new InvalidOperationException();

            var valueInstructions = argsInstructions.Dequeue();
            var variableType = parameters[0].ParameterType;
            if (variableType == typeof(FieldInfo))
                ProcessSurrogateSetFieldMethod(context, (FieldInfo)capture, valueInstructions);
            else if (variableType == typeof(PropertyInfo))
                ProcessSurrogateSetPropertyMethod(context, (PropertyInfo)capture, valueInstructions);
            else if (variableType == typeof(LocalVariableInfo))
                ProcessSurrogateSetLocalMethod(context, (LocalVariableInfo)capture, valueInstructions);
            else
                throw new InvalidOperationException();
        }

        private void ProcessSurrogateSetFieldMethod(Context context, FieldInfo field, Queue<ICilInstruction> valueInstructions)
        {
            if (!field.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            ProcessInstructions(context, valueInstructions);

            context.Result.Add(new CilStoreFieldInstruction(field));
        }

        private void ProcessSurrogateSetPropertyMethod(Context context, PropertyInfo property, Queue<ICilInstruction> valueInstructions)
        {
            if (!property.SetMethod.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            ProcessInstructions(context, valueInstructions);

            context.Result.Add(new CilCallInstruction(property.SetMethod));
        }

        private void ProcessSurrogateSetLocalMethod(Context context, LocalVariableInfo local, Queue<ICilInstruction> valueInstructions)
        {
            ProcessInstructions(context, valueInstructions);

            var stloc = local.LocalIndex switch
            {
                0 => new CilRawInstruction(OpCodes.Stloc_0),
                1 => new CilRawInstruction(OpCodes.Stloc_1),
                2 => new CilRawInstruction(OpCodes.Stloc_2),
                3 => new CilRawInstruction(OpCodes.Stloc_3),
                _ when local.LocalIndex >= byte.MinValue && local.LocalIndex <= byte.MaxValue => new CilRawInstruction(OpCodes.Stloc_S, local.LocalIndex),
                _ => new CilRawInstruction(OpCodes.Stloc, local.LocalIndex),
            };

            context.Result.Add(stloc);
        }

        private void ProcessSurrogateRefMethod(Context context, MethodInfo surrogateMethod, Queue<Queue<ICilInstruction>> argsInstructions)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length != 1)
                throw new InvalidOperationException();

            if (argsInstructions.Count != parameters.Length)
                throw new InvalidOperationException();

            var capture = GetCaptureArg(context, argsInstructions.Dequeue());
            if (capture is null)
                throw new InvalidOperationException();

            var variableType = parameters[0].ParameterType;
            if (variableType == typeof(FieldInfo))
                ProcessSurrogateRefFieldMethod(context, (FieldInfo)capture);
            else if (variableType == typeof(LocalVariableInfo))
                ProcessSurrogateRefLocalMethod(context, (LocalVariableInfo)capture);
            else
                throw new InvalidOperationException();
        }

        private void ProcessSurrogateRefFieldMethod(Context context, FieldInfo field)
        {
            if (!field.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            context.Result.Add(new CilRawInstruction(OpCodes.Ldflda, field));
        }

        private void ProcessSurrogateRefLocalMethod(Context context, LocalVariableInfo local)
        {
            var ldloca = local.LocalIndex >= byte.MinValue && local.LocalIndex <= byte.MaxValue ?
                new CilRawInstruction(OpCodes.Ldloca_S, (byte)local.LocalIndex) :
                new CilRawInstruction(OpCodes.Ldloca, (ushort)local.LocalIndex);

            context.Result.Add(ldloca);
        }

        private void ProcessSurrogateCallMethod(Context context, MethodInfo surrogateMethod, Queue<Queue<ICilInstruction>> argsInstructions)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length < 1)
                throw new InvalidOperationException();

            if (argsInstructions.Count != parameters.Length)
                throw new InvalidOperationException();

            var methodSourceType = parameters[0].ParameterType;
            if (methodSourceType == typeof(MethodInfo))
            {
                var method = (MethodInfo)GetCaptureArg(context, argsInstructions.Dequeue())!;

                if (!method.IsStatic)
                    context.Result.Add(new CilLoadArgumentInstruction(0));

                ProcessArgsInstructions(context, argsInstructions);

                context.Result.Add(new CilCallInstruction(method));
            }
            else if (methodSourceType == typeof(Delegate) || methodSourceType.BaseType == typeof(MulticastDelegate))
            {
                var methodInstructions = new Stack<ICilInstruction>(argsInstructions.Dequeue());

                if (methodInstructions.Pop() is not CilRawInstruction delegateNew || delegateNew.OpCode != OpCodes.Newobj)
                    throw new InvalidOperationException();
                if (delegateNew.Operand is not ConstructorInfo delegateCtor || !typeof(Delegate).IsAssignableFrom(delegateCtor.DeclaringType))
                    throw new InvalidOperationException();

                var ctorInstructions = GroupArgInstructions(delegateCtor, methodInstructions);

                var targetInstructions = ctorInstructions.Dequeue();
                if (targetInstructions.Peek() is not CilRawInstruction ldnull || ldnull.OpCode != OpCodes.Ldnull)
                    ProcessInstructions(context, targetInstructions);

                var methodPointerInstructions = ctorInstructions.Dequeue();
                if (methodPointerInstructions.Dequeue() is not CilRawInstruction ldftn || ldftn.OpCode != OpCodes.Ldftn)
                    throw new InvalidOperationException();
                if (ldftn.Operand is not MethodBase method)
                    throw new InvalidOperationException();

                if (ctorInstructions.Count > 0)
                    throw new InvalidOperationException();

                ProcessArgsInstructions(context, argsInstructions);

                context.Result.Add(new CilCallInstruction(method));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private Stack<ICilInstruction> LookAheadToSurrogateMethodCall(Queue<ICilToken> tokens)
        {
            var stack = new Stack<ICilInstruction>();
            var depth = 0;
            while (tokens.Count > 0)
            {
                if (tokens.Dequeue() is not ICilInstruction instruction)
                    throw new InvalidOperationException();

                stack.Push(instruction);

                switch (instruction.InstructionType)
                {
                    case CilInstructionType.LoadField when instruction is CilLoadFieldInstruction ldfld && ldfld.Field.FieldType == typeof(EmitTemplateSurrogate):
                        {
                            depth++;

                            break;
                        }
                    case CilInstructionType.Call when instruction is CilCallInstruction call && call.Method.DeclaringType == typeof(EmitTemplateSurrogate):
                        {
                            if (depth == 0)
                                return stack;

                            depth--;

                            break;
                        }
                    default:
                        {
                            // TODO: Follow each branch and we're okay if we make it back to this branch instruction or the same next call or ldfld at same depth?
                            if (IsJump(instruction))
                                throw new InvalidOperationException();

                            break;
                        }
                }
            }

            // If we haven't returned yet then we haven't found a paired call instruction
            throw new InvalidOperationException();
        }

        private Queue<Queue<ICilInstruction>> GroupArgInstructions(MethodBase method, Stack<ICilInstruction> argInstructions)
        {
            var parameters = method.GetParameters();

            var parameterInstructions = new List<List<ICilInstruction>>(parameters.Length);
            for (var i = 0; i < parameters.Length; i++)
                parameterInstructions.Add(new List<ICilInstruction>());

            var parameterIndex = parameters.Length - 1;
            var stack = -1;
            while (argInstructions.Count > 0)
            {
                if (argInstructions.Pop() is not ICilInstruction instruction)
                    throw new InvalidOperationException();

                parameterInstructions[parameterIndex].Add(instruction);

                // TODO: Hack to ignore stloc->ldloc or stloc->ldloca chains confusing the depth search
                // Ignore a ldloc or ldloca if that local was written to in the instruction immediately previous
                if (argInstructions.Count != 0 && instruction.InstructionType == CilInstructionType.LoadLocal || instruction.InstructionType == CilInstructionType.LoadLocalAddress)
                {
                    if (instruction is not IHasLocalOperand hasLocal)
                        throw new InvalidOperationException();

                    if (argInstructions.Peek() is ICilInstruction peek && peek.InstructionType == CilInstructionType.StoreLocal)
                    {
                        if (peek is not CilStoreLocalInstruction stloc)
                            throw new InvalidOperationException();

                        if (hasLocal.Local == stloc.Local)
                        {
                            parameterInstructions[parameterIndex].Add(argInstructions.Pop());
                            continue;
                        }
                    }
                }

                if (IsJump(instruction))
                    throw new InvalidOperationException();

                var pop = GetPopOffset(instruction);
                var push = GetPushOffset(instruction);

                stack = stack - pop + push;

                if (stack == 0)
                {
                    parameterIndex--;
                    stack = -1;

                    if (parameterIndex < 0)
                        break;
                }
            }

            if (parameterIndex >= 0)
                throw new InvalidOperationException();

            var result = parameterInstructions
                .Select(i => i
                    .AsEnumerable()
                    .Reverse())
                .Select(i => new Queue<ICilInstruction>(i));

            return new Queue<Queue<ICilInstruction>>(result);
        }

        private int GetPopOffset(ICilInstruction instruction)
        {
            return instruction.StackBehaviourPop switch
            {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 => 1,
                StackBehaviour.Pop1_pop1 => 2,
                StackBehaviour.Popi => 1,
                StackBehaviour.Popi_pop1 => 2,
                StackBehaviour.Popi_popi => 2,
                StackBehaviour.Popi_popi8 => 2,
                StackBehaviour.Popi_popi_popi => 3,
                StackBehaviour.Popi_popr4 => 2,
                StackBehaviour.Popi_popr8 => 3,
                StackBehaviour.Popref => 1,
                StackBehaviour.Popref_pop1 => 2,
                StackBehaviour.Popref_popi_pop1 => 3,
                StackBehaviour.Popref_popi_popi => 3,
                StackBehaviour.Popref_popi_popi8 => 3,
                StackBehaviour.Popref_popi_popr4 => 3,
                StackBehaviour.Popref_popi_popr8 => 3,
                StackBehaviour.Popref_popi_popref => 3,
                StackBehaviour.Varpop when instruction is CilCallInstruction call => call.Method.GetParameters().Length + (call.Method.IsStatic ? 0 : 1),
                StackBehaviour.Varpop when instruction is CilRawInstruction raw && raw.OpCode == OpCodes.Newobj => ((MethodBase)raw.Operand!).GetParameters().Length,
                _ => throw new InvalidOperationException(),
            };
        }

        private int GetPushOffset(ICilInstruction instruction)
        {
            return instruction.StackBehaviourPush switch
            {
                StackBehaviour.Push0 => 0,
                StackBehaviour.Push1 => 1,
                StackBehaviour.Push1_push1 => 2,
                StackBehaviour.Pushi => 1,
                StackBehaviour.Pushi8 => 1,
                StackBehaviour.Pushr4 => 1,
                StackBehaviour.Pushr8 => 1,
                StackBehaviour.Pushref => 1,
                StackBehaviour.Varpush when instruction is CilCallInstruction call && call.Method is MethodInfo method => method.ReturnType is null ? 0 : 1,
                _ => throw new InvalidOperationException(),
            };
        }

        private bool IsJump(ICilInstruction instruction)
        {
            if (instruction.InstructionType == CilInstructionType.Branch)
                return true;

            if (instruction.InstructionType == CilInstructionType.Raw)
            {
                if (instruction is not CilRawInstruction raw)
                    throw new InvalidOperationException();

                return raw.OpCode.FlowControl switch
                {
                    FlowControl.Next or
                    FlowControl.Call => false,
                    _ => true,
                };
            }

            return false;
        }

        private object? GetCaptureArg(Context context, Queue<ICilInstruction> argInstructions)
        {
            if (argInstructions.Dequeue() is not CilLoadArgumentInstruction ldarg || ldarg.Index != 0)
                throw new InvalidOperationException();
            if (argInstructions.Dequeue() is not CilLoadFieldInstruction ldfld)
                throw new InvalidOperationException();

            // Check if we're in outer or inner capture
            if (ldfld.Field.FieldType == context.Callback.Target?.GetType())
            {
                if (argInstructions.Dequeue() is not CilLoadFieldInstruction ldfld_inner)
                    throw new InvalidOperationException();

                ldfld = ldfld_inner;
            }

            if (ldfld.Field.DeclaringType != context.Callback.Target?.GetType())
                throw new InvalidOperationException();

            if (argInstructions.Count > 0)
                throw new InvalidOperationException();

            return context.Captures[ldfld.Field];
        }

        private void ProcessArgsInstructions(Context context, Queue<Queue<ICilInstruction>> argsInstructions)
        {
            foreach (var queue in argsInstructions)
                ProcessInstructions(context, queue);
        }

        private sealed class Context
        {
            public TypeBuilder TypeBuilder { get; }

            public Delegate Callback { get; }

            public Delegate Template { get; }
            public int ArugmentIndexOffset { get; }

            public Dictionary<MemberInfo, object?> Captures { get; } = new();

            public List<ICilToken> Result { get; } = new();

            public Context(TypeBuilder typeBuilder, Delegate callback, Delegate template, int argumentIndexOffset)
            {
                TypeBuilder = typeBuilder ?? throw new ArgumentNullException(nameof(typeBuilder));
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
                Template = template ?? throw new ArgumentNullException(nameof(template));
                ArugmentIndexOffset = argumentIndexOffset;
            }
        }
    }
}