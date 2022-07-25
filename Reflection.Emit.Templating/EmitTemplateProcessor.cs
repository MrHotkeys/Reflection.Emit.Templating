using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public sealed class EmitTemplateProcessor
    {
        private ILogger Logger { get; }

        private Dictionary<short, OpCode> OpCodeLookup { get; }

        public EmitTemplateProcessor(ILogger<EmitTemplateProcessor> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OpCodeLookup = typeof(OpCodes)
                .GetFields()
                .Select(f => f.GetValue(null))
                .Cast<OpCode>()
                .ToDictionary(c => c.Value);
        }

        public void Process(ILGenerator outIL, bool outIsStatic, Func<EmitTemplateSurrogate, Delegate> callback)
        {
            var template = callback(EmitTemplateSurrogate.Instance);
            var templateInfo = template.GetMethodInfo();
            var templateBody = templateInfo.GetMethodBody() ?? throw new InvalidOperationException();
            var templateIL = (ReadOnlySpan<byte>)templateBody.GetILAsByteArray().AsSpan();

            var captureLookup = new Dictionary<MemberInfo, object?>();
            if (callback.Target is not null)
                AddCaptures(callback.Target, captureLookup);
            if (template.Target is not null)
                AddCaptures(template.Target, captureLookup);

            var localLookup = new Dictionary<int, LocalBuilder>(templateBody.LocalVariables.Count);
            foreach (var localIn in templateBody.LocalVariables)
            {
                var localBuilder = outIL.DeclareLocal(localIn.LocalType);
                localLookup.Add(localIn.LocalIndex, localBuilder);
            }

            var argIndexOffset = GetArgIndexOffset(templateInfo.IsStatic, outIsStatic);

            var contextStack = new Stack<List<Action>>();
            var memberStack = new Stack<MemberInfo>();
            var lastBytesLength = templateIL.Length;
            var bytesRead = 0;
            while (templateIL.Length > 1) // Skip final ret
            {
                var opCode = ParseOpCode(ref templateIL);
                var opCodeName = (OpCodeName)opCode.Value;

                bytesRead += lastBytesLength - templateIL.Length;
                lastBytesLength = templateIL.Length;

                switch (opCodeName)
                {
                    case OpCodeName.Ldarg_0 when (!templateInfo.IsStatic):
                        EmitCapturesAccess(outIL, ref templateIL, callback, templateInfo, captureLookup, memberStack, contextStack);
                        break;

                    case OpCodeName.Call:
                    case OpCodeName.Callvirt:
                    case OpCodeName.Calli:
                        EmitCall(outIL, opCode, ref templateIL, templateInfo.Module, memberStack, contextStack);
                        break;

                    case OpCodeName.Ldarg_0:
                        EmitLdarg(outIL, 0 + argIndexOffset);
                        break;
                    case OpCodeName.Ldarg_1:
                        EmitLdarg(outIL, 0 + argIndexOffset);
                        break;
                    case OpCodeName.Ldarg_2:
                        EmitLdarg(outIL, 0 + argIndexOffset);
                        break;
                    case OpCodeName.Ldarg_3:
                        EmitLdarg(outIL, 0 + argIndexOffset);
                        break;
                    case OpCodeName.Ldarg_S:
                        EmitLdarg(outIL, ParseOperandByte(ref templateIL) + argIndexOffset);
                        break;
                    case OpCodeName.Ldarg:
                        EmitLdarg(outIL, ParseOperandInt(ref templateIL) + argIndexOffset);
                        break;

                    case OpCodeName.Starg_S:
                        EmitStarg(outIL, ParseOperandByte(ref templateIL) + argIndexOffset);
                        break;
                    case OpCodeName.Starg:
                        EmitStarg(outIL, ParseOperandInt(ref templateIL) + argIndexOffset);
                        break;

                    case OpCodeName.Ldarga_S:
                        EmitLdarga(outIL, ParseOperandByte(ref templateIL) + argIndexOffset);
                        break;
                    case OpCodeName.Ldarga:
                        EmitLdarga(outIL, ParseOperandInt(ref templateIL) + argIndexOffset);
                        break;

                    case OpCodeName.Ldloc_0:
                        EmitLdloc(outIL, localLookup[0]);
                        break;
                    case OpCodeName.Ldloc_1:
                        EmitLdloc(outIL, localLookup[1]);
                        break;
                    case OpCodeName.Ldloc_2:
                        EmitLdloc(outIL, localLookup[2]);
                        break;
                    case OpCodeName.Ldloc_3:
                        EmitLdloc(outIL, localLookup[3]);
                        break;
                    case OpCodeName.Ldloc_S:
                        EmitLdloc(outIL, localLookup[ParseOperandByte(ref templateIL)]);
                        break;
                    case OpCodeName.Ldloc:
                        EmitLdloc(outIL, localLookup[ParseOperandInt(ref templateIL)]);
                        break;

                    case OpCodeName.Stloc_0:
                        EmitStloc(outIL, localLookup[0]);
                        break;
                    case OpCodeName.Stloc_1:
                        EmitStloc(outIL, localLookup[1]);
                        break;
                    case OpCodeName.Stloc_2:
                        EmitStloc(outIL, localLookup[2]);
                        break;
                    case OpCodeName.Stloc_3:
                        EmitStloc(outIL, localLookup[3]);
                        break;
                    case OpCodeName.Stloc_S:
                        EmitStloc(outIL, localLookup[ParseOperandByte(ref templateIL)]);
                        break;
                    case OpCodeName.Stloc:
                        EmitStloc(outIL, localLookup[ParseOperandInt(ref templateIL)]);
                        break;

                    default:
                        EmitAsIs(outIL, opCode, templateIL, templateInfo.Module);
                        break;
                }
            }
        }

        private void EmitCapturesAccess(ILGenerator outIL, ref ReadOnlySpan<byte> templateIL,
            Delegate callback, MethodInfo templateInfo, Dictionary<MemberInfo, object?> captureLookup, Stack<MemberInfo> memberStack, Stack<List<Action>> contextStack)
        {
            // If here, we're loading outer capture
            if (templateIL[0] == OpCodes.Ldfld.Value)
            {
                templateIL = templateIL.Slice(1);
                var token = ParseOperandInt(ref templateIL);

                var field = templateInfo
                    .Module
                    .ResolveField(token)
                    ?? throw new InvalidOperationException();

                if (field.FieldType == typeof(EmitTemplateSurrogate))
                {
                    contextStack.Push(new List<Action>());
                }
                else
                {
                    // Check if we're in outer or inner capture
                    if (field.FieldType == callback.Target?.GetType())
                    {
                        // Load inner capture from outer capture
                        if (templateIL[0] != OpCodes.Ldfld.Value)
                            throw new InvalidOperationException();

                        templateIL = templateIL.Slice(1);
                        token = ParseOperandInt(ref templateIL);

                        field = templateInfo
                            .Module
                            .ResolveField(token)
                            ?? throw new InvalidOperationException();
                    }

                    if (field.DeclaringType != callback.Target?.GetType())
                        throw new InvalidOperationException();

                    var capture = captureLookup[field];

                    if (capture is MemberInfo)
                    {

                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        // private void EmitLdfld(ILGenerator outIL, OpCode templateOpCode, ref ReadOnlySpan<byte> templateIL,
        //     Module templateModule, Dictionary<MemberInfo, object?> captureLookup, Stack<MemberInfo> memberStack)
        // {
        //     var field = ParseOperandField(ref templateIL, templateModule);
        //     if (field.)

        // }

        private void EmitCall(ILGenerator outIL, OpCode templateOpCode, ref ReadOnlySpan<byte> templateIL,
            Module templateModule, Stack<MemberInfo> memberStack, Stack<List<Action>> contextStack)
        {
            var method = (MethodInfo)ParseOperandMethod(ref templateIL, templateModule);
            if (method.Name == nameof(EmitTemplateSurrogate.Get))
            {
                var context = contextStack.Pop();
                foreach (var action in context)
                    action();

                var member = memberStack.Pop();
                switch (member.MemberType)
                {
                    case MemberTypes.Field when member is FieldInfo field:
                        outIL.Emit(OpCodes.Ldfld, field);
                        break;
                    case MemberTypes.Property when member is PropertyInfo property:
                        outIL.Emit(OpCodes.Callvirt, property.GetMethod);
                        break;
                    default:
                        throw new Exception();
                }
            }
            else if (method.Name == nameof(EmitTemplateSurrogate.Set))
            {
                var context = contextStack.Pop();
                foreach (var action in context)
                    action();

                var member = memberStack.Pop();
                switch (member.MemberType)
                {
                    case MemberTypes.Field when member is FieldInfo field:
                        outIL.Emit(OpCodes.Stfld, field);
                        break;
                    case MemberTypes.Property when member is PropertyInfo property:
                        outIL.Emit(OpCodes.Callvirt, property.SetMethod);
                        break;
                    default:
                        throw new Exception();
                }
            }
            else if (method.Name == nameof(EmitTemplateSurrogate.Call))
            {
                var context = contextStack.Pop();
                foreach (var action in context)
                    action();

                var capturedMethod = (MethodInfo)memberStack.Pop();
                if (capturedMethod.IsVirtual || capturedMethod.IsAbstract)
                    outIL.Emit(OpCodes.Callvirt, capturedMethod);
                else
                    outIL.Emit(OpCodes.Call, capturedMethod);
            }
            else
            {
                EmitAndTrace(outIL, templateOpCode, method);
            }
        }

        private int GetArgIndexOffset(bool templateIsStatic, bool destinationIsStatic)
        {
            if (templateIsStatic == destinationIsStatic)
                return 0;
            else if (templateIsStatic)
                return -1;
            else
                return 1;
        }

        private void AddCaptures(object target, Dictionary<MemberInfo, object?> captures)
        {
            foreach (var member in target.GetType().GetMembers())
            {
                object? value;
                switch (member.MemberType)
                {
                    case MemberTypes.Field when member is FieldInfo field:
                        value = field.GetValue(target);
                        break;
                    case MemberTypes.Property when member is PropertyInfo property:
                        value = property.GetValue(target);
                        break;
                    default:
                        continue;
                }

                captures[member] = value;
            }
        }

        private OpCode ParseOpCode(ref ReadOnlySpan<byte> bytes)
        {
            var key = (short)bytes[0];
            if (key != 0xFE)
            {
                bytes = bytes.Slice(1);
            }
            else
            {
                key = unchecked((short)((key << 8) + bytes[1]));
                bytes = bytes.Slice(2);
            }

            return OpCodeLookup.TryGetValue(key, out var opCode) ?
                opCode :
                throw new InvalidOperationException();
        }

        private byte ParseOperandByte(ref ReadOnlySpan<byte> templateIL)
        {
            var value = templateIL[0];
            templateIL = templateIL.Slice(sizeof(byte));
            return value;
        }

        private sbyte ParseOperandSByte(ref ReadOnlySpan<byte> templateIL)
        {
            var value = unchecked((sbyte)templateIL[0]);
            templateIL = templateIL.Slice(sizeof(sbyte));
            return value;
        }

        private ushort ParseOperandUShort(ref ReadOnlySpan<byte> templateIL)
        {
            var value = BitConverter.ToUInt16(templateIL);
            templateIL = templateIL.Slice(sizeof(ushort));
            return value;
        }

        private int ParseOperandInt(ref ReadOnlySpan<byte> templateIL)
        {
            var value = BitConverter.ToInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(int));
            return value;
        }

        private uint ParseOperandUInt(ref ReadOnlySpan<byte> templateIL)
        {
            var value = BitConverter.ToUInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(uint));
            return value;
        }

        private long ParseOperandLong(ref ReadOnlySpan<byte> templateIL)
        {
            var value = BitConverter.ToInt64(templateIL);
            templateIL = templateIL.Slice(sizeof(long));
            return value;
        }

        private float ParseOperandFloat(ref ReadOnlySpan<byte> templateIL)
        {
            var value = BitConverter.ToSingle(templateIL);
            templateIL = templateIL.Slice(sizeof(float));
            return value;
        }

        private double ParseOperandDouble(ref ReadOnlySpan<byte> templateIL)
        {
            var value = BitConverter.ToDouble(templateIL);
            templateIL = templateIL.Slice(sizeof(double));
            return value;
        }

        private string ParseOperandString(ref ReadOnlySpan<byte> templateIL, Module module)
        {
            var token = BitConverter.ToInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(int));
            return module.ResolveString(token);
        }

        private Type ParseOperandType(ref ReadOnlySpan<byte> templateIL, Module module)
        {
            var token = BitConverter.ToInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(int));
            return module.ResolveType(token);
        }

        private FieldInfo ParseOperandField(ref ReadOnlySpan<byte> templateIL, Module module)
        {
            var token = BitConverter.ToInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(int));
            return module.ResolveField(token);
        }

        private MethodBase ParseOperandMethod(ref ReadOnlySpan<byte> templateIL, Module module)
        {
            var token = BitConverter.ToInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(int));
            return module.ResolveMethod(token);
        }

        private byte[] ParseOperandSignature(ref ReadOnlySpan<byte> templateIL, Module module)
        {
            var token = BitConverter.ToInt32(templateIL);
            templateIL = templateIL.Slice(sizeof(int));
            return module.ResolveSignature(token);
        }

        private void EmitLdarg(ILGenerator outIL, int index)
        {
            switch (index)
            {
                case 0:
                    EmitAndTrace(outIL, OpCodes.Ldarg_0);
                    break;
                case 1:
                    EmitAndTrace(outIL, OpCodes.Ldarg_1);
                    break;
                case 2:
                    EmitAndTrace(outIL, OpCodes.Ldarg_2);
                    break;
                case 3:
                    EmitAndTrace(outIL, OpCodes.Ldarg_3);
                    break;
                default:
                    if (index < byte.MaxValue)
                        EmitAndTrace(outIL, OpCodes.Ldarg_S, (byte)index);
                    else if (index > 0)
                        EmitAndTrace(outIL, OpCodes.Ldarg, index);
                    else
                        throw new InvalidOperationException();
                    break;
            }
        }

        private void EmitStarg(ILGenerator outIL, int index)
        {
            if (index < byte.MaxValue)
                EmitAndTrace(outIL, OpCodes.Starg_S, (byte)index);
            else if (index > 0)
                EmitAndTrace(outIL, OpCodes.Starg, index);
            else
                throw new InvalidOperationException();
        }

        private void EmitLdarga(ILGenerator outIL, int index)
        {
            if (index < byte.MaxValue)
                EmitAndTrace(outIL, OpCodes.Ldarga_S, (byte)index);
            else if (index > 0)
                EmitAndTrace(outIL, OpCodes.Ldarg, index);
            else
                throw new InvalidOperationException();
        }

        private void EmitLdloc(ILGenerator outIL, LocalBuilder local)
        {
            var index = local.LocalIndex;
            switch (index)
            {
                case 0:
                    EmitAndTrace(outIL, OpCodes.Ldloc_0);
                    break;
                case 1:
                    EmitAndTrace(outIL, OpCodes.Ldloc_1);
                    break;
                case 2:
                    EmitAndTrace(outIL, OpCodes.Ldloc_2);
                    break;
                case 3:
                    EmitAndTrace(outIL, OpCodes.Ldloc_3);
                    break;
                default:
                    if (index < byte.MaxValue)
                        EmitAndTrace(outIL, OpCodes.Ldloc_S, (byte)index);
                    else if (index > 0)
                        EmitAndTrace(outIL, OpCodes.Ldloc, index);
                    else
                        throw new InvalidOperationException();
                    break;
            }
        }

        private void EmitStloc(ILGenerator outIL, LocalBuilder local)
        {
            var index = local.LocalIndex;
            switch (index)
            {
                case 0:
                    EmitAndTrace(outIL, OpCodes.Stloc_0);
                    break;
                case 1:
                    EmitAndTrace(outIL, OpCodes.Stloc_1);
                    break;
                case 2:
                    EmitAndTrace(outIL, OpCodes.Stloc_2);
                    break;
                case 3:
                    EmitAndTrace(outIL, OpCodes.Stloc_3);
                    break;
                default:
                    if (index < byte.MaxValue)
                        EmitAndTrace(outIL, OpCodes.Stloc_S, (byte)index);
                    else if (index > 0)
                        EmitAndTrace(outIL, OpCodes.Stloc, index);
                    else
                        throw new InvalidOperationException();
                    break;
            }
        }

        private void EmitAsIs(ILGenerator outIL, OpCode templateOpCode, ReadOnlySpan<byte> templateIL, Module templateModule)
        {
            switch (templateOpCode.OperandType)
            {
                case OperandType.InlineNone:
                    EmitAndTrace(outIL, templateOpCode);
                    break;

                case OperandType.ShortInlineI when (templateOpCode == OpCodes.Ldc_I4):
                    EmitAndTrace(outIL, templateOpCode, ParseOperandSByte(ref templateIL));
                    break;

                case OperandType.ShortInlineI:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandByte(ref templateIL));
                    break;
                case OperandType.InlineI:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandInt(ref templateIL));
                    break;
                case OperandType.InlineI8:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandLong(ref templateIL));
                    break;

                case OperandType.ShortInlineR:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandFloat(ref templateIL));
                    break;
                case OperandType.InlineR:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandDouble(ref templateIL));
                    break;

                case OperandType.InlineString:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandString(ref templateIL, templateModule));
                    break;

                case OperandType.InlineType:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandType(ref templateIL, templateModule));
                    break;
                case OperandType.InlineField:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandField(ref templateIL, templateModule));
                    break;
                case OperandType.InlineMethod:
                    EmitAndTrace(outIL, templateOpCode, (MethodInfo)ParseOperandMethod(ref templateIL, templateModule));
                    break;

                case OperandType.InlineSig:
                    //EmitAndTrace(outIL, templateOpCode, ParseOperandSignature(ref templateIL, templateModule));
                    //break;
                    throw new NotImplementedException();

                case OperandType.ShortInlineVar:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandByte(ref templateIL));
                    break;
                case OperandType.InlineVar:
                    EmitAndTrace(outIL, templateOpCode, ParseOperandUShort(ref templateIL));
                    break;

                case OperandType.InlineTok:
                    throw new NotImplementedException();

                case OperandType.InlinePhi:
                    throw new NotSupportedException();
                default:
                    throw new InvalidOperationException();
            }
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace(opCode.Name);
            outIL.Emit(opCode);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, byte operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, int operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, long operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}L");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, float operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}f");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, double operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}d");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, string operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} \"{operand}\"");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, Type operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, FieldInfo operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}");
            outIL.Emit(opCode, operand);
        }

        private void EmitAndTrace(ILGenerator outIL, OpCode opCode, MethodInfo operand)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
                Logger.LogTrace($"{opCode.Name} {operand}");
            outIL.Emit(opCode, operand);
        }
    }
}