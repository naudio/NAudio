using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.Mixer;

namespace NAudio.Wave
{
    /// <summary>
    /// Allows recording using the Windows waveIn APIs
    /// Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveIn : IWaveIn
    {
        private IntPtr waveInHandle;
        private volatile bool recording;
        private WaveInBuffer[] buffers;
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
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveIn()
        {
            this.DeviceNumber = 0;
            this.WaveFormat = new WaveFormat(8000,16,1);
            this.BufferMillisconds = 100;
            this.NumberOfBuffers = 3;
            this.CallbackWindow = true;
        }

        /// <summary>
        /// Returns the number of Wave In devices available in the system
        /// </summary>
        public static int DeviceCount
        {
            get
            {
                return WaveInterop.waveInGetNumDevs();
            }
        }

        /// <summary>
        /// Retrieves the capabilities of a waveIn device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveIn device capabilities</returns>
        public static WaveInCapabilities GetCapabilities(int devNumber)
        {
            WaveInCapabilities caps = new WaveInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps(devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMillisconds { get; set; }

        /// <summary>
        /// Number of Buffers to use (usually 2 or 3)
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// The device number to use
        /// </summary>
        public int DeviceNumber { get; set; }

        /// <summary>
        /// If true, uses a window for callbacks (should only be set true on a GUI thread)
        /// otherwise uses function callbacks
        /// </summary>
        public bool CallbackWindow { get; set; }

        private void CreateBuffers()
        {
            // Default to three buffers of 100ms each
            int bufferSize = BufferMillisconds * WaveFormat.AverageBytesPerSecond / 1000;

            buffers = new WaveInBuffer[NumberOfBuffers];
            for (int n = 0; n < buffers.Length; n++)
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

        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();
            callback = new WaveInterop.WaveInCallback(Callback);
            if (!CallbackWindow)
            {
                MmException.Try(WaveInterop.waveInOpen(out waveInHandle, DeviceNumber, WaveFormat, callback, 0, WaveInterop.CallbackFunction), "waveInOpen");
            }
            else
            {
                waveInWindow = new WaveInWindow(callback);
                MmException.Try(WaveInterop.waveInOpenWindow(out waveInHandle, DeviceNumber, WaveFormat, waveInWindow.Handle, 0, WaveInterop.CallbackWindow), "waveInOpen");
                //waveInWindow.AssignHandle(callbackWindow.Handle);
            }
            CreateBuffers();
        }


        /// <summary>
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            OpenWaveInDevice();
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
            // Don't actually close yet so we get the last buffer
        }

        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }
        
        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (recording)
                    StopRecording();                
                CloseWaveInDevice();
            }
        }

        private void CloseWaveInDevice()
        {
            // Some drivers need the reset to properly release buffers
            WaveInterop.waveInReset(waveInHandle);
            if (buffers != null)
            {
                for (int n = 0; n < buffers.Length; n++)
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

        /// <summary>
        /// Microphone Level
        /// </summary>
        public MixerLine GetMixerLine()
        {
            // TODO use mixerGetID instead to see if this helps with XP
            MixerLine mixerLine;
            if (waveInHandle != IntPtr.Zero)
            {
                mixerLine = new MixerLine(this.waveInHandle, 0, MixerFlags.WaveInHandle);
            }
            else
            {
                mixerLine = new MixerLine((IntPtr)DeviceNumber, 0, MixerFlags.WaveIn);
            }
            return mixerLine;
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
