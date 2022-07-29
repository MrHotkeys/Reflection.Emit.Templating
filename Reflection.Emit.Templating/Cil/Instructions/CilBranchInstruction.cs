using System;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil.Instructions
{
    public sealed class CilBranchInstruction : ICilInstruction, IHasLabelOperand
    {
        public CilInstructionType InstructionType => CilInstructionType.Branch;

        private ICilLabel _label;

        public ICilLabel Label
        {
            get => _label;
            set => _label = value ?? throw new ArgumentNullException(nameof(Label));
        }

        object? ICilInstruction.Operand
        {
            get => _label;
            set => _label = (ICilLabel)(value ?? throw new ArgumentNullException(nameof(ICilInstruction.Operand)));
        }

        public CilBranchCondition Condition { get; set; }

        public StackBehaviour StackBehaviourPop => Condition switch
        {
            CilBranchCondition.Always => StackBehaviour.Pop0,
            CilBranchCondition.True or CilBranchCondition.False => StackBehaviour.Pop1,
            _ => StackBehaviour.Pop1_pop1,
        };

        public StackBehaviour StackBehaviourPush => StackBehaviour.Push0;

        public CilBranchInstruction(ICilLabel label)
        {
            _label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public override string ToString() => $"Branch {Condition} -> {Label.Name}";
    }
}