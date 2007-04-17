using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NAudio.Wave 
{
    /// <summary>
    /// Represents a wave out device
    /// </summary>
    public class WaveOut : IWavePlayer
    {
        private IntPtr hWaveOut;
        private WaveOutBuffer[] buffers;
        private WaveStream waveStream;
        private int numBuffers;
        private volatile bool playing;
        private volatile bool paused;
        private WaveInterop.WaveOutCallback callback;
        private int devNumber;
        private int desiredLatency;
        private float pan = 0;
        private float volume = 1;
        private bool buffersQueued;
        private WaveOutWindow waveOutWindow;
        private object waveOutLock;


        /// <summary>
        /// Retrieves the capabilities of a waveOut device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveOut device capabilities</returns>
        public static WaveOutCapabilities GetCapabilities(int devNumber)
        {
            WaveOutCapabilities caps = new WaveOutCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps(devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Returns the number of Wave Out devices available in the system
        /// </summary>
        public static Int32 DeviceCount
        {
            get
            {
                return WaveInterop.waveOutGetNumDevs();
            }
        }

        /// <summary>
        /// Opens a WaveOut device
        /// </summary>
        /// <param name="devNumber">This is the device number to open. 
        /// This must be between 0 and <see>DeviceCount</see> - 1.</param>
        /// <param name="desiredLatency">The number of milliseconds of audio to read before 
        /// streaming to the audio device. This will be broken into 3 buffers</param>
        /// <param name="parentWindow">If this parameter is non-null, the Wave Out Messages
        /// will be sent to the message loop of the supplied control. This is considered a
        /// safer way to use the waveOut functionality. If this parameter is null, we use a
        /// lock to ensure that no two threads can call WaveOut functions at the same time, which
        /// can cause lockups or crashes with some drivers</param>
        public WaveOut(int devNumber, int desiredLatency, System.Windows.Forms.Control parentWindow)
        {
            this.devNumber = devNumber;
            this.desiredLatency = desiredLatency;
            this.callback = new WaveInterop.WaveOutCallback(Callback);
            this.waveOutLock = new object();
            if (parentWindow != null)
            {
                waveOutWindow = new WaveOutWindow(callback);
                waveOutWindow.AssignHandle(parentWindow.Handle);
            }
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="waveStream">WaveStream to play</param>
        public void Init(WaveStream waveStream)
        {
            this.waveStream = waveStream;
            int bufferSize = waveStream.GetReadSize((desiredLatency + 2) / 3);
            this.numBuffers = 3;

            MmResult result;
            lock (waveOutLock)
            {
                if (waveOutWindow == null)
                {
                    result = WaveInterop.waveOutOpen(out hWaveOut, devNumber, waveStream.WaveFormat, callback, 0, WaveInterop.CallbackFunction);
                }
                else
                {
                    result = WaveInterop.waveOutOpenWindow(out hWaveOut, devNumber, waveStream.WaveFormat, waveOutWindow.Handle, 0, WaveInterop.CallbackWindow);
                }
            }
            MmException.Try(result,"waveOutOpen");

            buffers = new WaveOutBuffer[numBuffers];
            playing = false;
            paused = false;
            for (int n = 0; n < numBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        private class WaveOutWindow : System.Windows.Forms.NativeWindow
        {
            private WaveInterop.WaveOutCallback waveOutCallback;

            public WaveOutWindow(WaveInterop.WaveOutCallback waveOutCallback)
            {
                this.waveOutCallback = waveOutCallback;
            }

            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == (int)WaveInterop.WaveOutMessage.Done)
                {
                    IntPtr hOutputDevice = m.WParam;
                    WaveHeader waveHeader = new WaveHeader();
                    Marshal.PtrToStructure(m.LParam, waveHeader);

                    waveOutCallback(hOutputDevice, WaveInterop.WaveOutMessage.Done, 0, waveHeader, 0);
                }
                else if (m.Msg == (int)WaveInterop.WaveOutMessage.Open)
                {
                    waveOutCallback(m.WParam, WaveInterop.WaveOutMessage.Open, 0, null, 0);
                }
                else if (m.Msg == (int)WaveInterop.WaveOutMessage.Close)
                {
                    waveOutCallback(m.WParam, WaveInterop.WaveOutMessage.Close, 0, null, 0);
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
        }


        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (!IsPlaying)
            {
                playing = true;

                if (!buffersQueued)
                {
                    Pause(); // to avoid a deadlock - we don't want two waveOutWrites at once
                    for (int n = 0; n < numBuffers; n++)
                    {
                        System.Diagnostics.Debug.Assert(buffers[n].InQueue == false, "Adding a buffer that was already queued on play");
                        playing = buffers[n].OnDone();
                    }
                    buffersQueued = playing;
                }
                Resume();
            }
        }

        /// <summary>
        /// Pause the audio
        /// </summary>
        public void Pause()
        {
            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutPause(hWaveOut);
            }
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutPause");
            paused = true;
        }

        /// <summary>
        /// Resume playing after a pause from the same position
        /// </summary>
        public void Resume()
        {
            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutRestart(hWaveOut);
            }
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutRestart");
            paused = false;
        }

        /// <summary>
        /// Stop and reset the WaveOut device
        /// </summary>
        public void Stop()
        {
            playing = false;
            paused = false;
            buffersQueued = false;
            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutReset(hWaveOut);
            }
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutReset");
        }

        /// <summary>
        /// Returns true if the audio is currently paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return paused;
            }
        }

        /// <summary>
        /// Returns true if the audio is currently playing
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return playing && !paused;
            }
        }

        /// <summary>
        /// Pan / Balance for this device -1.0 to 1.0
        /// </summary>
        public float Pan
        {
            get
            {
                return pan;
            }
            set
            {
                pan = value;
                Volume = Volume;
            }
        }

        /// <summary>
        /// Volume for this device 1.0 is full scale
        /// </summary>
        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
                float left = volume * (1 - pan);
                float right = volume * (1 + pan);
                if (left > 1.0) left = 1.0f;
                if (right > 1.0) right = 1.0f;
                if (left < 0.0) left = 0.0f;
                if (right < 0.0) right = 0.0f;
                int stereoVolume = (int)(left * 0xFFFF) + ((int)(right * 0xFFFF) << 16);
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutSetVolume(hWaveOut, stereoVolume);
                }
                MmException.Try(result,"waveOutSetVolume");
            }

        }


        #region Dispose Pattern

        /// <summary>
        /// Closes this WaveOut device
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Closes the WaveOut device and disposes of buffers
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected void Dispose(bool disposing)
        {
            Stop();
            lock (waveOutLock)
            {
                WaveInterop.waveOutClose(hWaveOut);
            }
            if (disposing)
            {
                if (buffers != null)
                {
                    for (int n = 0; n < numBuffers; n++)
                    {
                        buffers[n].Dispose();
                    }
                    buffers = null;
                }
                if (waveOutWindow != null)
                {
                    waveOutWindow.ReleaseHandle();
                    waveOutWindow = null;
                }
            }
        }

        /// <summary>
        /// Finalizer. Only called when user forgets to call <see>Dispose</see>
        /// </summary>
        ~WaveOut()
        {
            System.Diagnostics.Debug.Assert(false, "WaveOut device was not closed");
            Dispose(false);
        }

        #endregion

        // made non-static so that playing can be stopped here
        private void Callback(IntPtr hWaveOut, WaveInterop.WaveOutMessage uMsg, Int32 dwUser, WaveHeader wavhdr, int dwReserved)
        {
            if (uMsg == WaveInterop.WaveOutMessage.Done)
            {
                // check that we're not here through pressing stop
                GCHandle hBuffer = (GCHandle)wavhdr.userData;
                WaveOutBuffer buffer = (WaveOutBuffer)hBuffer.Target;
                if (playing)
                {
                    playing = buffer.OnDone();
                    // TODO: could signal end of file reached
                    if (!playing)
                    {
                        buffersQueued = false;
                    }
                }

                // n.b. this was wrapped in an exception handler, but bug should be fixed now
            }
        }
    }
}
