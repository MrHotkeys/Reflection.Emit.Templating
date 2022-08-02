using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

using MrHotkeys.Reflection.Emit.Templating.Cil;
using MrHotkeys.Reflection.Emit.Templating.Cil.Instructions;
using MrHotkeys.Reflection.Emit.Templating.Extensions;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public sealed class EmitTemplateProcessor
    {
        private static Dictionary<string, object> StaticCaptures { get; } = new Dictionary<string, object>();

        private static object StaticCapturesLock { get; } = new object();

        private ILogger Logger { get; }

        private ICilParser CilParser { get; }

        private ICilWriter CilWriter { get; }

        public EmitTemplateProcessor(ILogger<EmitTemplateProcessor> logger, ICilParser cilParser, ICilWriter cilWriter)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CilParser = cilParser ?? throw new ArgumentNullException(nameof(cilParser));
            CilWriter = cilWriter ?? throw new ArgumentNullException(nameof(cilWriter));
        }

        public static object GetStaticCapture(string key)
        {
            lock (StaticCapturesLock)
            {
                var captureTarget = StaticCaptures[key];
                StaticCaptures.Remove(key);
                return captureTarget;
            }
        }

        public void Process(TypeBuilder typeBuilder, ILGenerator il, bool outIsStatic, Func<EmitTemplateSurrogate, Delegate> callback)
        {
            var template = callback(EmitTemplateSurrogate.Instance);
            var templateBody = CilParser.ParseMethodBody(template.Method);

            var tokens = new ReadOnlyStreamSpan<ICilToken>(templateBody.Tokens.ToArray());

            var context = new Context(
                typeBuilder: typeBuilder,
                callback: callback,
                template: template,
                argumentIndexOffset: GetArgIndexOffset(template.Method.IsStatic, outIsStatic)
            );

            ProcessTokens(context, ref tokens);

            templateBody.Tokens = context.Result;
            foreach (var local in context.Locals)
                templateBody.Locals.Add(local);

            if (templateBody.Tokens[^1] is ICilInstruction lastInstruction && lastInstruction.InstructionType == CilInstructionType.Return)
                templateBody.Tokens.RemoveAt(templateBody.Tokens.Count - 1);

            CilWriter.Write(il, templateBody);
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

        private void ProcessTokens(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens)
        {
            while (tokens.Length > 0)
            {
                var token = tokens.Take();
                ProcessToken(context, ref tokens, token);
            }
        }

        private void ProcessToken(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, ICilToken token)
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
                        ProcessInstruction(context, ref tokens, instruction);
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

        private void ProcessInstruction(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, ICilInstruction instruction)
        {
            switch (instruction.InstructionType)
            {
                case CilInstructionType.LoadArgument when !context.Template.Method.IsStatic && instruction is CilLoadArgumentInstruction ldarg && ldarg.Index == 0:
                    {
                        ProcessTargetAccess(context, ref tokens);
                        break;
                    }

                case CilInstructionType.LoadStaticField when instruction is CilLoadStaticFieldInstruction ldsfld &&
                        (context.CallbackTargetFields.Contains(ldsfld.Field) || context.TemplateTargetFields.Contains(ldsfld.Field)):
                    {
                        ProcessLoadStaticField(context, ref tokens, ldsfld);
                        break;
                    }

                case CilInstructionType.Call when instruction is CilCallInstruction call && call.Method.IsStatic &&
                        (context.CallbackTargetPropertyGetters.Contains(call.Method) || context.TemplateTargetPropertyGetters.Contains(call.Method)):
                    {
                        ProcessCallStaticPropertyGetter(context, ref tokens, call);
                        break;
                    }

                case CilInstructionType.StoreField when instruction is CilStoreFieldInstruction stfld &&
                        (context.CallbackTargetFields.Contains(stfld.Field) || context.TemplateTargetFields.Contains(stfld.Field)):
                    throw new InvalidOperationException();

                case CilInstructionType.StoreStaticField when instruction is CilStoreStaticFieldInstruction stsfld &&
                        (context.CallbackTargetFields.Contains(stsfld.Field) || context.TemplateTargetFields.Contains(stsfld.Field)):
                    throw new InvalidOperationException();

                case CilInstructionType.Call when instruction is CilCallInstruction call &&
                        (context.CallbackTargetPropertySetters.Contains(call.Method) || context.TemplateTargetPropertySetters.Contains(call.Method)):
                    throw new InvalidOperationException();

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

        private void ProcessTargetAccess(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens)
        {
            switch (tokens.Take())
            {
                case CilLoadFieldInstruction ldfld when ldfld.Field.FieldType == typeof(EmitTemplateSurrogate):
                    {
                        var (surrogateMethod, argsTokenCount) = LookAheadToSurrogateMethodCall(tokens);
                        var argsTokens = tokens.Slice(0, argsTokenCount);
                        var argRanges = GetArgSliceInfos(surrogateMethod, argsTokens);

                        ProcessSurrogateMethod(context, surrogateMethod, argsTokens, argRanges);

                        // Move for every token plus the call at the end
                        tokens.Move(argsTokenCount + 1);

                        break;
                    }

                case CilLoadFieldInstruction ldfld when ldfld.Field.DeclaringType == context.TemplateTargetType:
                    {
                        if (ldfld.Field.FieldType == context.CallbackTargetType)
                            ProcessTargetAccess(context, ref tokens);
                        else
                            ProcessLoadFieldFromTarget(context, ref tokens, ldfld, context.Template.Target);
                        break;
                    }

                case CilLoadFieldInstruction ldfld when ldfld.Field.DeclaringType == context.CallbackTargetType:
                    {
                        ProcessLoadFieldFromTarget(context, ref tokens, ldfld, context.Callback.Target);
                        break;
                    }

                case CilLoadFieldAddressInstruction ldflda when ldflda.Field.DeclaringType == context.TemplateTargetType:
                    {
                        if (ldflda.Field.FieldType == context.CallbackTargetType)
                            ProcessTargetAccess(context, ref tokens);
                        else
                            ProcessLoadFieldAddressFromTarget(context, ref tokens, ldflda, context.Template.Target);
                        break;
                    }

                case CilLoadFieldAddressInstruction ldflda when ldflda.Field.DeclaringType == context.CallbackTargetType:
                    {
                        ProcessLoadFieldAddressFromTarget(context, ref tokens, ldflda, context.Callback.Target);
                        break;
                    }

                case CilCallInstruction call when context.CallbackTargetPropertyGetters.Contains(call.Method):
                    {
                        ProcessCallPropertyGetterOnTarget(context, ref tokens, call, context.Callback.Target);
                        break;
                    }

                case CilCallInstruction call when context.TemplateTargetPropertyGetters.Contains(call.Method):
                    {
                        ProcessCallPropertyGetterOnTarget(context, ref tokens, call, context.Template.Target);
                        break;
                    }

                default:
                    throw new InvalidOperationException();
            }
        }

        private void ProcessSurrogateMethod(Context context, MethodInfo surrogateMethod, ReadOnlyStreamSpan<ICilToken> argsTokens, Range[] argRanges)
        {
            switch (surrogateMethod.Name)
            {
                case nameof(EmitTemplateSurrogate.Get):
                    ProcessSurrogateGetMethod(context, surrogateMethod, argsTokens, argRanges);
                    break;
                case nameof(EmitTemplateSurrogate.Set):
                    ProcessSurrogateSetMethod(context, surrogateMethod, argsTokens, argRanges);
                    break;
                case nameof(EmitTemplateSurrogate.Ref):
                    ProcessSurrogateRefMethod(context, surrogateMethod, argsTokens, argRanges);
                    break;
                case nameof(EmitTemplateSurrogate.Call):
                    ProcessSurrogateCallMethod(context, surrogateMethod, argsTokens, argRanges);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void ProcessSurrogateGetMethod(Context context, MethodInfo surrogateMethod, ReadOnlyStreamSpan<ICilToken> argsTokens, Range[] argRanges)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length != 1)
                throw new InvalidOperationException();

            if (argRanges.Length != parameters.Length)
                throw new InvalidOperationException();

            var captureSlice = argsTokens.Slice(argRanges[0]);
            var capture = GetArgValue(context, ref captureSlice);
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

        private void ProcessSurrogateSetMethod(Context context, MethodInfo surrogateMethod, ReadOnlyStreamSpan<ICilToken> argsTokens, Range[] argRanges)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length != 2)
                throw new InvalidOperationException();

            if (argRanges.Length != parameters.Length)
                throw new InvalidOperationException();

            var captureSlice = argsTokens.Slice(argRanges[0]);
            var capture = GetArgValue(context, ref captureSlice);
            if (capture is null)
                throw new InvalidOperationException();

            var valueSlice = argsTokens.Slice(argRanges[1]);
            var variableType = parameters[0].ParameterType;
            if (variableType == typeof(FieldInfo))
                ProcessSurrogateSetFieldMethod(context, (FieldInfo)capture, ref valueSlice);
            else if (variableType == typeof(PropertyInfo))
                ProcessSurrogateSetPropertyMethod(context, (PropertyInfo)capture, ref valueSlice);
            else if (variableType == typeof(LocalVariableInfo))
                ProcessSurrogateSetLocalMethod(context, (LocalVariableInfo)capture, ref valueSlice);
            else
                throw new InvalidOperationException();
        }

        private void ProcessSurrogateSetFieldMethod(Context context, FieldInfo field, ref ReadOnlyStreamSpan<ICilToken> valueTokens)
        {
            if (!field.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            ProcessTokens(context, ref valueTokens);

            context.Result.Add(new CilStoreFieldInstruction(field));
        }

        private void ProcessSurrogateSetPropertyMethod(Context context, PropertyInfo property, ref ReadOnlyStreamSpan<ICilToken> valueTokens)
        {
            if (!property.SetMethod.IsStatic)
                context.Result.Add(new CilLoadArgumentInstruction(0));

            ProcessTokens(context, ref valueTokens);

            context.Result.Add(new CilCallInstruction(property.SetMethod));
        }

        private void ProcessSurrogateSetLocalMethod(Context context, LocalVariableInfo local, ref ReadOnlyStreamSpan<ICilToken> valueTokens)
        {
            ProcessTokens(context, ref valueTokens);

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

        private void ProcessSurrogateRefMethod(Context context, MethodInfo surrogateMethod, ReadOnlyStreamSpan<ICilToken> argsTokens, Range[] argRanges)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length != 1)
                throw new InvalidOperationException();

            if (argRanges.Length != parameters.Length)
                throw new InvalidOperationException();

            var captureSlice = argsTokens.Slice(argRanges[0]);
            var capture = GetArgValue(context, ref captureSlice);
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

        private void ProcessSurrogateCallMethod(Context context, MethodInfo surrogateMethod, ReadOnlyStreamSpan<ICilToken> argsTokens, Range[] argRanges)
        {
            var parameters = surrogateMethod.GetParameters();
            if (parameters.Length < 1)
                throw new InvalidOperationException();

            if (argRanges.Length != parameters.Length)
                throw new InvalidOperationException();


            var methodSourceType = parameters[0].ParameterType;
            if (methodSourceType == typeof(MethodInfo))
            {
                var captureSlice = argsTokens.Slice(argRanges[0]);
                var method = (MethodInfo?)GetArgValue(context, ref captureSlice);
                if (method is null)
                    throw new InvalidOperationException();

                if (!method.IsStatic)
                    context.Result.Add(new CilLoadArgumentInstruction(0));

                ProcessArgsInstructions(context, argsTokens, argRanges.AsSpan().Slice(1));

                context.Result.Add(new CilCallInstruction(method));
            }
            else if (methodSourceType == typeof(Delegate) || methodSourceType.BaseType == typeof(MulticastDelegate))
            {
                var methodSlice = argsTokens.Slice(argRanges[0]);

                if (methodSlice[^1] is not CilRawInstruction delegateNew || delegateNew.OpCode != OpCodes.Newobj)
                    throw new InvalidOperationException();
                if (delegateNew.Operand is not ConstructorInfo delegateCtor || !typeof(Delegate).IsAssignableFrom(delegateCtor.DeclaringType))
                    throw new InvalidOperationException();

                var delegateCtorArgTokens = methodSlice[..^1];
                var delegateCtorArgRanges = GetArgSliceInfos(delegateCtor, delegateCtorArgTokens);

                if (delegateCtorArgRanges.Length != 2)
                    throw new InvalidOperationException();

                var delegateCtorTargetTokens = delegateCtorArgTokens.Slice(delegateCtorArgRanges[0]);
                if (delegateCtorTargetTokens[0] is not CilRawInstruction ldnull || ldnull.OpCode != OpCodes.Ldnull)
                    ProcessTokens(context, ref delegateCtorTargetTokens);

                var delegateMethodPointerInstructions = delegateCtorArgTokens.Slice(delegateCtorArgRanges[1]);
                if (delegateMethodPointerInstructions[0] is not CilRawInstruction ldftn || ldftn.OpCode != OpCodes.Ldftn)
                    throw new InvalidOperationException();
                if (ldftn.Operand is not MethodBase method)
                    throw new InvalidOperationException();

                ProcessArgsInstructions(context, argsTokens, argRanges.AsSpan().Slice(1));

                context.Result.Add(new CilCallInstruction(method));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private (MethodInfo, int) LookAheadToSurrogateMethodCall(ReadOnlyStreamSpan<ICilToken> tokens)
        {
            var depth = 0;
            var sliceLength = 0;
            for (var i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] is not ICilInstruction instruction)
                    throw new InvalidOperationException();

                sliceLength++;

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
                            {
                                if (call.Method is not MethodInfo method)
                                    throw new InvalidOperationException();

                                return (method, sliceLength - 1);
                            }

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

        private Range[] GetArgSliceInfos(MethodBase method, ReadOnlyStreamSpan<ICilToken> argsTokens)
        {
            var parameters = method.GetParameters();

            var argRanges = new Range[parameters.Length];
            var argRangesIndex = argRanges.Length - 1; // Fill from right to left

            var stack = -1;
            var sliceLength = 0;
            var tokensScanned = 0;
            for (var i = argsTokens.Length - 1; i >= 0; i--)
            {
                if (argsTokens[i] is not ICilInstruction instruction)
                    throw new InvalidOperationException();

                tokensScanned++;
                sliceLength++;

                // TODO: Hack to ignore stloc->ldloc or stloc->ldloca chains confusing the depth search
                // Ignore a ldloc or ldloca if that local was written to in the instruction immediately previous
                if (i > 0 && instruction.InstructionType == CilInstructionType.LoadLocal || instruction.InstructionType == CilInstructionType.LoadLocalAddress)
                {
                    if (instruction is not IHasLocalOperand hasLocal)
                        throw new InvalidOperationException();

                    if (argsTokens[i - 1] is ICilInstruction peek && peek.InstructionType == CilInstructionType.StoreLocal)
                    {
                        if (peek is not CilStoreLocalInstruction stloc)
                            throw new InvalidOperationException();

                        if (hasLocal.Local == stloc.Local)
                        {
                            // Skip rest of this instruction and next
                            tokensScanned++;
                            sliceLength++;
                            i--;
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
                    // We have found a complete argument
                    argRanges[argRangesIndex] = new Range(i, i + sliceLength);

                    stack = -1;
                    sliceLength = 0;
                    argRangesIndex--;

                    if (argRangesIndex < 0)
                        break;
                }
            }

            if (argRangesIndex >= 0)
                throw new InvalidOperationException();

            if (tokensScanned != argsTokens.Length)
                throw new InvalidOperationException();

            return argRanges;
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

        private object? GetLoadFieldFromTargetValue(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilLoadFieldInstruction ldfld, object target)
        {
            if (!context.CaptureValues.TryGetValue(ldfld.Field, out var value))
            {
                value = ldfld.Field.GetValue(target);
                context.CaptureValues.Add(ldfld.Field, value);
            }

            return value;
        }

        private void ProcessLoadFieldFromTarget(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilLoadFieldInstruction ldfld, object target)
        {
            var value = GetLoadFieldFromTargetValue(context, ref tokens, ldfld, target);
            ProcessTargetValue(context, ldfld.Field, value);
        }

        private object? GetLoadFieldAddressFromTargetValue(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilLoadFieldAddressInstruction ldflda, object target)
        {
            if (!context.CaptureValues.TryGetValue(ldflda.Field, out var value))
            {
                value = ldflda.Field.GetValue(target);
                context.CaptureValues.Add(ldflda.Field, value);
            }

            return value;
        }

        private void ProcessLoadFieldAddressFromTarget(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilLoadFieldAddressInstruction ldflda, object target)
        {
            var value = GetLoadFieldAddressFromTargetValue(context, ref tokens, ldflda, target);
            ProcessTargetValue(context, ldflda.Field, value);

            // We need to store in a local and load that local's address to mimic the ldflda
            var local = new CilLocalVariable(ldflda.Field.FieldType, "");
            context.Locals.Add(local);
            context.Result.Add(new CilStoreLocalInstruction(local));
            context.Result.Add(new CilLoadLocalAddressInstruction(local));
        }

        private object? GetLoadStaticFieldValue(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilLoadStaticFieldInstruction ldsfld)
        {
            if (!context.CaptureValues.TryGetValue(ldsfld.Field, out var value))
            {
                value = ldsfld.Field.GetValue(null);
                context.CaptureValues.Add(ldsfld.Field, value);
            }

            return value;
        }

        private void ProcessLoadStaticField(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilLoadStaticFieldInstruction ldsfld)
        {
            var value = GetLoadStaticFieldValue(context, ref tokens, ldsfld);
            ProcessTargetValue(context, ldsfld.Field, value);
        }

        private void ProcessCallStaticPropertyGetter(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilCallInstruction call)
        {
            var value = GetCallStaticPropertyGetterValue(context, ref tokens, call);
            ProcessTargetValue(context, call.Method, value);
        }

        private object? GetCallStaticPropertyGetterValue(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilCallInstruction call)
        {
            if (!context.CaptureValues.TryGetValue(call.Method, out var value))
            {
                if (call.Method.GetParameters().Length > 0)
                    throw new InvalidOperationException();

                value = call.Method.Invoke(null, Array.Empty<object>());
                context.CaptureValues.Add(call.Method, value);
            }

            return value;
        }

        private object? GetCallPropertyGetterOnTargetValue(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilCallInstruction call, object target)
        {
            if (!context.CaptureValues.TryGetValue(call.Method, out var value))
            {
                if (call.Method.GetParameters().Length > 0)
                    throw new InvalidOperationException();

                value = call.Method.Invoke(target, Array.Empty<object>());
                context.CaptureValues.Add(call.Method, value);
            }

            return value;
        }

        private void ProcessCallPropertyGetterOnTarget(Context context, ref ReadOnlyStreamSpan<ICilToken> tokens, CilCallInstruction call, object target)
        {
            var value = GetCallPropertyGetterOnTargetValue(context, ref tokens, call, target);
            ProcessTargetValue(context, call.Method, value);
        }

        private object? GetArgValue(Context context, ref ReadOnlyStreamSpan<ICilToken> argTokens)
        {
            if (argTokens.Take() is not ICilInstruction instruction)
                throw new InvalidOperationException();

            if (instruction is CilLoadArgumentInstruction ldarg)
            {
                if (ldarg.Index != 0)
                    throw new InvalidOperationException();

                return GetArgValueFromTarget(context, ref argTokens);
            }
            else
            {
                return GetArgValueFromStatic(context, ref argTokens);
            }
        }

        private object? GetArgValueFromTarget(Context context, ref ReadOnlyStreamSpan<ICilToken> argTokens)
        {
            if (argTokens.Take() is not ICilInstruction instruction)
                throw new InvalidOperationException();

            var value = instruction switch
            {
                CilLoadFieldInstruction ldfld when ldfld.Field.DeclaringType == context.TemplateTargetType =>
                    ldfld.Field.FieldType == context.CallbackTargetType ?
                    GetArgValueFromTarget(context, ref argTokens) :
                    GetLoadFieldFromTargetValue(context, ref argTokens, ldfld, context.Template.Target),

                CilLoadFieldInstruction ldfld when ldfld.Field.DeclaringType == context.CallbackTargetType =>
                    GetLoadFieldFromTargetValue(context, ref argTokens, ldfld, context.Callback.Target),

                CilCallInstruction call when context.CallbackTargetPropertyGetters.Contains(call.Method) =>
                    GetCallPropertyGetterOnTargetValue(context, ref argTokens, call, context.Callback.Target),

                CilCallInstruction call when context.TemplateTargetPropertyGetters.Contains(call.Method) =>
                    GetCallPropertyGetterOnTargetValue(context, ref argTokens, call, context.Template.Target),

                _ => throw new InvalidOperationException(),
            };

            if (argTokens.Length > 0)
                throw new InvalidOperationException();

            return value;
        }

        private object? GetArgValueFromStatic(Context context, ref ReadOnlyStreamSpan<ICilToken> argTokens)
        {
            if (argTokens.Take() is not ICilInstruction instruction)
                throw new InvalidOperationException();

            var value = instruction switch
            {
                CilLoadStaticFieldInstruction ldsfld when context.CallbackTargetFields.Contains(ldsfld.Field) || context.TemplateTargetFields.Contains(ldsfld.Field) =>
                    GetLoadStaticFieldValue(context, ref argTokens, ldsfld),

                CilCallInstruction call when context.CallbackTargetPropertyGetters.Contains(call.Method) || context.TemplateTargetPropertyGetters.Contains(call.Method) =>
                    GetCallStaticPropertyGetterValue(context, ref argTokens, call),

                _ => throw new InvalidOperationException(),
            };

            if (argTokens.Length > 0)
                throw new InvalidOperationException();

            return value;
        }

        private void ProcessTargetValue(Context context, MemberInfo member, object? value)
        {
            ICilInstruction instruction = value switch
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
                _ => GetStaticCapture(context, member, value),
            };

            context.Result.Add(instruction);
        }

        private ICilInstruction GetStaticCapture(Context context, MemberInfo member, object value)
        {
            if (!context.StaticCaptureMaps.TryGetValue(member, out var propertyBuilder))
            {
                var type = value.GetType();
                var propertyName = $"{member.DeclaringType}.{member.Name}";
                var staticCapturesKey = $"({context.Guid}) {propertyName}";

                var backingFieldBuilder = context.TypeBuilder.DefineAutoPropertyBackingField(
                    propertyName: propertyName,
                    type: type,
                    attributes: FieldAttributes.Private | FieldAttributes.Static);

                // Used to keep track of whether the code has run to pull the value for this capture from the static capture dictionary
                var claimedBuilder = context.TypeBuilder.DefineField(
                    fieldName: $"<{propertyName}>k__Claimed",
                    type: typeof(bool),
                    attributes: FieldAttributes.Private | FieldAttributes.Static
                );

                propertyBuilder = context.TypeBuilder.DefineProperty(
                    name: propertyName,
                    attributes: PropertyAttributes.None,
                    callingConvention: CallingConventions.Standard,
                    returnType: type,
                    parameterTypes: Array.Empty<Type>()
                );

                var getterBuilder = context.TypeBuilder.DefineMethod(
                    name: $"get_{propertyName}",
                    attributes: MethodAttributes.Private | MethodAttributes.Static,
                    callingConvention: CallingConventions.Standard,
                    returnType: type,
                    parameterTypes: Array.Empty<Type>()
                );

                var il = getterBuilder.GetILGenerator();

                var returnLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldsfld, claimedBuilder);
                il.Emit(OpCodes.Brtrue, returnLabel);

                il.Emit(OpCodes.Ldstr, staticCapturesKey);
                il.Emit(OpCodes.Call, typeof(EmitTemplateProcessor).GetMethod(nameof(GetStaticCapture)));
                if (type.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stsfld, backingFieldBuilder);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stsfld, claimedBuilder);

                il.MarkLabel(returnLabel);
                il.Emit(OpCodes.Ldsfld, backingFieldBuilder);
                il.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getterBuilder);

                lock (StaticCapturesLock)
                {
                    StaticCaptures.Add(staticCapturesKey, value);
                }

                context.StaticCaptureMaps.Add(member, propertyBuilder);
            }

            return new CilCallInstruction(propertyBuilder.GetMethod);
        }

        private void ProcessArgsInstructions(Context context, ReadOnlyStreamSpan<ICilToken> tokens, Span<Range> argRanges)
        {
            foreach (var argRange in argRanges)
            {
                var argTokens = tokens.Slice(argRange);
                ProcessTokens(context, ref argTokens);
            }
        }

        private sealed class Context
        {
            public TypeBuilder TypeBuilder { get; }

            public Delegate Callback { get; }

            public Type? CallbackTargetType { get; }

            public HashSet<FieldInfo> CallbackTargetFields { get; }

            public HashSet<MethodInfo> CallbackTargetPropertyGetters { get; }

            public HashSet<MethodInfo> CallbackTargetPropertySetters { get; }

            public Delegate Template { get; }

            public Type? TemplateTargetType { get; }

            public HashSet<FieldInfo> TemplateTargetFields { get; }

            public HashSet<MethodInfo> TemplateTargetPropertyGetters { get; }

            public HashSet<MethodInfo> TemplateTargetPropertySetters { get; }

            public int ArugmentIndexOffset { get; }

            public List<ICilLocalVariable> Locals { get; } = new();

            public List<ICilToken> Result { get; } = new();

            public Dictionary<MemberInfo, object?> CaptureValues { get; } = new();

            public Dictionary<MemberInfo, PropertyBuilder> StaticCaptureMaps { get; } = new();

            public Guid Guid { get; } = Guid.NewGuid();

            public Context(TypeBuilder typeBuilder, Delegate callback, Delegate template, int argumentIndexOffset)
            {
                TypeBuilder = typeBuilder ?? throw new ArgumentNullException(nameof(typeBuilder));
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
                Template = template ?? throw new ArgumentNullException(nameof(template));
                ArugmentIndexOffset = argumentIndexOffset;

                const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                CallbackTargetType = Callback.Target?.GetType();
                CallbackTargetFields = CallbackTargetType?
                    .GetFields(Flags)
                    .ToHashSet()
                    ?? new HashSet<FieldInfo>();
                CallbackTargetPropertyGetters = CallbackTargetType?
                    .GetProperties(Flags)
                    .Select(p => p.GetMethod)
                    .ToHashSet()
                    ?? new HashSet<MethodInfo>();
                CallbackTargetPropertySetters = CallbackTargetType?
                    .GetProperties(Flags)
                    .Select(p => p.SetMethod)
                    .ToHashSet()
                    ?? new HashSet<MethodInfo>();

                TemplateTargetType = Template.Target?.GetType();
                TemplateTargetFields = TemplateTargetType?
                    .GetFields(Flags)
                    .ToHashSet()
                    ?? new HashSet<FieldInfo>();
                TemplateTargetPropertyGetters = TemplateTargetType?
                    .GetProperties(Flags)
                    .Select(p => p.GetMethod)
                    .ToHashSet()
                    ?? new HashSet<MethodInfo>();
                TemplateTargetPropertySetters = TemplateTargetType?
                    .GetProperties(Flags)
                    .Select(p => p.SetMethod)
                    .ToHashSet()
                    ?? new HashSet<MethodInfo>();
            }
        }
    }
}