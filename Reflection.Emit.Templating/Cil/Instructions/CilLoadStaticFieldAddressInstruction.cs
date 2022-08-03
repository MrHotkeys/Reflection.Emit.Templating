using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public class CilLoadStaticFieldAddressInstruction : ICilInstruction, IHasFieldOperand
    {
        CilInstructionType ICilInstruction.InstructionType => CilInstructionType.LoadStaticFieldAddress;

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

        public StackBehaviour StackBehaviourPop => StackBehaviour.Pop0;

        public StackBehaviour StackBehaviourPush => StackBehaviour.Push1;

        public CilLoadStaticFieldAddressInstruction(FieldInfo field)
        {
            if (field is null)
                throw new ArgumentNullException(nameof(field));
            if (!field.IsStatic)
                throw new ArgumentException("Must be static!", nameof(field));

            _field = field;
        }

        public override string ToString() => $"Load Static Field Address {Field}";
    }
}