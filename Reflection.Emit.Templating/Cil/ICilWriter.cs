using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilWriter
    {
        public void Write(ILGenerator il, ICilMethodBody body);
    }
}