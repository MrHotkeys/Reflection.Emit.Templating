namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public interface IHasLabelOperand : ICilInstruction
    {

        CilOperandType ICilInstruction.OperandType => CilOperandType.Label;

        public ICilLabel Label { get; set; }
    }
}