namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public interface IHasArgumentIndexOperand : ICilInstruction
    {
        CilOperandType ICilInstruction.OperandType => CilOperandType.ArgumentIndex;

        public int Index { get; set; }
    }
}