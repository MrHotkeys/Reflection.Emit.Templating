using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilParser
    {
        public ICilMethodBody ParseMethodBody(MethodInfo method);

        public OpCode ParseOpCode(ref ReadOnlySpan<byte> bytes);

        public object? ParseOperand(OpCode opCode, ref ReadOnlySpan<byte> bytes, Module module);

        public byte ParseOperandByte(ref ReadOnlySpan<byte> bytes);

        public double ParseOperandDouble(ref ReadOnlySpan<byte> bytes);

        public FieldInfo ParseOperandField(ref ReadOnlySpan<byte> bytes, Module module);

        public float ParseOperandFloat(ref ReadOnlySpan<byte> bytes);

        public int ParseOperandInt(ref ReadOnlySpan<byte> bytes);

        public long ParseOperandLong(ref ReadOnlySpan<byte> bytes);

        public MethodBase ParseOperandMethod(ref ReadOnlySpan<byte> bytes, Module module);

        public sbyte ParseOperandSByte(ref ReadOnlySpan<byte> bytes);

        public byte[] ParseOperandSignature(ref ReadOnlySpan<byte> bytes, Module module);

        public string ParseOperandString(ref ReadOnlySpan<byte> bytes, Module module);

        public Type ParseOperandType(ref ReadOnlySpan<byte> bytes, Module module);

        public uint ParseOperandUInt(ref ReadOnlySpan<byte> bytes);

        public ushort ParseOperandUShort(ref ReadOnlySpan<byte> bytes);
    }
}