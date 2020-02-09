using System;
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
        private IntPtr buffer;
        private int length;
        private readonly int maxLength;
        
        /// <summary>
        /// Creates a new Media Buffer
        /// </summary>
        /// <param name="maxLength">Maximum length in bytes</param>
        public MediaBuffer(int maxLength)
        {
            buffer = Marshal.AllocCoTaskMem(maxLength);
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
        int IMediaBuffer.SetLength(int length)
        {
            //System.Diagnostics.Debug.WriteLine(String.Format("Set Length {0}", length));
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
        int IMediaBuffer.GetMaxLength(out int maxLength)
        {
            //System.Diagnostics.Debug.WriteLine("Get Max Length");
            maxLength = this.maxLength;
            return HResult.S_OK;
        }

        /// <summary>
        /// Gets buffer and / or length
        /// </summary>
        /// <param name="bufferPointerPointer">Pointer to variable into which buffer pointer should be written</param>
        /// <param name="validDataLengthPointer">Pointer to variable into which valid data length should be written</param>
        /// <returns>HRESULT</returns>
        int IMediaBuffer.GetBufferAndLength(IntPtr bufferPointerPointer, IntPtr validDataLengthPointer)
        {

            //System.Diagnostics.Debug.WriteLine(String.Format("Get Buffer and Length {0},{1}",
            //    bufferPointerPointer,validDataLengthPointer));
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

        /// <summary>
        /// Length of data in the media buffer
        /// </summary>
        public int Length
        {
            get { return length; }
            set 
            {
                if (length > maxLength)
                {
                    throw new ArgumentException("Cannot be greater than maximum buffer size");
                }
                length = value; 
            }
        }

        /// <summary>
        /// Loads data into this buffer
        /// </summary>
        /// <param name="data">Data to load</param>
        /// <param name="bytes">Number of bytes to load</param>
        public void LoadData(byte[] data, int bytes)
        {
            this.Length = bytes;
            Marshal.Copy(data, 0, buffer, bytes);
        }

        /// <summary>
        /// Retrieves the data in the output buffer
        /// </summary>
        /// <param name="data">buffer to retrieve into</param>
        /// <param name="offset">offset within that buffer</param>
        public void RetrieveData(byte[] data, int offset)
        {
            Marshal.Copy(buffer, data, offset, Length);
        }
    }
}
