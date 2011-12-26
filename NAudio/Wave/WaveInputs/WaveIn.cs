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
        private WaveInterop.WaveCallback callback;
        private WaveCallbackInfo callbackInfo;

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
            : this(WaveCallbackInfo.NewWindow())
        {

        }

        /// <summary>
        /// Creates a WaveIn device using the specified window handle for callbacks
        /// </summary>
        /// <param name="windowHandle">A valid window handle</param>
        public WaveIn(IntPtr windowHandle)
            : this(WaveCallbackInfo.ExistingWindow(windowHandle))
        {

        }

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveIn(WaveCallbackInfo callbackInfo)
        {
            this.DeviceNumber = 0;
            this.WaveFormat = new WaveFormat(8000, 16, 1);
            this.BufferMilliseconds = 100;
            this.NumberOfBuffers = 3;
            this.callback = new WaveInterop.WaveCallback(Callback);
            this.callbackInfo = callbackInfo;
            callbackInfo.Connect(this.callback);
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
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// Number of Buffers to use (usually 2 or 3)
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// The device number to use
        /// </summary>
        public int DeviceNumber { get; set; }

        private void CreateBuffers()
        {
            // Default to three buffers of 100ms each
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;

            buffers = new WaveInBuffer[NumberOfBuffers];
            for (int n = 0; n < buffers.Length; n++)
            {
                buffers[n] = new WaveInBuffer(waveInHandle, bufferSize);
            }
        }

        /// <summary>
        /// Called when we get a new buffer of recorded data
        /// </summary>
        private void Callback(IntPtr waveInHandle, WaveInterop.WaveMessage message, IntPtr userData, WaveHeader waveHeader, IntPtr reserved)
        {
            if (message == WaveInterop.WaveMessage.WaveInData)
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
            MmResult result;
            result = callbackInfo.WaveInOpen(out waveInHandle, DeviceNumber, WaveFormat, callback);
            MmException.Try(result, "waveInOpen");
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
                if (callbackInfo != null)
                {
                    callbackInfo.Disconnect();
                    callbackInfo = null;
                }
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
