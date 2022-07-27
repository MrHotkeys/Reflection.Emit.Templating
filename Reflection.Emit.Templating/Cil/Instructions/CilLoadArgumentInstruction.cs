using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilLoadArgumentInstruction : ICilInstruction, IHasArgumentIndexOperand
    {
        public CilInstructionType InstructionType => CilInstructionType.LoadArgument;

        private int _index;

        public int Index
        {
            get => _index;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Index));
                _index = value;
            }
        }

        object? ICilInstruction.Operand
        {
            get => _index;
            set
            {
                var x = (int)(value ?? throw new ArgumentNullException(nameof(ICilInstruction.Operand)));
                if (x < 0)
                    throw new ArgumentOutOfRangeException(nameof(ICilInstruction.Operand));
                _index = x;
            }
        }

        public CilLoadArgumentInstruction(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            _index = index;
        }

        public override string ToString() => $"Load Argument {Index}";
    }
}