using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo
{
    /// <summary>
    /// IMediaBuffer Interface.
    /// </summary>
    /// <remarks>
    /// Implemented by the managed <see cref="MediaBuffer"/> class as a callback
    /// surface for DMOs. Source-generated COM (<see cref="GeneratedComInterfaceAttribute"/>)
    /// is used so that CCWs created via
    /// <see cref="System.Runtime.InteropServices.Marshalling.StrategyBasedComWrappers"/>
    /// are compatible with the modern <c>IMediaObject</c> dispatch path.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("59eff8b9-938c-4a26-82f2-95cb84cdc837")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMediaBuffer
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
