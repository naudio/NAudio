using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Utils;

namespace NAudio.Dmo
{
    /// <summary>
    /// Implements the COM IMediaBuffer interface as a managed object backed by CoTaskMem allocation.
    /// </summary>
    [GeneratedComClass]
    public partial class MediaBuffer : IMediaBuffer, IDisposable
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


        #region IMediaBuffer Members

        /// <summary>
        /// Set length of valid data in the buffer
        /// </summary>
        /// <param name="length">length</param>
        /// <returns>HRESULT</returns>
        public int SetLength(int length)
        {
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
            if (bufferPointerPointer != IntPtr.Zero)
            {
                Marshal.WriteIntPtr(bufferPointerPointer, this.buffer);
            }
            if (validDataLengthPointer != IntPtr.Zero)
            {
                Marshal.WriteInt32(validDataLengthPointer, this.length);
            }
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
        public unsafe void LoadData(ReadOnlySpan<byte> data)
        {
            this.Length = data.Length;
            data.CopyTo(new Span<byte>((void*)buffer, maxLength));
        }

        /// <summary>
        /// Retrieves the data in the output buffer into a span
        /// </summary>
        /// <param name="destination">Span to copy data into</param>
        public unsafe void RetrieveData(Span<byte> destination)
        {
            int bytesToCopy = Math.Min(Length, destination.Length);
            new Span<byte>((void*)buffer, bytesToCopy).CopyTo(destination);
        }
    }
}
