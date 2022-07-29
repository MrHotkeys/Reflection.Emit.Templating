using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilRawInstruction : ICilInstruction
    {
        public CilInstructionType InstructionType => CilInstructionType.Raw;

        public CilOperandType OperandType => CilOperandType.Raw;

        public OpCode OpCode { get; }

        public object? Operand { get; set; }

        public StackBehaviour StackBehaviourPop => OpCode.StackBehaviourPop;

        public StackBehaviour StackBehaviourPush => OpCode.StackBehaviourPush;

        public CilRawInstruction(OpCode opCode)
        {
            OpCode = opCode;
        }

        public CilRawInstruction(OpCode opCode, object? operand)
        {
            OpCode = opCode;
            Operand = operand;
        }

        public override string ToString() => Operand is null ?
            $"Raw: {OpCode}" :
            $"Raw: {OpCode} {Operand}";
    }
}