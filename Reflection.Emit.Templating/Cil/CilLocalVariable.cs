using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public sealed class CilLocalVariable : ICilLocalVariable
    {
        private Type _type;

        public Type Type
        {
            get => _type;
            set => _type = value ?? throw new ArgumentNullException(nameof(Type));
        }

        public string Name { get; }

        public bool IsPinned { get; set; }

        public CilLocalVariable(Type type, string name)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString() => $"Local {Type}" + (IsPinned ? " (pinned)" : "");
    }
}