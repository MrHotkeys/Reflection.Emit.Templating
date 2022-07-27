using System;
using System.Collections.Generic;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilMethodBody
    {
        public IList<ICilLocalVariable> Locals { get; set; }

        public IList<ICilToken> Tokens { get; set; }
    }
}