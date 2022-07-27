using System.Reflection;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public interface IHasFieldOperand : ICilInstruction
    {
        CilOperandType ICilInstruction.OperandType => CilOperandType.Field;

        public FieldInfo Field { get; set; }
    }
}