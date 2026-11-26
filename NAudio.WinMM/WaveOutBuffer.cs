using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave 
{
    /// <summary>
    /// A buffer of Wave samples for streaming to a Wave Output device
    /// </summary>
    public class WaveOutBuffer : IDisposable
    {
        private readonly WaveHeader header;
        private readonly Int32 bufferSize; // allocated bytes, may not be the same as bytes read
        private readonly byte[] buffer;
        private readonly IWaveProvider waveStream;
        private readonly object waveOutLock;
        private readonly Func<bool> canQueue;
        private GCHandle hBuffer;
        private IntPtr hWaveOut;
        private GCHandle hHeader; // we need to pin the header structure
        private GCHandle hThis; // for the user callback

        /// <summary>
        /// creates a new wavebuffer
        /// </summary>
        /// <param name="hWaveOut">WaveOut device to write to</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <param name="bufferFillStream">Stream to provide more data</param>
        /// <param name="waveOutLock">Lock to protect WaveOut API's from being called on >1 thread</param>
        public WaveOutBuffer(IntPtr hWaveOut, Int32 bufferSize, IWaveProvider bufferFillStream, object waveOutLock)
            : this(hWaveOut, bufferSize, bufferFillStream, waveOutLock, null)
        {
        }

        /// <summary>
        /// creates a new wavebuffer
        /// </summary>
        /// <param name="hWaveOut">WaveOut device to write to</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <param name="bufferFillStream">Stream to provide more data</param>
        /// <param name="waveOutLock">Lock to protect WaveOut API's from being called on >1 thread</param>
        /// <param name="canQueue">
        /// Optional check, evaluated while holding <paramref name="waveOutLock"/> immediately before
        /// waveOutWrite. Returning false skips the write, so an owner whose Stop() changes its state
        /// before calling waveOutReset under the same lock never has a buffer queued again after
        /// the reset (issue #1515).
        /// </param>
        public WaveOutBuffer(IntPtr hWaveOut, Int32 bufferSize, IWaveProvider bufferFillStream, object waveOutLock, Func<bool> canQueue)
        {
            this.canQueue = canQueue;
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
            hBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            this.hWaveOut = hWaveOut;
            waveStream = bufferFillStream;
            this.waveOutLock = waveOutLock;

            header = new WaveHeader();
            hHeader = GCHandle.Alloc(header, GCHandleType.Pinned);
            header.dataBuffer = hBuffer.AddrOfPinnedObject();
            header.bufferLength = bufferSize;
            header.loops = 1;
            hThis = GCHandle.Alloc(this);
            header.userData = (IntPtr)hThis;
            lock (waveOutLock)
            {
                MmException.Try(WaveInterop.waveOutPrepareHeader(hWaveOut, header, Marshal.SizeOf(header)), "waveOutPrepareHeader");
            }
        }

        #region Dispose Pattern

        /// <summary>
        /// Finalizer for this wave buffer
        /// </summary>
        ~WaveOutBuffer()
        {
            Dispose(false);
            System.Diagnostics.Debug.Assert(true, "WaveBuffer was not disposed");
        }

        /// <summary>
        /// Releases resources held by this WaveBuffer
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Releases resources held by this WaveBuffer
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free unmanaged resources
            // Unprepare first: the driver may still hold the header and data buffer, and freeing
            // their pins before it lets the GC move or reuse memory the driver then reads from or
            // writes to (issue #1515).
            if (hWaveOut != IntPtr.Zero)
            {
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutUnprepareHeader(hWaveOut, header, Marshal.SizeOf(header));
                }
                hWaveOut = IntPtr.Zero;
                if (result == MmResult.WaveStillPlaying)
                {
                    // The driver still owns this buffer and may yet read from it. Deliberately
                    // leak the pins rather than hand the driver memory the GC can reuse.
                    return;
                }
            }
            if (hHeader.IsAllocated)
                hHeader.Free();
            if (hBuffer.IsAllocated)
                hBuffer.Free();
            if (hThis.IsAllocated)
                hThis.Free();
        }

        #endregion

        /// this is called by the WAVE callback and should be used to refill the buffer
        public bool OnDone()
        {
            int bytes;
            lock (waveStream)
            {
                bytes = waveStream.Read(buffer, 0, buffer.Length);
            }
            if (bytes == 0)
            {
                return false;
            }
            for (int n = bytes; n < buffer.Length; n++)
            {
                buffer[n] = 0;
            }
            return WriteToWaveOut();
        }

        /// <summary>
        /// Whether the header's in queue flag is set
        /// </summary>
        public bool InQueue
        {
            get
            {
                return (header.flags & WaveHeaderFlags.InQueue) == WaveHeaderFlags.InQueue;
            }
        }

        /// <summary>
        /// The buffer size in bytes
        /// </summary>
        public int BufferSize => bufferSize;

        private bool WriteToWaveOut()
        {
            MmResult result;

            lock (waveOutLock)
            {
                if (canQueue != null && !canQueue())
                {
                    return false;
                }
                result = WaveInterop.waveOutWrite(hWaveOut, header, Marshal.SizeOf(header));
            }
            if (result != MmResult.NoError)
            {
                throw new MmException(result, "waveOutWrite");
            }

            GC.KeepAlive(this);
            return true;
        }

    }
}
