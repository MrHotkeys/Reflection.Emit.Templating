using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

using MrHotkeys.Reflection.Emit.Templating.Cil.Instructions;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public sealed class CilParser : ICilParser
    {
        private ILogger Logger { get; }

        private Dictionary<short, OpCode> OpCodeLookup { get; }

        public CilParser(ILogger<CilParser> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OpCodeLookup = typeof(OpCodes)
                .GetFields()
                .Select(f => f.GetValue(null))
                .Cast<OpCode>()
                .ToDictionary(c => c.Value);
        }

        public ICilMethodBody ParseMethodBody(MethodInfo method)
        {
            var body = method.GetMethodBody();
            var bytes = (ReadOnlySpan<byte>)body
                .GetILAsByteArray()
                .AsSpan();

            var localCounter = 0;
            var locals = body
                .LocalVariables
                .Select(l => new CilLocalVariable(l.LocalType, $"Local_{localCounter++}") { IsPinned = l.IsPinned })
                .Cast<ICilLocalVariable>()
                .ToList();

            var instructions = new List<(int Address, ICilInstruction Instruction)>();

            var labels = new Dictionary<int, ICilLabel>();
            ICilLabel GetLabel(int address)
            {
                if (!labels.TryGetValue(address, out var label))
                {
                    label = new CilLabel($"Label_{labels.Count}");
                    labels[address] = label;
                }

                return label;
            }

            var address = 0;
            var lastBytesLength = bytes.Length;
            while (bytes.Length > 0)
            {
                address += lastBytesLength - bytes.Length;
                lastBytesLength = bytes.Length;

                var opCode = ParseOpCode(ref bytes);
                var operand = ParseOperand(opCode, ref bytes, method.Module);

                // Calculate address of next instruction for branches
                var nextAddress = address + lastBytesLength - bytes.Length;
                ICilInstruction instruction = (OpCodeName)opCode.Value switch
                {
                    OpCodeName.Ldarg_0 => new CilLoadArgumentInstruction(0),
                    OpCodeName.Ldarg_1 => new CilLoadArgumentInstruction(1),
                    OpCodeName.Ldarg_2 => new CilLoadArgumentInstruction(2),
                    OpCodeName.Ldarg_3 => new CilLoadArgumentInstruction(3),
                    OpCodeName.Ldarg_S => new CilLoadArgumentInstruction((byte)operand!),
                    OpCodeName.Ldarg => new CilLoadArgumentInstruction((int)operand!),

                    OpCodeName.Ldarga_S => new CilLoadArgumentAddressInstruction((byte)operand!),
                    OpCodeName.Ldarga => new CilLoadArgumentAddressInstruction((int)operand!),

                    OpCodeName.Starg_S => new CilLoadArgumentInstruction((byte)operand!),
                    OpCodeName.Starg => new CilLoadArgumentInstruction((int)operand!),

                    OpCodeName.Ldloc_0 => new CilLoadLocalInstruction(locals[0]),
                    OpCodeName.Ldloc_1 => new CilLoadLocalInstruction(locals[1]),
                    OpCodeName.Ldloc_2 => new CilLoadLocalInstruction(locals[2]),
                    OpCodeName.Ldloc_3 => new CilLoadLocalInstruction(locals[3]),
                    OpCodeName.Ldloc_S => new CilLoadLocalInstruction(locals[(byte)operand!]),
                    OpCodeName.Ldloc => new CilLoadLocalInstruction(locals[(int)operand!]),

                    OpCodeName.Ldloca_S => new CilLoadLocalAddressInstruction(locals[(byte)operand!]),
                    OpCodeName.Ldloca => new CilLoadLocalAddressInstruction(locals[(int)operand!]),

                    OpCodeName.Stloc_0 => new CilStoreLocalInstruction(locals[0]),
                    OpCodeName.Stloc_1 => new CilStoreLocalInstruction(locals[1]),
                    OpCodeName.Stloc_2 => new CilStoreLocalInstruction(locals[2]),
                    OpCodeName.Stloc_3 => new CilStoreLocalInstruction(locals[3]),
                    OpCodeName.Stloc_S => new CilStoreLocalInstruction(locals[(byte)operand!]),
                    OpCodeName.Stloc => new CilStoreLocalInstruction(locals[(int)operand!]),

                    OpCodeName.Br_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.Always, },
                    OpCodeName.Br => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.Always, },
                    OpCodeName.Brtrue_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.True, },
                    OpCodeName.Brtrue => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.True, },
                    OpCodeName.Brfalse_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.False, },
                    OpCodeName.Brfalse => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.False, },
                    OpCodeName.Beq_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.Equal, },
                    OpCodeName.Beq => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.Equal, },
                    OpCodeName.Bne_Un_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.NotEqual, },
                    OpCodeName.Bne_Un => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.NotEqual, },
                    OpCodeName.Bgt_Un_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.GreaterThanUnsignedUnordered, },
                    OpCodeName.Bgt_Un => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.GreaterThanUnsignedUnordered, },
                    OpCodeName.Bge_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.GreaterThanOrEqual, },
                    OpCodeName.Bge => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.GreaterThanOrEqual, },
                    OpCodeName.Bge_Un_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.GreaterThanOrEqualUnsignedUnordered, },
                    OpCodeName.Bge_Un => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.GreaterThanOrEqualUnsignedUnordered, },
                    OpCodeName.Blt_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.LessThan, },
                    OpCodeName.Blt => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.LessThan, },
                    OpCodeName.Blt_Un_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.LessThanUnsignedUnordered, },
                    OpCodeName.Blt_Un => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.LessThanUnsignedUnordered, },
                    OpCodeName.Ble_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.LessThanOrEqual, },
                    OpCodeName.Ble => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.LessThanOrEqual, },
                    OpCodeName.Ble_Un_S => new CilBranchInstruction(GetLabel(nextAddress + (sbyte)operand!)) { Condition = CilBranchCondition.LessThanOrEqualUnsignedUnordered, },
                    OpCodeName.Ble_Un => new CilBranchInstruction(GetLabel(nextAddress + (int)operand!)) { Condition = CilBranchCondition.LessThanOrEqualUnsignedUnordered, },

                    OpCodeName.Ldfld => new CilLoadFieldInstruction((FieldInfo)operand!),
                    OpCodeName.Ldsfld => new CilLoadStaticFieldInstruction((FieldInfo)operand!),
                    OpCodeName.Stfld => new CilStoreFieldInstruction((FieldInfo)operand!),
                    OpCodeName.Stsfld => new CilStoreStaticFieldInstruction((FieldInfo)operand!),

                    OpCodeName.Call or
                    OpCodeName.Callvirt => new CilCallInstruction((MethodBase)operand!),

                    OpCodeName.Ret => new CilReturnInstruction(),

                    _ => new CilRawInstruction(opCode, operand),
                };

                instructions.Add((address, instruction));
            }

            var tokens = new List<ICilToken>(instructions.Count + labels.Count);
            for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var current = instructions[instructionIndex];

                // Want to add any labels *before* the instruction we're jumping to
                if (labels.TryGetValue(current.Address, out var label))
                    tokens.Add(label);

                tokens.Add(current.Instruction);
            }

            return new CilMethodBody(locals, tokens);
        }

        public OpCode ParseOpCode(ref ReadOnlySpan<byte> bytes)
        {
            var key = (short)bytes[0];
            if (key != 0xFE)
            {
                bytes = bytes.Slice(1);
            }
            else
            {
                key = unchecked((short)((key << 8) + bytes[1]));
                bytes = bytes.Slice(2);
            }

            return OpCodeLookup.TryGetValue(key, out var opCode) ?
                opCode :
                throw new InvalidOperationException();
        }

        public object? ParseOperand(OpCode opCode, ref ReadOnlySpan<byte> bytes, Module module)
        {
            return opCode.OperandType switch
            {
                OperandType.InlineNone => null,

                OperandType.ShortInlineI when (OpCodeName)opCode.Value == OpCodeName.Ldc_I4_S => (object?)ParseOperandSByte(ref bytes),

                OperandType.ShortInlineI => ParseOperandByte(ref bytes),
                OperandType.InlineI => ParseOperandInt(ref bytes),
                OperandType.InlineI8 => ParseOperandLong(ref bytes),

                OperandType.ShortInlineR => ParseOperandFloat(ref bytes),
                OperandType.InlineR => ParseOperandDouble(ref bytes),

                OperandType.ShortInlineBrTarget => ParseOperandSByte(ref bytes),
                OperandType.InlineBrTarget => ParseOperandInt(ref bytes),

                OperandType.InlineString => ParseOperandString(ref bytes, module),

                OperandType.InlineType => ParseOperandType(ref bytes, module),
                OperandType.InlineField => ParseOperandField(ref bytes, module),
                OperandType.InlineMethod => ParseOperandMethod(ref bytes, module),
                OperandType.InlineSig => ParseOperandSignature(ref bytes, module),

                OperandType.ShortInlineVar => ParseOperandByte(ref bytes),
                OperandType.InlineVar => ParseOperandUShort(ref bytes),

                OperandType.InlineSwitch => ParseOperandUInt(ref bytes),

                OperandType.InlineTok => ParseOperandMetadataToken(ref bytes, module),

                OperandType.InlinePhi => throw new NotSupportedException(),

                _ => throw new InvalidOperationException(),
            };
        }

        public byte ParseOperandByte(ref ReadOnlySpan<byte> bytes)
        {
            var value = bytes[0];
            bytes = bytes.Slice(sizeof(byte));
            return value;
        }

        public sbyte ParseOperandSByte(ref ReadOnlySpan<byte> bytes)
        {
            var value = unchecked((sbyte)bytes[0]);
            bytes = bytes.Slice(sizeof(sbyte));
            return value;
        }

        public ushort ParseOperandUShort(ref ReadOnlySpan<byte> bytes)
        {
            var value = BitConverter.ToUInt16(bytes);
            bytes = bytes.Slice(sizeof(ushort));
            return value;
        }

        public int ParseOperandInt(ref ReadOnlySpan<byte> bytes)
        {
            var value = BitConverter.ToInt32(bytes);
            bytes = bytes.Slice(sizeof(int));
            return value;
        }

        public uint ParseOperandUInt(ref ReadOnlySpan<byte> bytes)
        {
            var value = BitConverter.ToUInt32(bytes);
            bytes = bytes.Slice(sizeof(uint));
            return value;
        }

        public long ParseOperandLong(ref ReadOnlySpan<byte> bytes)
        {
            var value = BitConverter.ToInt64(bytes);
            bytes = bytes.Slice(sizeof(long));
            return value;
        }

        public float ParseOperandFloat(ref ReadOnlySpan<byte> bytes)
        {
            var value = BitConverter.ToSingle(bytes);
            bytes = bytes.Slice(sizeof(float));
            return value;
        }

        public double ParseOperandDouble(ref ReadOnlySpan<byte> bytes)
        {
            var value = BitConverter.ToDouble(bytes);
            bytes = bytes.Slice(sizeof(double));
            return value;
        }

        public string ParseOperandString(ref ReadOnlySpan<byte> bytes, Module module)
        {
            var token = ParseOperandInt(ref bytes);
            return module.ResolveString(token);
        }

        public Type ParseOperandType(ref ReadOnlySpan<byte> bytes, Module module)
        {
            var token = ParseOperandInt(ref bytes);
            return module.ResolveType(token);
        }

        public FieldInfo ParseOperandField(ref ReadOnlySpan<byte> bytes, Module module)
        {
            var token = ParseOperandInt(ref bytes);
            return module.ResolveField(token);
        }

        public MethodBase ParseOperandMethod(ref ReadOnlySpan<byte> bytes, Module module)
        {
            var token = ParseOperandInt(ref bytes);
            return module.ResolveMethod(token);
        }

        public byte[] ParseOperandSignature(ref ReadOnlySpan<byte> bytes, Module module)
        {
            Logger.LogWarning($"Resolving signature as {typeof(byte[]).Name} - writing  is unsupported unless converted to {nameof(SignatureHelper)}!");
            var token = ParseOperandInt(ref bytes);
            return module.ResolveSignature(token);
        }

        public MemberInfo ParseOperandMember(ref ReadOnlySpan<byte> bytes, Module module)
        {
            var token = ParseOperandInt(ref bytes);
            return module.ResolveMember(token);
        }

        public object ParseOperandMetadataToken(ref ReadOnlySpan<byte> bytes, Module module)
        {
            // Most significant byte of token defines type
            var mostSignificantByteIndex = BitConverter.IsLittleEndian ? 3 : 0;
            var tokenType = (CilMetadataTokenType)bytes[mostSignificantByteIndex];
            return tokenType switch
            {
                CilMetadataTokenType.Type => ParseOperandType(ref bytes, module),
                CilMetadataTokenType.Field => ParseOperandField(ref bytes, module),
                CilMetadataTokenType.Method => ParseOperandMethod(ref bytes, module),
                CilMetadataTokenType.Member => ParseOperandMember(ref bytes, module),
                CilMetadataTokenType.Signature => ParseOperandSignature(ref bytes, module),
                CilMetadataTokenType.String => ParseOperandString(ref bytes, module),
                _ => throw new NotSupportedException(),
            };
        }
    }
}