using System.Reflection;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public interface IHasMethodOperand : ICilInstruction
    {

        CilOperandType ICilInstruction.OperandType => CilOperandType.Method;

        public MethodBase Method { get; set; }
    }
}