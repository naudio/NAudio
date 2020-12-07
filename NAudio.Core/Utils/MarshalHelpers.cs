using System;
using System.Runtime.InteropServices;

namespace NAudio.Utils
{
    /// <summary>
    /// Support for Marshal Methods in both UWP and .NET 3.5
    /// </summary>
    public static class MarshalHelpers
    {
        /// <summary>
        /// SizeOf a structure
        /// </summary>
        public static int SizeOf<T>()
        {
#if NET35
            return Marshal.SizeOf(typeof (T));
#else
            return Marshal.SizeOf<T>();
#endif
        }

        /// <summary>
        /// Offset of a field in a structure
        /// </summary>
        public static IntPtr OffsetOf<T>(string fieldName)
        {
#if NET35
            return Marshal.OffsetOf(typeof(T), fieldName);
#else
            return Marshal.OffsetOf<T>(fieldName);
#endif
        }

        /// <summary>
        /// Pointer to Structure
        /// </summary>
        public static T PtrToStructure<T>(IntPtr pointer)
        {
#if NET35
            return (T)Marshal.PtrToStructure(pointer, typeof(T));
#else
            return Marshal.PtrToStructure<T>(pointer);
#endif
        }
    }
}
