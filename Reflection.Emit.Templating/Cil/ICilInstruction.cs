using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilInstruction : ICilToken
    {
        CilTokenType ICilToken.TokenType => CilTokenType.Instruction;

        public CilInstructionType InstructionType { get; }

        public CilOperandType OperandType { get; }

        public StackBehaviour StackBehaviourPop { get; }

        public StackBehaviour StackBehaviourPush { get; }

        public object? Operand { get; set; }
    }
}