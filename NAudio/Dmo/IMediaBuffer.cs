using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// IMediaBuffer Interface
    /// </summary>
    [ComImport,
    System.Security.SuppressUnmanagedCodeSecurity,
    Guid("59eff8b9-938c-4a26-82f2-95cb84cdc837"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMediaBuffer
    {
        /// <summary>
        /// Set Length
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>HRESULT</returns>
        [PreserveSig]
        int SetLength(int length);
        
        /// <summary>
        /// Get Max Length
        /// </summary>
        /// <param name="maxLength">Max Length</param>
        /// <returns>HRESULT</returns>
        [PreserveSig]
        int GetMaxLength(out int maxLength);
        
        /// <summary>
        /// Get Buffer and Length
        /// </summary>
        /// <param name="bufferPointerPointer">Pointer to variable into which to write the Buffer Pointer </param>
        /// <param name="validDataLengthPointer">Pointer to variable into which to write the Valid Data Length</param>
        /// <returns>HRESULT</returns>
        [PreserveSig]
        int GetBufferAndLength(IntPtr bufferPointerPointer, IntPtr validDataLengthPointer);
    }
}
