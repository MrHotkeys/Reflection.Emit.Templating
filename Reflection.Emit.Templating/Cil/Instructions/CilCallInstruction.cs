using System;
using System.Reflection;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilCallInstruction : ICilInstruction, IHasMethodOperand
    {
        CilInstructionType ICilInstruction.InstructionType => CilInstructionType.Call;

        private MethodBase _method;

        public MethodBase Method
        {
            get => _method;
            set => _method = value ?? throw new ArgumentNullException(nameof(Method));
        }

        object? ICilInstruction.Operand
        {
            get => _method;
            set => _method = (MethodBase)(value ?? throw new ArgumentNullException(nameof(ICilInstruction.Operand)));
        }

        public CilCallInstruction(MethodBase method)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public override string ToString() => $"Call {Method}";
    }
}