using System;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilReturnInstruction : ICilInstruction
    {
        public CilInstructionType InstructionType => CilInstructionType.Return;

        CilOperandType ICilInstruction.OperandType => CilOperandType.None;

        public StackBehaviour StackBehaviourPop => StackBehaviour.Varpop;

        public StackBehaviour StackBehaviourPush => StackBehaviour.Push0;

        object? ICilInstruction.Operand
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public StackBehaviour StackBehaviour => throw new NotImplementedException();

        public CilReturnInstruction()
        { }

        public override string ToString() => "Return";
    }
}