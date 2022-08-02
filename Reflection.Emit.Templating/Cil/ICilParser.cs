using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilParser
    {
        public ICilMethodBody ParseMethodBody(MethodInfo method);
    }
}