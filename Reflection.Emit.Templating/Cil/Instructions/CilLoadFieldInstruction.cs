using System;
using System.Reflection;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilLoadFieldInstruction : ICilInstruction, IHasFieldOperand
    {
        CilInstructionType ICilInstruction.InstructionType => CilInstructionType.LoadField;

        private FieldInfo _field;

        public FieldInfo Field
        {
            get => _field;
            set => _field = value ?? throw new ArgumentNullException(nameof(Field));
        }

        object? ICilInstruction.Operand
        {
            get => _field;
            set => _field = (FieldInfo)(value ?? throw new ArgumentNullException(nameof(ICilInstruction.Operand)));
        }

        public CilLoadFieldInstruction(FieldInfo field)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public override string ToString() => $"Load Field {Field}";
    }
}