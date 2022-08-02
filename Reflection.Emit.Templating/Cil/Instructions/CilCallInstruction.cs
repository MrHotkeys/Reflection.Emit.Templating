using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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

        public StackBehaviour StackBehaviourPop => StackBehaviour.Varpop;

        public StackBehaviour StackBehaviourPush => StackBehaviour.Varpush;

        public CilCallInstruction(MethodBase method)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public override string ToString()
        {
            try
            {
                var parameterStrings = Method
                    .GetParameters()
                    .Select(p => $"{p.ParameterType} {p.Name}");

                return $"Call {Method.DeclaringType}.{Method.Name}({string.Join(", ", parameterStrings)})";
            }
            catch (NotSupportedException e)
            {
                // TODO: Handle better
                return $"Call {Method.DeclaringType}.{Method.Name}()";
            }
        }
    }
}