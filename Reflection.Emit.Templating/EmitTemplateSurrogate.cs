using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public sealed class EmitTemplateSurrogate
    {
        public static EmitTemplateSurrogate Instance { get; } = new EmitTemplateSurrogate();

        private EmitTemplateSurrogate()
        { }

        public T Get<T>(LocalVariableInfo local) =>
            throw new InvalidOperationException();

        public T Get<T>(FieldInfo field) =>
            throw new InvalidOperationException();

        public T Get<T>(PropertyInfo property) =>
            throw new InvalidOperationException();

        public void Set<T>(FieldInfo field, T value) =>
            throw new InvalidOperationException();

        public void Set<T>(LocalVariableInfo local, T value) =>
            throw new InvalidOperationException();

        public void Set<T>(PropertyInfo property, T value) =>
            throw new InvalidOperationException();

        public T Ref<T>(LocalVariableInfo local) =>
            throw new InvalidOperationException();

        public T Ref<T>(FieldInfo field) =>
            throw new InvalidOperationException();

        public void Call<T>(MethodInfo method, T arg) =>
            throw new InvalidOperationException();

        public void Call<T1, T2>(MethodInfo method, T1 arg1, T2 arg2) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, T6>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            throw new InvalidOperationException();

        public void Call<T, TOut>(MethodInfo method, T arg) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, TOut>(MethodInfo method, T1 arg1, T2 arg2) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, TOut>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, TOut>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, TOut>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, T6, TOut>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            throw new InvalidOperationException();

        public void Call(Action action) =>
            throw new InvalidOperationException();

        public void Call<T>(Action<T> action, T arg) =>
            throw new InvalidOperationException();

        public void Call<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            throw new InvalidOperationException();

        public void Call<TOut>(Func<TOut> func) =>
            throw new InvalidOperationException();

        public void Call<T, TOut>(Func<T, TOut> func, T arg) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, TOut>(Func<T1, T2, TOut> func, T1 arg1, T2 arg2) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, TOut>(Func<T1, T2, T3, TOut> func, T1 arg1, T2 arg2, T3 arg3) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, TOut>(Func<T1, T2, T3, T4, TOut> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, TOut>(Func<T1, T2, T3, T4, T5, TOut> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, T6, TOut>(Func<T1, T2, T3, T4, T5, T6, TOut> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            throw new InvalidOperationException();

        public void Call(Delegate del) =>
            throw new InvalidOperationException();

        public void Call<T>(Delegate del, T arg) =>
            throw new InvalidOperationException();

        public void Call<T1, T2>(Delegate del, T1 arg1, T2 arg2) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3>(Delegate del, T1 arg1, T2 arg2, T3 arg3) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4>(Delegate del, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5>(Delegate del, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, T6>(Delegate del, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            throw new InvalidOperationException();

        public void Call<TOut>(Delegate del) =>
            throw new InvalidOperationException();

        public void Call<T, TOut>(Delegate del, T arg) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, TOut>(Delegate del, T1 arg1, T2 arg2) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, TOut>(Delegate del, T1 arg1, T2 arg2, T3 arg3) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, TOut>(Delegate del, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, TOut>(Delegate del, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            throw new InvalidOperationException();

        public void Call<T1, T2, T3, T4, T5, T6, TOut>(Delegate del, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            throw new InvalidOperationException();
    }
}