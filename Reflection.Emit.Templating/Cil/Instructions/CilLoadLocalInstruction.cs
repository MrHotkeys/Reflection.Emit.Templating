using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilLoadLocalInstruction : ICilInstruction, IHasLocalOperand
    {
        public CilInstructionType InstructionType => CilInstructionType.LoadLocal;

        private ICilLocalVariable _local;

        public ICilLocalVariable Local
        {
            get => _local;
            set => _local = value ?? throw new ArgumentNullException(nameof(Local));
        }

        object? ICilInstruction.Operand
        {
            get => _local;
            set => _local = (ICilLocalVariable)(value ?? throw new ArgumentNullException(nameof(ICilInstruction.Operand)));
        }

        public CilLoadLocalInstruction(ICilLocalVariable local)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
        }

        public override string ToString() => $"Load Local {Local.Name}";
    }
}