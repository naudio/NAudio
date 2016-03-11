using System;
using System.Linq;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
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
            return Marshal.SizeOf<T>();
        }

        /// <summary>
        /// Offset of a field in a structure
        /// </summary>
        public static IntPtr OffsetOf<T>(string fieldName)
        {
            return Marshal.OffsetOf<T>(fieldName);
        }
        
        /// <summary>
        /// Pointer to Structure
        /// </summary>
        public static T PtrToStructure<T>(IntPtr pointer)
        {
            return Marshal.PtrToStructure<T>(pointer);
        }
    }
}
