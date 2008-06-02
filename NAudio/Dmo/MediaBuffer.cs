using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Dmo
{
    /// <summary>
    /// Attempting to implement the COM IMediaBuffer interface as a .NET object
    /// Not sure what will happen when I pass this to an unmanaged object
    /// </summary>
    public class MediaBuffer : IMediaBuffer, IDisposable
    {
        IntPtr buffer;
        int length;
        int maxLength;
        
        /// <summary>
        /// Creates a new Media Buffer
        /// </summary>
        /// <param name="maxLength">Maximum length in bytes</param>
        public MediaBuffer(int maxLength)
        {
            this.buffer = Marshal.AllocCoTaskMem(maxLength);
            this.maxLength = maxLength;
        }

        /// <summary>
        /// Dispose and free memory for buffer
        /// </summary>
        public void Dispose()
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(buffer);
                buffer = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MediaBuffer()
        {
            Dispose();
        }

        #region IMediaBuffer Members

        /// <summary>
        /// Set length of valid data in the buffer
        /// </summary>
        /// <param name="length">length</param>
        /// <returns>HRESULT</returns>
        public int SetLength(int length)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Set Length {0}", length));
            if (length > maxLength)
            {
                return HResult.E_INVALIDARG;
            }
            this.length = length;
            return HResult.S_OK;
        }

        /// <summary>
        /// Gets the maximum length of the buffer
        /// </summary>
        /// <param name="maxLength">Max length (output parameter)</param>
        /// <returns>HRESULT</returns>
        public int GetMaxLength(out int maxLength)
        {
            System.Diagnostics.Debug.WriteLine("Get Max Length");
            maxLength = this.maxLength;
            return HResult.S_OK;
        }

        /// <summary>
        /// Gets buffer and / or length
        /// </summary>
        /// <param name="bufferPointerPointer">Pointer to variable into which buffer pointer should be written</param>
        /// <param name="validDataLengthPointer">Pointer to variable into which valid data length should be written</param>
        /// <returns>HRESULT</returns>
        public int GetBufferAndLength(IntPtr bufferPointerPointer, IntPtr validDataLengthPointer)
        {

            System.Diagnostics.Debug.WriteLine(String.Format("Get Buffer and Length {0},{1}",
                bufferPointerPointer,validDataLengthPointer));
            if (bufferPointerPointer != IntPtr.Zero)
            {
                Marshal.WriteIntPtr(bufferPointerPointer, this.buffer);
            }
            if (validDataLengthPointer != IntPtr.Zero)
            {
                Marshal.WriteInt32(validDataLengthPointer, this.length);

            }
            //System.Diagnostics.Debug.WriteLine("Finished Getting Buffer and Length");
            return HResult.S_OK;

        }

        #endregion
    }
}
