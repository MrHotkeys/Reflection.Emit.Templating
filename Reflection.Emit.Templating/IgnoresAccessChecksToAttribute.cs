namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public string AssemblyName { get; }

        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        }
    }
}