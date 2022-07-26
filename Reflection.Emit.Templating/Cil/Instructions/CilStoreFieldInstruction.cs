using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilStoreFieldInstruction : ICilInstruction, IHasFieldOperand
    {
        CilInstructionType ICilInstruction.InstructionType => CilInstructionType.StoreField;

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

        public StackBehaviour StackBehaviourPop => StackBehaviour.Pop1_pop1;

        public StackBehaviour StackBehaviourPush => StackBehaviour.Push0;

        public CilStoreFieldInstruction(FieldInfo field)
        {
            if (field is null)
                throw new ArgumentNullException(nameof(field));
            if (field.IsStatic)
                throw new ArgumentException("May not be static!", nameof(field));

            _field = field;
        }

        public override string ToString() => $"Store Field {Field}";
    }
}