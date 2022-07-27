using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilLoadLocalAddressInstruction : ICilInstruction, IHasLocalOperand
    {
        public CilInstructionType InstructionType => CilInstructionType.LoadLocalAddress;

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

        public CilLoadLocalAddressInstruction(ICilLocalVariable local)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
        }

        public override string ToString() => $"Load Local Address {Local.Name}";
    }
}