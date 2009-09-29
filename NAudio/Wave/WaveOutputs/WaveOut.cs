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
        private IWaveProvider waveStream;
        
        private PlaybackState playbackState;
        private WaveInterop.WaveCallback callback;
        
        private float volume = 1;
        private WaveCallbackInfo callbackInfo;
        private object waveOutLock;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler PlaybackStopped;

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
        /// Gets or sets the desired latency in milliseconds
        /// Should be set before a call to Init
        /// </summary>
        public int DesiredLatency { get; set; }

        /// <summary>
        /// Gets or sets the number of buffers used
        /// Should be set before a call to Init
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// Gets or sets the device number
        /// Should be set before a call to Init
        /// This must be between 0 and <see>DeviceCount</see> - 1.
        /// </summary>
        public int DeviceNumber { get; set; }


        /// <summary>
        /// Creates a default WaveOut device
        /// WARNING: only use this constructor on a GUI thread
        /// </summary>
        public WaveOut()
            : this(WaveCallbackInfo.NewWindow())
        {
        }

        /// <summary>
        /// Creates a WaveOut device using the specified window handle for callbacks
        /// </summary>
        /// <param name="windowHandle">A valid window handle</param>
        public WaveOut(IntPtr windowHandle)
            : this(WaveCallbackInfo.ExistingWindow(windowHandle))
        {

        }

        /// <summary>
        /// Opens a WaveOut device
        /// </summary>
        public WaveOut(WaveCallbackInfo callbackInfo)
        {
            // set default values up
            this.DeviceNumber = 0;
            this.DesiredLatency = 300;
            this.NumberOfBuffers = 3;

            this.callback = new WaveInterop.WaveCallback(Callback);
            this.waveOutLock = new object();
            this.callbackInfo = callbackInfo;
            callbackInfo.Connect(this.callback);
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="waveProvider">WaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            this.waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);            

            MmResult result;
            lock (waveOutLock)
            {
                result = callbackInfo.WaveOutOpen(out hWaveOut, DeviceNumber, waveStream.WaveFormat, callback);
            }
            MmException.Try(result, "waveOutOpen");

            buffers = new WaveOutBuffer[NumberOfBuffers];
            playbackState = PlaybackState.Stopped;
            for (int n = 0; n < NumberOfBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                for (int n = 0; n < NumberOfBuffers; n++)
                {
                    System.Diagnostics.Debug.Assert(buffers[n].InQueue == false, "Adding a buffer that was already queued on play");
                    if (!buffers[n].OnDone())
                    {
                        playbackState = PlaybackState.Stopped;
                    }
                }
            }
            else if (playbackState == PlaybackState.Paused)
            {
                Resume();
                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Pause the audio
        /// </summary>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutPause(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutPause");
                }
                playbackState = PlaybackState.Paused;
            }
        }

        /// <summary>
        /// Resume playing after a pause from the same position
        /// </summary>
        public void Resume()
        {
            if (playbackState == PlaybackState.Paused)
            {
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutRestart(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutRestart");
                }
                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Stop and reset the WaveOut device
        /// </summary>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutReset(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutReset");
                }
                playbackState = PlaybackState.Stopped;
            } 
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
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
                float left = volume;
                float right = volume;

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

            if (disposing)
            {
                if (buffers != null)
                {
                    for (int n = 0; n < buffers.Length; n++)
                    {
                        if (buffers[n] != null)
                        {
                            buffers[n].Dispose();
                        }
                    }
                    buffers = null;
                }
            }

            lock (waveOutLock)
            {
                WaveInterop.waveOutClose(hWaveOut);
            }
            if (disposing)
            {
                callbackInfo.Disconnect();
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
        private void Callback(IntPtr hWaveOut, WaveInterop.WaveMessage uMsg, Int32 dwUser, WaveHeader wavhdr, int dwReserved)
        {
            if (uMsg == WaveInterop.WaveMessage.WaveOutDone)
            {
                GCHandle hBuffer = (GCHandle)wavhdr.userData;
                WaveOutBuffer buffer = (WaveOutBuffer)hBuffer.Target;
                // check that we're not here through pressing stop
                if (PlaybackState == PlaybackState.Playing)
                {
                    if(!buffer.OnDone())
                    {
                        playbackState = PlaybackState.Stopped;
                        RaisePlaybackStoppedEvent();
                    }
                }

                // n.b. this was wrapped in an exception handler, but bug should be fixed now
            }
        }

        private void RaisePlaybackStoppedEvent()
        {
            if (PlaybackStopped != null)
            {
                PlaybackStopped(this, EventArgs.Empty);
            }
        }
    }
}
