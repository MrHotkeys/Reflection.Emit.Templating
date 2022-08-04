using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

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
            var bytes = new ReadOnlyStreamSpan<byte>(body.GetILAsByteArray());

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

            var genericContext = new GenericContext(
                typeArguments: method.DeclaringType.GenericTypeArguments,
                methodArguments: method.GetGenericArguments()
            );

            var address = 0;
            var lastBytesLength = bytes.Length;
            while (bytes.Length > 0)
            {
                address += lastBytesLength - bytes.Length;
                lastBytesLength = bytes.Length;

                var opCode = ParseOpCode(ref bytes);
                var operand = ParseOperand(opCode, ref bytes, method.Module, genericContext);

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
                    OpCodeName.Ldflda => new CilLoadFieldAddressInstruction((FieldInfo)operand!),
                    OpCodeName.Ldsfld => new CilLoadStaticFieldInstruction((FieldInfo)operand!),
                    OpCodeName.Ldsflda => new CilLoadStaticFieldAddressInstruction((FieldInfo)operand!),
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

        private OpCode ParseOpCode(ref ReadOnlyStreamSpan<byte> bytes)
        {
            var key = bytes[0] != 0xFE ?
                bytes.Take<byte>() :
                bytes.Take<short>(false);

            return OpCodeLookup.TryGetValue(key, out var opCode) ?
                opCode :
                throw new InvalidOperationException();
        }

        private object? ParseOperand(OpCode opCode, ref ReadOnlyStreamSpan<byte> bytes, Module module, GenericContext genericContext)
        {
            // CMA-335 6th Edition / June 2012 -> VI.C.2
            return opCode.OperandType switch
            {
                OperandType.InlineNone => null,

                OperandType.ShortInlineI when (OpCodeName)opCode.Value == OpCodeName.Ldc_I4_S => (object?)bytes.Take<sbyte>(),

                OperandType.ShortInlineI => bytes.Take<byte>(),
                OperandType.InlineI => bytes.Take<int>(),
                OperandType.InlineI8 => bytes.Take<long>(),

                OperandType.ShortInlineR => bytes.Take<float>(),
                OperandType.InlineR => bytes.Take<double>(),

                OperandType.ShortInlineBrTarget => bytes.Take<sbyte>(),
                OperandType.InlineBrTarget => bytes.Take<int>(),

                OperandType.InlineString => ParseOperandString(ref bytes, module),

                OperandType.InlineType => ParseOperandType(ref bytes, module, genericContext),
                OperandType.InlineField => ParseOperandField(ref bytes, module, genericContext),
                OperandType.InlineMethod => ParseOperandMethod(ref bytes, module, genericContext),
                OperandType.InlineSig => ParseOperandSignature(ref bytes, module),

                OperandType.InlineTok => ParseOperandMetadataToken(ref bytes, module, genericContext),

                OperandType.ShortInlineVar => bytes.Take<byte>(),
                OperandType.InlineVar => bytes.Take<ushort>(),

                OperandType.InlineSwitch => bytes.Take<uint>(),

                OperandType.InlinePhi => throw new NotSupportedException(),

                _ => throw new InvalidOperationException(),
            };
        }

        private string ParseOperandString(ref ReadOnlyStreamSpan<byte> bytes, Module module)
        {
            var token = bytes.Take<int>();
            return module.ResolveString(token);
        }

        private Type ParseOperandType(ref ReadOnlyStreamSpan<byte> bytes, Module module, GenericContext genericContext)
        {
            return module.ResolveType(
                metadataToken: bytes.Take<int>(),
                genericTypeArguments: genericContext.TypeArguments,
                genericMethodArguments: genericContext.MethodArguments
            );
        }

        private FieldInfo ParseOperandField(ref ReadOnlyStreamSpan<byte> bytes, Module module, GenericContext genericContext)
        {
            return module.ResolveField(
                metadataToken: bytes.Take<int>(),
                genericTypeArguments: genericContext.TypeArguments,
                genericMethodArguments: genericContext.MethodArguments
            );
        }

        private MethodBase ParseOperandMethod(ref ReadOnlyStreamSpan<byte> bytes, Module module, GenericContext genericContext)
        {
            return module.ResolveMethod(
                metadataToken: bytes.Take<int>(),
                genericTypeArguments: genericContext.TypeArguments,
                genericMethodArguments: genericContext.MethodArguments
            );
        }

        private MemberInfo ParseOperandMemberRef(ref ReadOnlyStreamSpan<byte> bytes, Module module, GenericContext genericContext)
        {
            return module.ResolveMember(
                metadataToken: bytes.Take<int>(),
                genericTypeArguments: genericContext.TypeArguments,
                genericMethodArguments: genericContext.MethodArguments
            );
        }

        private byte[] ParseOperandSignature(ref ReadOnlyStreamSpan<byte> bytes, Module module)
        {
            Logger.LogWarning($"Resolving signature as {typeof(byte[]).Name} at position {bytes.Position} - writing is unsupported unless converted to {nameof(SignatureHelper)}!");

            var token = bytes.Take<int>();
            return module.ResolveSignature(token);
        }

        private object ParseOperandMetadataToken(ref ReadOnlyStreamSpan<byte> bytes, Module module, GenericContext genericContext)
        {
            return GetMetadataTokenType(ref bytes) switch
            {
                CilMetadataTokenType.Field => ParseOperandField(ref bytes, module, genericContext),
                CilMetadataTokenType.MemberRef => ParseOperandMemberRef(ref bytes, module, genericContext),
                CilMetadataTokenType.MethodDef => ParseOperandMethod(ref bytes, module, genericContext),
                CilMetadataTokenType.StandAloneSig => ParseOperandSignature(ref bytes, module),
                CilMetadataTokenType.String => ParseOperandString(ref bytes, module),
                CilMetadataTokenType.TypeRef => ParseOperandType(ref bytes, module, genericContext),
                _ => throw new NotSupportedException(),
            };
        }

        private CilMetadataTokenType GetMetadataTokenType(ref ReadOnlyStreamSpan<byte> bytes)
        {
            // Most significant byte of token defines type
            var index = BitConverter.IsLittleEndian ? 3 : 0;
            return (CilMetadataTokenType)bytes[index];
        }

        private sealed class GenericContext
        {
            public Type[] TypeArguments { get; }

            public Type[] MethodArguments { get; }

            public GenericContext(Type[] typeArguments, Type[] methodArguments)
            {
                TypeArguments = typeArguments ?? throw new ArgumentNullException(nameof(typeArguments));
                MethodArguments = methodArguments ?? throw new ArgumentNullException(nameof(methodArguments));
            }
        }
    }
}