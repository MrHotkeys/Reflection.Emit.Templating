using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public sealed class CilLabel : ICilLabel
    {
        public string Name { get; }

        public CilLabel(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString() => $"==== {Name} ====";
    }
}