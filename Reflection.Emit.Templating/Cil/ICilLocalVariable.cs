using System;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilLocalVariable
    {
        public Type Type { get; set; }

        public bool IsPinned { get; set; }

        public string Name { get; }
    }
}