using System;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilStoreLocalInstruction : ICilInstruction, IHasLocalOperand
    {
        public CilInstructionType InstructionType => CilInstructionType.StoreLocal;

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

        public StackBehaviour StackBehaviourPop => StackBehaviour.Pop1;

        public StackBehaviour StackBehaviourPush => StackBehaviour.Push0;

        public CilStoreLocalInstruction(ICilLocalVariable local)
        {
            _local = local ?? throw new ArgumentNullException(nameof(local));
        }

        public override string ToString() => $"Store Local {Local.Name}";
    }
}