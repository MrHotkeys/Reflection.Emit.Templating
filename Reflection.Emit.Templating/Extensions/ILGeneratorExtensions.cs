using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Extensions
{
    public static class ILGeneratorExtensions
    {
        public static void EmitCode(this ILGenerator il, Func<EmitTemplateSurrogate, Delegate> callback)
        {
            EmitTemplating.TemplateProcessor.Process(il, false, callback);
        }
        public static void EmitCodeStatic(this ILGenerator il, Func<EmitTemplateSurrogate, Delegate> callback)
        {
            EmitTemplating.TemplateProcessor.Process(il, true, callback);
        }

        /// <summary>
        /// Pops a string off the stack, and uses it as the comparison value for a string jump table.
        /// </summary>
        /// <param name="il">The IL generator to use.</param>
        /// <param name="caseValues">0 or more test values to use for the cases.</param>
        /// <param name="emitCaseCallback">A callback to emit code for each test case. The value for the test case
        ///     is given as the second parameter. Should return true if a break should be emitted by this method, or
        ///     false if not necessary (e.g. if a return was emitted).</param>
        /// <param name="emitDefaultCallback">A callback to emit code for the default case. Should return true if a break
        ///     should be emitted by this method, or false if not necessary (e.g. if a return was emitted).</param>
        /// <exception cref="ArgumentNullException"><paramref name="il"/>, <paramref name="caseValues"/>,
        ///     <paramref name="emitCaseCallback"/>, or <paramref name="emitDefaultCallback"/> is null.</exception>
        public static void EmitStringJumpTable(this ILGenerator il, IEnumerable<string> caseValues,
            Func<ILGenerator, string, bool> emitCaseCallback, Func<ILGenerator, bool> emitDefaultCallback)
        {
            if (il is null)
                throw new ArgumentNullException(nameof(il));
            if (caseValues is null)
                throw new ArgumentNullException(nameof(caseValues));
            if (emitCaseCallback is null)
                throw new ArgumentNullException(nameof(emitCaseCallback));
            if (emitDefaultCallback is null)
                throw new ArgumentNullException(nameof(emitDefaultCallback));

            var jumpTable = new List<(Label, string)>();

            var switchValueLocal = il.DeclareLocal(typeof(string));
            il.Emit(OpCodes.Stloc, switchValueLocal);

            var stringEqualsMethod = typeof(string)
                .GetMethod(
                    name: nameof(string.Equals),
                    genericParameterCount: 0,
                    bindingAttr: BindingFlags.Public | BindingFlags.Static,
                    types: new[] { typeof(string), typeof(string) }
                ) ?? throw new InvalidOperationException();
            foreach (var caseValue in caseValues)
            {
                var caseLabel = il.DefineLabel();
                jumpTable.Add((caseLabel, caseValue));

                il.Emit(OpCodes.Ldloc, switchValueLocal);
                il.Emit(OpCodes.Ldstr, caseValue);
                il.Emit(OpCodes.Call, stringEqualsMethod);
                il.Emit(OpCodes.Brtrue, caseLabel);
            }

            var defaultLabel = il.DefineLabel();
            il.Emit(OpCodes.Br, defaultLabel);

            var breakLabel = il.DefineLabel();

            foreach (var (caseLabel, caseValue) in jumpTable)
            {
                il.MarkLabel(caseLabel);

                if (emitCaseCallback(il, caseValue))
                    il.Emit(OpCodes.Br, breakLabel);
            }

            il.MarkLabel(defaultLabel);
            if (emitDefaultCallback(il))
                il.Emit(OpCodes.Br, breakLabel);

            il.MarkLabel(breakLabel);
        }
    }
}