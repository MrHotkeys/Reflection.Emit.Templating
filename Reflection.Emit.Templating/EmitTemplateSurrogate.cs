using System;
using System.Reflection;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public sealed class EmitTemplateSurrogate
    {
        public static EmitTemplateSurrogate Instance { get; } = new EmitTemplateSurrogate();

        private EmitTemplateSurrogate()
        { }

        public T Get<T>(FieldInfo field) =>
            throw new InvalidOperationException();

        public T Get<T>(PropertyInfo property) =>
            throw new InvalidOperationException();

        public void Set<T>(FieldInfo field, T value) =>
            throw new InvalidOperationException();

        public void Set<T>(PropertyInfo property, T value) =>
            throw new InvalidOperationException();

        public void Call<T>(MethodInfo method, T arg) =>
            throw new InvalidOperationException();
    }
}