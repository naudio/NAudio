using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFMediaBuffer
    /// http://msdn.microsoft.com/en-gb/library/windows/desktop/ms696261%28v=vs.85%29.aspx
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
    public interface IMFMediaBuffer
    {
        /// <summary>
        /// Gives the caller access to the memory in the buffer.
        /// </summary>
        void Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);
        /// <summary>
        /// Unlocks a buffer that was previously locked.
        /// </summary>
        void Unlock();
        /// <summary>
        /// Retrieves the length of the valid data in the buffer.
        /// </summary>
        void GetCurrentLength(out int pcbCurrentLength);
        /// <summary>
        /// Sets the length of the valid data in the buffer.
        /// </summary>
        void SetCurrentLength(int cbCurrentLength);
        /// <summary>
        /// Retrieves the allocated size of the buffer.
        /// </summary>
        void GetMaxLength(out int pcbMaxLength);
    }
}