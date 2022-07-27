using System;
using System.Collections.Generic;
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
            var context = new Context(
                typeBuilder: typeBuilder,
                callback: callback,
                template: template,
                templateBody: CilParser.ParseMethodBody(template.Method),
                argumentIndexOffset: GetArgIndexOffset(template.Method.IsStatic, outIsStatic));

            if (callback.Target is not null)
                AddCaptures(callback.Target, context.Captures);
            if (template.Target is not null)
                AddCaptures(template.Target, context.Captures);

            for (var i = 0; i < context.TemplateBody.Tokens.Count; i++)
            {
                var token = context.TemplateBody.Tokens[i];
                ProcessToken(context, ref i, token);
            }

            context.TemplateBody.Tokens = context.Pop();

            if (context.TemplateBody.Tokens[^1] is ICilInstruction lastInstruction && lastInstruction.InstructionType == CilInstructionType.Return)
                context.TemplateBody.Tokens.RemoveAt(context.TemplateBody.Tokens.Count - 1);

            CilWriter.Write(il, context.TemplateBody);
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

        private void ProcessToken(Context context, ref int index, ICilToken token)
        {
            switch (token.TokenType)
            {
                case CilTokenType.Label:
                    {
                        if (token is not ICilLabel label)
                            throw new InvalidOperationException();
                        ProcessLabel(context, ref index, label);
                        break;
                    }
                case CilTokenType.Instruction:
                    {
                        if (token is not ICilInstruction instruction)
                            throw new InvalidOperationException();
                        ProcessInstruction(context, ref index, instruction);
                        break;
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        private void ProcessLabel(Context context, ref int index, ICilLabel label)
        {
            context.Add(label);
        }

        private void ProcessInstruction(Context context, ref int index, ICilInstruction instruction)
        {
            switch (instruction.InstructionType)
            {
                case CilInstructionType.LoadArgument when !context.Template.Method.IsStatic && instruction is CilLoadArgumentInstruction ldarg && ldarg.Index == 0:
                    {
                        ProcessCapturesAccess(context, ref index, ldarg);
                        break;
                    }

                case CilInstructionType.Call when !context.Template.Method.IsStatic && instruction is CilCallInstruction call && call.Method.DeclaringType == typeof(EmitTemplateSurrogate):
                    {
                        ProcessSurrogateCall(context, ref index, call);
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

                        context.Add(instruction);
                        break;
                    }
            }
        }

        private void ProcessCapturesAccess(Context context, ref int index, CilLoadArgumentInstruction ldarg)
        {
            // If here, we're loading outer capture
            if (context.TemplateBody.Tokens[index + 1] is CilLoadFieldInstruction ldfld)
            {
                index++;

                if (ldfld.Field.FieldType == typeof(EmitTemplateSurrogate))
                {
                    // Add a new group in prep for a call
                    context.Push();
                }
                else
                {
                    // Check if we're in outer or inner capture
                    if (ldfld.Field.FieldType == context.Callback.Target?.GetType())
                    {
                        // Load inner capture from outer capture
                        if (context.TemplateBody.Tokens[index + 1] is not CilLoadFieldInstruction ldfld_inner)
                            throw new InvalidOperationException();

                        index++;

                        ldfld = ldfld_inner;
                    }

                    if (ldfld.Field.DeclaringType != context.Callback.Target?.GetType())
                        throw new InvalidOperationException();

                    var capture = context.Captures[ldfld.Field];

                    if (capture is MemberInfo member && member.DeclaringType == context.TypeBuilder)
                    {
                        context.TargetMemberStack.Push(member);
                    }
                    else
                    {
                        if (capture is null)
                        {
                            context.Add(new CilRawInstruction(OpCodes.Ldnull));
                        }
                        else
                        {
                            var captureType = capture.GetType();
                            switch (Type.GetTypeCode(captureType))
                            {
                                case TypeCode.Boolean:
                                    if ((bool)capture)
                                        context.Add(new CilRawInstruction(OpCodes.Ldc_I4_1));
                                    else
                                        context.Add(new CilRawInstruction(OpCodes.Ldc_I4_0));
                                    break;
                                case TypeCode.SByte:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I4_S, (byte)capture!));
                                    break;
                                case TypeCode.Byte:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I4, (byte)capture!));
                                    break;
                                case TypeCode.Int16:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I4, (short)capture!));
                                    break;
                                case TypeCode.UInt16:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I4, (ushort)capture!));
                                    break;
                                case TypeCode.Int32:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I4, (int)capture!));
                                    break;
                                case TypeCode.UInt32:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I4, (uint)capture!));
                                    break;
                                case TypeCode.Int64:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I8, (long)capture!));
                                    break;
                                case TypeCode.UInt64:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_I8, (ulong)capture!));
                                    break;
                                case TypeCode.Single:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_R4, (float)capture!));
                                    break;
                                case TypeCode.Double:
                                    context.Add(new CilRawInstruction(OpCodes.Ldc_R8, (double)capture!));
                                    break;
                                case TypeCode.String:
                                    context.Add(new CilRawInstruction(OpCodes.Ldstr, (string)capture!));
                                    break;
                                default:
                                    throw new NotSupportedException($"Captured value \"{ldfld.Field.Name}\" has unsupported non-primitive type {captureType}!");
                            }
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void ProcessSurrogateCall(Context context, ref int index, CilCallInstruction call)
        {
            switch (call.Method.Name)
            {
                case nameof(EmitTemplateSurrogate.Get):
                    {
                        var member = context.TargetMemberStack.Pop();
                        var group = context.Pop();

                        switch (member.MemberType)
                        {
                            case MemberTypes.Field when member is FieldInfo field:
                                if (!field.IsStatic)
                                    context.Add(new CilLoadArgumentInstruction(0));
                                context.Add(group);
                                context.Add(new CilLoadFieldInstruction(field));
                                break;
                            case MemberTypes.Property when member is PropertyInfo property:
                                if (!property.GetMethod.IsStatic)
                                    context.Add(new CilLoadArgumentInstruction(0));
                                context.Add(group);
                                context.Add(new CilCallInstruction(property.GetMethod));
                                break;
                            default:
                                throw new Exception();
                        }

                        break;
                    }

                case nameof(EmitTemplateSurrogate.Set):
                    {
                        var member = context.TargetMemberStack.Pop();
                        var group = context.Pop();

                        switch (member.MemberType)
                        {
                            case MemberTypes.Field when member is FieldInfo field:
                                if (!field.IsStatic)
                                    context.Add(new CilLoadArgumentInstruction(0));
                                context.Add(group);
                                context.Add(new CilStoreFieldInstruction(field));
                                break;
                            case MemberTypes.Property when member is PropertyInfo property:
                                if (!property.SetMethod.IsStatic)
                                    context.Add(new CilLoadArgumentInstruction(0));
                                context.Add(group);
                                context.Add(new CilCallInstruction(property.SetMethod));
                                break;
                            default:
                                throw new Exception();
                        }

                        break;
                    }

                case nameof(EmitTemplateSurrogate.Call):
                    {
                        var method = (MethodInfo)context.TargetMemberStack.Pop();
                        var group = context.Pop();

                        if (!method.IsStatic)
                            context.Add(new CilLoadArgumentInstruction(0));

                        context.Add(group);
                        context.Add(new CilCallInstruction(method));

                        break;
                    }
            }
        }

        private sealed class Context
        {
            public TypeBuilder TypeBuilder { get; }

            public Delegate Callback { get; }

            public Delegate Template { get; }

            public ICilMethodBody TemplateBody { get; }

            public int ArugmentIndexOffset { get; }

            public Dictionary<MemberInfo, object?> Captures { get; } = new();

            public Stack<MemberInfo> TargetMemberStack { get; } = new();

            private Stack<List<ICilToken>> Groups { get; } = new();

            public Context(TypeBuilder typeBuilder, Delegate callback, Delegate template, ICilMethodBody templateBody, int argumentIndexOffset)
            {
                TypeBuilder = typeBuilder ?? throw new ArgumentNullException(nameof(typeBuilder));
                Callback = callback ?? throw new ArgumentNullException(nameof(callback));
                Template = template ?? throw new ArgumentNullException(nameof(template));
                TemplateBody = templateBody ?? throw new ArgumentNullException(nameof(templateBody));
                ArugmentIndexOffset = argumentIndexOffset;

                Push();
            }

            public void Add(ICilToken token)
            {
                Groups.Peek().Add(token);
            }

            public void Add(IEnumerable<ICilToken> tokens)
            {
                Groups.Peek().AddRange(tokens);
            }

            public void Push()
            {
                Groups.Push(new());
            }

            public List<ICilToken> Pop()
            {
                return Groups.Pop();
            }
        }
    }
}