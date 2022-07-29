using System;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilStoreArgumentInstruction : ICilInstruction, IHasArgumentIndexOperand
    {
        public CilInstructionType InstructionType => CilInstructionType.StoreArgument;

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

        public StackBehaviour StackBehaviourPop => StackBehaviour.Pop1;

        public StackBehaviour StackBehaviourPush => StackBehaviour.Push0;

        public CilStoreArgumentInstruction(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            _index = index;
        }

        public override string ToString() => $"Store Argument {Index}";
    }
}