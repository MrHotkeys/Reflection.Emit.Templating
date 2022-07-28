using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

using MrHotkeys.Reflection.Emit.Templating.Cil.Instructions;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public sealed class CilWriter : ICilWriter
    {
        private ILogger Logger { get; }

        public LogLevel? EmitLogLevel { get; } = LogLevel.Debug;

        public CilWriter(ILogger<CilWriter> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Write(ILGenerator il, ICilMethodBody body)
        {
            var context = new Context(body);

            for (var i = 0; i < body.Locals.Count; i++)
            {
                var local = body.Locals[i];
                context.Locals[local] = il.DeclareLocal(local.Type, local.IsPinned);
            }

            var labels = body
                .Tokens
                .Where(t => t.TokenType == CilTokenType.Label)
                .OfType<ICilLabel>();
            foreach (var label in labels)
                context.Labels[label] = il.DefineLabel();

            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{nameof(CilWriter)}.{nameof(CilWriter.Write)} START");

            foreach (var token in body.Tokens)
                WriteToken(il, context, token);

            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{nameof(CilWriter)}.{nameof(CilWriter.Write)} END");
        }

        private void WriteToken(ILGenerator il, Context context, ICilToken token)
        {
            switch (token.TokenType)
            {
                case CilTokenType.Label:
                    {
                        if (token is not ICilLabel label)
                            throw new InvalidOperationException();
                        WriteLabel(il, context, label);
                        break;
                    }
                case CilTokenType.Instruction:
                    {
                        if (token is not ICilInstruction instruction)
                            throw new InvalidOperationException();
                        WriteInstruction(il, context, instruction);
                        break;
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        private void WriteLabel(ILGenerator il, Context context, ICilLabel label)
        {
            if (!context.Labels.TryGetValue(label, out var emittedLabel))
                throw new InvalidOperationException();

            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{label}");

            il.MarkLabel(emittedLabel);
        }

        private void WriteInstruction(ILGenerator il, Context context, ICilInstruction instruction)
        {
            switch (instruction.InstructionType)
            {
                case CilInstructionType.LoadArgument:
                    {
                        if (instruction is not CilLoadArgumentInstruction ldarg)
                            throw new InvalidOperationException();
                        WriteLoadArgumentInstruction(il, context, ldarg);
                        break;
                    }
                case CilInstructionType.LoadArgumentAddress:
                    {
                        if (instruction is not CilLoadArgumentAddressInstruction ldarga)
                            throw new InvalidOperationException();
                        WriteLoadArgumentAddressInstruction(il, context, ldarga);
                        break;
                    }
                case CilInstructionType.StoreArgument:
                    {
                        if (instruction is not CilStoreArgumentInstruction starg)
                            throw new InvalidOperationException();
                        WriteStoreArgumentInstruction(il, context, starg);
                        break;
                    }
                case CilInstructionType.LoadLocal:
                    {
                        if (instruction is not CilLoadLocalInstruction ldloc)
                            throw new InvalidOperationException();
                        WriteLoadLocalInstruction(il, context, ldloc);
                        break;
                    }
                case CilInstructionType.LoadLocalAddress:
                    {
                        if (instruction is not CilLoadLocalAddressInstruction ldloca)
                            throw new InvalidOperationException();
                        WriteLoadLocalAddressInstruction(il, context, ldloca);
                        break;
                    }
                case CilInstructionType.StoreLocal:
                    {
                        if (instruction is not CilStoreLocalInstruction stloc)
                            throw new InvalidOperationException();
                        WriteStoreLocalInstruction(il, context, stloc);
                        break;
                    }
                case CilInstructionType.Call:
                    {
                        if (instruction is not CilCallInstruction call)
                            throw new InvalidOperationException();
                        WriteCallInstruction(il, context, call);
                        break;
                    }
                case CilInstructionType.Branch:
                    {
                        if (instruction is not CilBranchInstruction br)
                            throw new InvalidOperationException();
                        WriteBranchInstruction(il, context, br);
                        break;
                    }
                case CilInstructionType.LoadField:
                    {
                        if (instruction is not CilLoadFieldInstruction ldfld)
                            throw new InvalidOperationException();
                        WriteLoadFieldInstruction(il, context, ldfld);
                        break;
                    }
                case CilInstructionType.StoreField:
                    {
                        if (instruction is not CilStoreFieldInstruction stfld)
                            throw new InvalidOperationException();
                        WriteStoreFieldInstruction(il, context, stfld);
                        break;
                    }
                case CilInstructionType.Return:
                    {
                        if (instruction is not CilReturnInstruction ret)
                            throw new InvalidOperationException();
                        WriteReturnInstruction(il, context, ret);
                        break;
                    }
                case CilInstructionType.Raw:
                    {
                        if (instruction is not CilRawInstruction raw)
                            throw new InvalidOperationException();
                        WriteRawInstruction(il, context, raw);
                        break;
                    }
                default:
                    throw new InvalidOperationException();
            }
        }

        private void WriteLoadArgumentInstruction(ILGenerator il, Context context, CilLoadArgumentInstruction ldarg)
        {
            var index = ldarg.Index;
            switch (index)
            {
                case 0:
                    EmitAndLog(il, OpCodes.Ldarg_0);
                    break;
                case 1:
                    EmitAndLog(il, OpCodes.Ldarg_1);
                    break;
                case 2:
                    EmitAndLog(il, OpCodes.Ldarg_2);
                    break;
                case 3:
                    EmitAndLog(il, OpCodes.Ldarg_3);
                    break;
                default:
                    if (index >= byte.MinValue && index <= byte.MaxValue)
                        EmitAndLog(il, OpCodes.Ldarg_S, (byte)index);
                    else
                        EmitAndLog(il, OpCodes.Ldarg, index);
                    break;
            }
        }

        private void WriteLoadArgumentAddressInstruction(ILGenerator il, Context context, CilLoadArgumentAddressInstruction ldarga)
        {
            var index = ldarga.Index;
            if (index >= byte.MinValue && index <= byte.MaxValue)
                EmitAndLog(il, OpCodes.Ldarga_S, (byte)index);
            else
                EmitAndLog(il, OpCodes.Ldarga, index);
        }

        private void WriteStoreArgumentInstruction(ILGenerator il, Context context, CilStoreArgumentInstruction starg)
        {
            var index = starg.Index;
            if (index >= byte.MinValue && index <= byte.MaxValue)
                EmitAndLog(il, OpCodes.Starg_S, (byte)index);
            else
                EmitAndLog(il, OpCodes.Starg, index);
        }

        private void WriteLoadLocalInstruction(ILGenerator il, Context context, CilLoadLocalInstruction ldloc)
        {
            if (!context.Locals.TryGetValue(ldloc.Local, out var emittedLocal))
                throw new InvalidOperationException();

            var index = emittedLocal.LocalIndex;
            switch (index)
            {
                case 0:
                    EmitAndLog(il, OpCodes.Ldloc_0);
                    break;
                case 1:
                    EmitAndLog(il, OpCodes.Ldloc_1);
                    break;
                case 2:
                    EmitAndLog(il, OpCodes.Ldloc_2);
                    break;
                case 3:
                    EmitAndLog(il, OpCodes.Ldloc_3);
                    break;
                default:
                    if (index >= byte.MinValue && index <= byte.MaxValue)
                        EmitAndLog(il, OpCodes.Ldloc_S, (byte)index);
                    else
                        EmitAndLog(il, OpCodes.Ldloc, index);
                    break;
            }
        }

        private void WriteLoadLocalAddressInstruction(ILGenerator il, Context context, CilLoadLocalAddressInstruction ldloca)
        {
            if (!context.Locals.TryGetValue(ldloca.Local, out var emittedLocal))
                throw new InvalidOperationException();

            var index = emittedLocal.LocalIndex;
            if (index >= byte.MinValue && index <= byte.MaxValue)
                EmitAndLog(il, OpCodes.Ldloca_S, (byte)index);
            else
                EmitAndLog(il, OpCodes.Ldloca, index);
        }

        private void WriteStoreLocalInstruction(ILGenerator il, Context context, CilStoreLocalInstruction stloc)
        {
            if (!context.Locals.TryGetValue(stloc.Local, out var emittedLocal))
                throw new InvalidOperationException();

            var index = emittedLocal.LocalIndex;
            switch (index)
            {
                case 0:
                    EmitAndLog(il, OpCodes.Stloc_0);
                    break;
                case 1:
                    EmitAndLog(il, OpCodes.Stloc_1);
                    break;
                case 2:
                    EmitAndLog(il, OpCodes.Stloc_2);
                    break;
                case 3:
                    EmitAndLog(il, OpCodes.Stloc_3);
                    break;
                default:
                    if (index >= byte.MinValue && index <= byte.MaxValue)
                        EmitAndLog(il, OpCodes.Stloc_S, (byte)index);
                    else
                        EmitAndLog(il, OpCodes.Stloc, index);
                    break;
            }
        }

        private void WriteCallInstruction(ILGenerator il, Context context, CilCallInstruction call)
        {
            if (call.Method is not MethodInfo method)
                throw new InvalidOperationException();

            if (call.Method.IsStatic || call.Method.DeclaringType.IsSealed || (!call.Method.IsAbstract && !call.Method.IsVirtual))
                EmitAndLog(il, OpCodes.Call, method);
            else
                EmitAndLog(il, OpCodes.Callvirt, method);

        }

        private void WriteBranchInstruction(ILGenerator il, Context context, CilBranchInstruction br)
        {
            var opCode = br.Condition switch
            {
                CilBranchCondition.Always => OpCodes.Br,
                CilBranchCondition.True => OpCodes.Brtrue,
                CilBranchCondition.False => OpCodes.Brfalse,
                CilBranchCondition.Equal => OpCodes.Beq,
                CilBranchCondition.NotEqual => OpCodes.Bne_Un,
                CilBranchCondition.GreaterThan => OpCodes.Bgt,
                CilBranchCondition.GreaterThanOrEqual => OpCodes.Bge,
                CilBranchCondition.LessThan => OpCodes.Blt,
                CilBranchCondition.LessThanOrEqual => OpCodes.Ble,
                CilBranchCondition.GreaterThanUnsignedUnordered => OpCodes.Bgt_Un,
                CilBranchCondition.GreaterThanOrEqualUnsignedUnordered => OpCodes.Bge_Un,
                CilBranchCondition.LessThanUnsignedUnordered => OpCodes.Blt_Un,
                CilBranchCondition.LessThanOrEqualUnsignedUnordered => OpCodes.Ble_Un,
                _ => throw new InvalidOperationException(),
            };

            if (!context.Labels.TryGetValue(br.Label, out var emittedLabel))
                throw new InvalidOperationException();

            EmitAndLog(il, opCode, emittedLabel, br.Label);
        }

        private void WriteLoadFieldInstruction(ILGenerator il, Context context, CilLoadFieldInstruction ldfld)
        {
            EmitAndLog(il, OpCodes.Ldfld, ldfld.Field);
        }

        private void WriteStoreFieldInstruction(ILGenerator il, Context context, CilStoreFieldInstruction stfld)
        {
            EmitAndLog(il, OpCodes.Stfld, stfld.Field);
        }

        private void WriteReturnInstruction(ILGenerator il, Context context, CilReturnInstruction ret)
        {
            EmitAndLog(il, OpCodes.Ret);
        }

        private void WriteRawInstruction(ILGenerator il, Context context, CilRawInstruction raw)
        {
            var opCode = raw.OpCode;
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    EmitAndLog(il, opCode);
                    break;

                case OperandType.ShortInlineI when (OpCodeName)opCode.Value == OpCodeName.Ldc_I4_S:
                    EmitAndLog(il, opCode, (sbyte)raw.Operand!);
                    break;

                case OperandType.ShortInlineI:
                    EmitAndLog(il, opCode, (byte)raw.Operand!);
                    break;
                case OperandType.InlineI:
                    EmitAndLog(il, opCode, (int)raw.Operand!);
                    break;
                case OperandType.InlineI8:
                    EmitAndLog(il, opCode, (long)raw.Operand!);
                    break;

                case OperandType.ShortInlineR:
                    EmitAndLog(il, opCode, (float)raw.Operand!);
                    break;
                case OperandType.InlineR:
                    EmitAndLog(il, opCode, (double)raw.Operand!);
                    break;

                case OperandType.ShortInlineBrTarget:
                    EmitAndLog(il, opCode, (sbyte)raw.Operand!);
                    break;
                case OperandType.InlineBrTarget:
                    EmitAndLog(il, opCode, (int)raw.Operand!);
                    break;

                case OperandType.InlineString:
                    EmitAndLog(il, opCode, (string)raw.Operand!);
                    break;

                case OperandType.InlineType:
                    EmitAndLog(il, opCode, (Type)raw.Operand!);
                    break;
                case OperandType.InlineField:
                    EmitAndLog(il, opCode, (FieldInfo)raw.Operand!);
                    break;
                case OperandType.InlineMethod:
                    EmitAndLog(il, opCode, (MethodInfo)raw.Operand!);
                    break;
                case OperandType.InlineSig:
                    throw new NotImplementedException(); // EmitAndLog(il, opCode, (byte[])instruction.Operand!);

                case OperandType.ShortInlineVar:
                    EmitAndLog(il, opCode, (byte)raw.Operand!);
                    break;
                case OperandType.InlineVar:
                    EmitAndLog(il, opCode, (ushort)raw.Operand!);
                    break;

                case OperandType.InlineSwitch:
                    EmitAndLog(il, opCode, (uint)raw.Operand!);
                    break;

                case OperandType.InlineTok:
                    throw new NotImplementedException();

                case OperandType.InlinePhi:
                    throw new NotSupportedException();

                default:
                    throw new InvalidOperationException();
            };

        }

        private void EmitAndLog(ILGenerator il, OpCode opCode)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, opCode.Name);
            il.Emit(opCode);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, byte operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand}");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, int operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand}");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, long operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand}L");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, float operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand}f");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, double operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand}d");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, string operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} \"{operand}\"");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, Label operand, ICilLabel label)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {label.Name}");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, Type operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand}");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, FieldInfo operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand.FieldType} {operand.DeclaringType}.{operand.Name}");
            il.Emit(opCode, operand);
        }

        private void EmitAndLog(ILGenerator il, OpCode opCode, MethodInfo operand)
        {
            if (EmitLogLevel.HasValue && Logger.IsEnabled(EmitLogLevel.Value))
                Logger.Log(EmitLogLevel.Value, $"{opCode.Name} {operand.ReturnType.Name ?? "void"} {operand.DeclaringType}.{operand.Name}");
            il.Emit(opCode, operand);
        }

        private sealed class Context
        {
            public Dictionary<ICilLocalVariable, LocalBuilder> Locals { get; } = new Dictionary<ICilLocalVariable, LocalBuilder>();

            public Dictionary<ICilLabel, Label> Labels { get; } = new Dictionary<ICilLabel, Label>();

            public ICilMethodBody Body { get; }

            public Context(ICilMethodBody body)
            {
                Body = body ?? throw new ArgumentNullException(nameof(body));
            }
        }
    }
}