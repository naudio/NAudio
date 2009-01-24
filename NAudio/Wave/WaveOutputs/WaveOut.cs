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
        private int numBuffers;
        private PlaybackState playbackState;
        private WaveInterop.WaveOutCallback callback;
        private int devNumber;
        private int desiredLatency;
        private float volume = 1;
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
        [Obsolete]
        public WaveOut(int devNumber, int desiredLatency, System.Windows.Forms.Control parentWindow)
            : this(devNumber, desiredLatency, parentWindow != null)
        {

        }

        /// <summary>
        /// Opens a WaveOut device
        /// </summary>
        /// <param name="devNumber">This is the device number to open. 
        /// This must be between 0 and <see>DeviceCount</see> - 1.</param>
        /// <param name="desiredLatency">The number of milliseconds of audio to read before 
        /// streaming to the audio device. This will be broken into 3 buffers</param>
        /// <param name="windowCallback">If this parameter is true, the Wave Out Messages
        /// will be sent to the message loop of a Windows form. This is considered a
        /// safer way to use the waveOut functionality. If this parameter is false, we use a
        /// lock to ensure that no two threads can call WaveOut functions at the same time, which
        /// can cause lockups or crashes with some drivers</param>
        public WaveOut(int devNumber, int desiredLatency, bool windowCallback)
        {
            this.devNumber = devNumber;
            this.desiredLatency = desiredLatency;
            this.callback = new WaveInterop.WaveOutCallback(Callback);
            this.waveOutLock = new object();
            if (windowCallback)
            {
                waveOutWindow = new WaveOutWindow(callback);
                //waveOutWindow.AssignHandle(windowHandle);
            }
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="waveProvider">WaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            this.waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((desiredLatency + 2) / 3); //waveStream.GetReadSize((desiredLatency + 2) / 3);
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
            playbackState = PlaybackState.Stopped;
            for (int n = 0; n < numBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {                
                if (playbackState == PlaybackState.Stopped)
                {
                    for (int n = 0; n < numBuffers; n++)
                    {
                        System.Diagnostics.Debug.Assert(buffers[n].InQueue == false, "Adding a buffer that was already queued on play");
                        if (!buffers[n].OnDone())
                        {
                            playbackState = PlaybackState.Stopped;
                        }
                    }                    
                }
                Resume();
                playbackState = PlaybackState.Playing;

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
            playbackState = PlaybackState.Paused;
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
            playbackState = PlaybackState.Playing;
        }

        /// <summary>
        /// Stop and reset the WaveOut device
        /// </summary>
        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutReset(hWaveOut);
            }
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutReset");
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
                    //waveOutWindow.ReleaseHandle();
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
                GCHandle hBuffer = (GCHandle)wavhdr.userData;
                WaveOutBuffer buffer = (WaveOutBuffer)hBuffer.Target;
                // check that we're not here through pressing stop
                if (PlaybackState == PlaybackState.Playing)
                {
                    if(!buffer.OnDone())
                    {
                        playbackState = PlaybackState.Stopped;
                    }
                }

                // n.b. this was wrapped in an exception handler, but bug should be fixed now
            }
        }
    }
}
