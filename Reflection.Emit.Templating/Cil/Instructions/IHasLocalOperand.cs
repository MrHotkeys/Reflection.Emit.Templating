namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public interface IHasLocalOperand : ICilInstruction
    {
        CilOperandType ICilInstruction.OperandType => CilOperandType.Local;

        public ICilLocalVariable Local { get; set; }
    }
}