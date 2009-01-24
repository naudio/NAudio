using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Allows recording using the Windows waveIn APIs
    /// Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveIn
    {
        private IntPtr waveInHandle;
        private WaveFormat waveFormat;
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
        /// Prepares a wave input device for recording with the most typical options
        /// The default input device will be used, bit depth will default to 16, and 
        /// a Window handle will be used for callbacks
        /// </summary>
        /// <param name="sampleRate">Recording sample rate (e.g. 8000, 22050, 44100)</param>
        /// <param name="channels">Number of channels (1 for mono, 2 for stereo)</param>
        public WaveIn(int sampleRate, int channels)
            : this(0, sampleRate, 16, channels, true)
        {
        }

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        /// <param name="deviceNumber">The device to open - 0 is default</param>
        /// <param name="sampleRate">Recording sample rate (e.g. 8000, 22050, 44100)</param>
        /// <param name="bitDepth">Recording bit depth (typically 16)</param>
        /// <param name="channels">Number of channels (1 for mono, 2 for stereo)</param>
        /// <param name="callbackWindow">If true, a window handle will be used for callbacks</param>
        public WaveIn(int deviceNumber, int sampleRate, int bitDepth, int channels, bool callbackWindow)
        {
            this.waveFormat = new WaveFormat(sampleRate, bitDepth, channels);
            callback = new WaveInterop.WaveInCallback(Callback);
            if (!callbackWindow)
            {
                MmException.Try(WaveInterop.waveInOpen(out waveInHandle, deviceNumber, waveFormat, callback, 0, WaveInterop.CallbackFunction), "waveInOpen");
            }
            else
            {
                waveInWindow = new WaveInWindow(callback);
                MmException.Try(WaveInterop.waveInOpenWindow(out waveInHandle, deviceNumber, waveFormat, waveInWindow.Handle, 0, WaveInterop.CallbackWindow), "waveInOpen");
                //waveInWindow.AssignHandle(callbackWindow.Handle);
            }

            CreateBuffers();
        }

        private void CreateBuffers()
        {
            // Default to three buffers of 100ms each
            int bufferSize = waveFormat.AverageBytesPerSecond / 10;
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
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            if (recording)
                throw new InvalidOperationException("Already recording");
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            recording = true;
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            recording = false;
            MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");
            //MmException.Try(WaveInterop.waveInReset(waveInHandle), "waveInReset");           
        }

        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
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
                    waveInWindow.Dispose();
                    //waveInWindow.ReleaseHandle();
                    waveInWindow = null;
                }
            }
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
