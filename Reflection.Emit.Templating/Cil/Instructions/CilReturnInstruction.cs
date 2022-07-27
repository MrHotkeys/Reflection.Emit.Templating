using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilReturnInstruction : ICilInstruction
    {
        public CilInstructionType InstructionType => CilInstructionType.Return;

        CilOperandType ICilInstruction.OperandType => CilOperandType.None;

        object? ICilInstruction.Operand
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public CilReturnInstruction()
        { }

        public override string ToString() => "Return";
    }
}