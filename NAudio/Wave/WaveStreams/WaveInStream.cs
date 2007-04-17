using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Event Args for WaveInStream event
    /// </summary>
    public class WaveInEventArgs : EventArgs
    {
        private byte[] buffer;
        private int bytes;

        /// <summary>
        /// Creates new WaveInEventArgs
        /// </summary>
        public WaveInEventArgs(byte[] buffer, int bytes)
        {
            this.buffer = buffer;
            this.bytes = bytes;
        }

        /// <summary>
        /// Buffer containing recorded data. Note that it might not be completely
        /// full. <seealso cref="BytesRecorded"/>
        /// </summary>
        public byte[] Buffer
        {
            get { return buffer; }
        }

        /// <summary>
        /// The number of recorded bytes in Buffer. <seealso cref="Buffer"/>
        /// </summary>
        public int BytesRecorded
        {
            get { return bytes; }
        }

    }


    /// <summary>
    /// A class to allow recording from WaveIn
    /// </summary>
    public class WaveInStream : WaveStream
    {
        private IntPtr waveInHandle;
        private WaveFormat waveFormat;
        private long length;
        private long position;
        private volatile bool recording;
        private WaveInBuffer[] buffers;
        private int numBuffers;
        private WaveInterop.WaveInCallback callback;
        private WaveInWindow waveInWindow;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler RecordingStopped;

        /// <summary>
        /// Creates a new Wave input stream
        /// </summary>
        /// <param name="deviceNumber">The device to open - 0 is default</param>
        /// <param name="desiredFormat">The PCM format to record in</param>
        /// <param name="callbackWindow">If this parameter is non-null, the Wave In Messages
        /// will be sent to the message loop of the supplied control. This is considered a
        /// safer way to use the waveIn functionality</param>
        public WaveInStream(int deviceNumber, WaveFormat desiredFormat, System.Windows.Forms.Control callbackWindow)
        {
            this.waveFormat = desiredFormat;
            callback = new WaveInterop.WaveInCallback(Callback);
            if (callbackWindow == null)
            {
                MmException.Try(WaveInterop.waveInOpen(out waveInHandle, deviceNumber, desiredFormat, callback, 0, WaveInterop.CallbackFunction), "waveInOpen");
            }
            else
            {
                waveInWindow = new WaveInWindow(callback);
                MmException.Try(WaveInterop.waveInOpenWindow(out waveInHandle, deviceNumber, desiredFormat, callbackWindow.Handle, 0, WaveInterop.CallbackWindow), "waveInOpen");
                waveInWindow.AssignHandle(callbackWindow.Handle);
            }

            // 3 buffers of 1 second each should be fairly safe
            int bufferSize = desiredFormat.AverageBytesPerSecond;
            numBuffers = 3;

            buffers = new WaveInBuffer[numBuffers];
            for (int n = 0; n < numBuffers; n++)
            {
                buffers[n] = new WaveInBuffer(waveInHandle, bufferSize);
            }
        }

        /// <summary>
        /// Called when we get a new buffer of recorded data
        /// </summary>
        private void Callback(IntPtr waveInHandle, WaveInterop.WaveInMessage message, int userData, WaveHeader waveHeader, int reserved)
        {
            if (message == WaveInterop.WaveInMessage.Data)
            {
                GCHandle hBuffer = (GCHandle)waveHeader.userData;
                WaveInBuffer buffer = (WaveInBuffer)hBuffer.Target;

                length += buffer.BytesRecorded;
                if (DataAvailable != null)
                {
                    DataAvailable(this, new WaveInEventArgs(buffer.Data, buffer.BytesRecorded));
                }
                if (recording)
                {
                    buffer.Reuse();
                }
                else
                {
                    if (RecordingStopped != null)
                    {
                        RecordingStopped(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// number of bytes received in this recording session
        /// </summary>
        public override long Length
        {
            get { return length; }
        }

        /// <summary>
        /// Current position in the stream. For future use
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                throw new Exception("You can't reposition a WaveIn stream.");
            }
        }

        /// <summary>
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            if (recording)
                throw new InvalidOperationException("Already recording");
            length = 0;
            position = 0;
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            recording = true;
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            //if (!recording)
            //    throw new InvalidOperationException("Not recording");
            recording = false;
            MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");
            //MmException.Try(WaveInterop.waveInReset(waveInHandle), "waveInReset");           
        }

        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (recording)
                    StopRecording();
                if (buffers != null)
                {
                    for (int n = 0; n < numBuffers; n++)
                    {
                        buffers[n].Dispose();
                    }
                    buffers = null;
                }
                WaveInterop.waveInClose(waveInHandle);
                waveInHandle = IntPtr.Zero;
                if (waveInWindow != null)
                {
                    waveInWindow.ReleaseHandle();
                    waveInWindow = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Reads from this stream. For future use.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // TODO: basic reading support
            // use a queue of buffers. Dropout if queue.Count = numBuffers
            throw new Exception("The method or operation is not implemented.");
        }

        private class WaveInWindow : System.Windows.Forms.NativeWindow
        {
            private WaveInterop.WaveInCallback waveInCallback;

            public WaveInWindow(WaveInterop.WaveInCallback waveInCallback)
            {
                this.waveInCallback = waveInCallback;
            }

            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == (int)WaveInterop.WaveInMessage.Data)
                {
                    IntPtr hOutputDevice = m.WParam;
                    WaveHeader waveHeader = new WaveHeader();
                    Marshal.PtrToStructure(m.LParam, waveHeader);
                    waveInCallback(hOutputDevice, WaveInterop.WaveInMessage.Data, 0, waveHeader, 0);
                }
                else if (m.Msg == (int)WaveInterop.WaveInMessage.Open)
                {
                    waveInCallback(m.WParam, WaveInterop.WaveInMessage.Open, 0, null, 0);
                }
                else if (m.Msg == (int)WaveInterop.WaveInMessage.Close)
                {
                    waveInCallback(m.WParam, WaveInterop.WaveInMessage.Close, 0, null, 0);
                }
                base.WndProc(ref m);
            }
        }

    }
}
