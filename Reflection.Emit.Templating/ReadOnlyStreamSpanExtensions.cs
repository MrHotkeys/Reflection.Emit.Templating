using System;
using System.Runtime.InteropServices;

namespace MrHotkeys.Reflection.Emit.Templating
{
    public static class ReadOnlyStreamSpanExtensions
    {
        public static T Take<T>(ref this ReadOnlyStreamSpan<byte> window)
            where T : unmanaged
        {
            return Take<T>(ref window, BitConverter.IsLittleEndian);
        }

        public static T Take<T>(ref this ReadOnlyStreamSpan<byte> window, bool littleEndian)
            where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            if (size > window.Length)
                throw new ArgumentException($"Not enough bytes remaining to take {typeof(T).Name} (need {size}, found {window.Length})!");

            Span<byte> bytes = stackalloc byte[size];

            window.Take(size).CopyTo(bytes);

            if (littleEndian != BitConverter.IsLittleEndian)
                bytes.Reverse();

            return MemoryMarshal.Cast<byte, T>(bytes)[0];
        }
    }
}