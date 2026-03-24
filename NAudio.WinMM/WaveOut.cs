using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WaveOut playback device using event callbacks
    /// </summary>
    public class WaveOut : IWavePlayer, IWavePosition
    {
        private readonly object waveOutLock;
        private readonly SynchronizationContext syncContext;
        private IntPtr hWaveOut; // WaveOut handle
        private WaveOutBuffer[] buffers;
        private IAudioSource waveStream;
        private volatile PlaybackState playbackState;
        private AutoResetEvent callbackEvent;
        private bool isDisposed;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Returns the number of Wave Out devices available in the system
        /// </summary>
        public static int DeviceCount => WaveInterop.waveOutGetNumDevs();

        /// <summary>
        /// Retrieves the capabilities of a waveOut device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveOut device capabilities</returns>
        public static WaveOutCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveOutCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Milliseconds for each buffer. Recommended value is 100ms.
        /// Should be set before a call to Init
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the number of buffers used
        /// Should be set before a call to Init
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// Gets or sets the device number
        /// Should be set before a call to Init
        /// This must be between -1 and <see>DeviceCount</see> - 1.
        /// -1 means stick to default device even default device is changed
        /// </summary>
        public int DeviceNumber { get; set; } = -1;

        /// <summary>
        /// Creates a new WaveOut device
        /// </summary>
        public WaveOut()
        {
            syncContext = SynchronizationContext.Current;
            BufferMilliseconds = 100;
            NumberOfBuffers = 2;
            waveOutLock = new object();
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="audioSource">Audio source to play</param>
        public void Init(IAudioSource audioSource)
        {
            if (playbackState != PlaybackState.Stopped)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }
            if (hWaveOut != IntPtr.Zero)
            {
                // normally we don't allow calling Init twice, but as experiment, see if we can clean up and go again
                // try to allow reuse of this waveOut device
                // n.b. risky if Playback thread has not exited
                DisposeBuffers();
                CloseWaveOut();
            }

            callbackEvent = new AutoResetEvent(false);

            waveStream = audioSource;
            int bufferSize = audioSource.WaveFormat.ConvertLatencyToByteSize(BufferMilliseconds);

            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutOpenWindow(out hWaveOut, (IntPtr)DeviceNumber, waveStream.WaveFormat, callbackEvent.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackEvent);
            }
            MmException.Try(result, "waveOutOpen");

            buffers = new WaveOutBuffer[NumberOfBuffers];
            playbackState = PlaybackState.Stopped;
            for (var n = 0; n < NumberOfBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (buffers == null || waveStream == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                callbackEvent.Set(); // give the thread a kick
                var playbackThread = new Thread(PlaybackThread)
                {
                    IsBackground = true,
                    Name = "NAudio WaveOut Playback",
                    Priority = ThreadPriority.AboveNormal
                };
                playbackThread.Start();
            }
            else if (playbackState == PlaybackState.Paused)
            {
                Resume();
                callbackEvent.Set(); // give the thread a kick
            }
        }

        private void PlaybackThread()
        {
            Exception exception = null;
            try
            {
                DoPlayback();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                playbackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void DoPlayback()
        {
            while (playbackState != PlaybackState.Stopped)
            {
                if (!callbackEvent.WaitOne(BufferMilliseconds * NumberOfBuffers))
                {
                    if (playbackState == PlaybackState.Playing)
                    {
                        Debug.WriteLine("WARNING: WaveOut callback event timeout");
                    }
                }


                // requeue any buffers returned to us
                if (playbackState == PlaybackState.Playing)
                {
                    int queued = 0;
                    foreach (var buffer in buffers)
                    {
                        if (buffer.InQueue || buffer.OnDone())
                        {
                            queued++;
                        }
                    }
                    if (queued == 0)
                    {
                        // we got to the end
                        playbackState = PlaybackState.Stopped;
                        callbackEvent.Set();
                    }
                }
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
                playbackState = PlaybackState.Paused; // set this here to avoid a deadlock problem with some drivers
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutPause(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutPause");
                }
            }
        }

        /// <summary>
        /// Resume playing after a pause from the same position
        /// </summary>
        private void Resume()
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
                playbackState = PlaybackState.Stopped; // set this here to avoid a problem with some drivers
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutReset(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutReset");
                }
                callbackEvent.Set(); // give the thread a kick, make sure we exit
            }
        }

        /// <summary>
        /// Gets the current position in bytes from the wave output device.
        /// (n.b. this is not the same thing as the position within your reader
        /// stream - it calls directly into waveOutGetPosition)
        /// </summary>
        /// <returns>Position in bytes</returns>
        public long GetPosition() => WaveOutUtils.GetPositionBytes(hWaveOut, waveOutLock);

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat => waveStream.WaveFormat;

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// Volume for this device 1.0 is full scale
        /// </summary>
        public float Volume
        {
            get => WaveOutUtils.GetWaveOutVolume(hWaveOut, waveOutLock);
            set => WaveOutUtils.SetWaveOutVolume(value, hWaveOut, waveOutLock);
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
            if (isDisposed) return;
            isDisposed = true;

            if (disposing)
            {
                Stop();
                DisposeBuffers();
            }

            CloseWaveOut();
        }

        private void CloseWaveOut()
        {
            if (callbackEvent != null)
            {
                callbackEvent.Close();
                callbackEvent = null;
            }
            lock (waveOutLock)
            {
                if (hWaveOut != IntPtr.Zero)
                {
                    WaveInterop.waveOutClose(hWaveOut);
                    hWaveOut = IntPtr.Zero;
                }
            }
        }

        private void DisposeBuffers()
        {
            if (buffers != null)
            {
                foreach (var buffer in buffers)
                {
                    buffer.Dispose();
                }
                buffers = null;
            }
        }

        /// <summary>
        /// Finalizer. Only called when user forgets to call <see>Dispose</see>
        /// </summary>
        ~WaveOut()
        {
            Dispose(false);
        }

#endregion

        private void RaisePlaybackStoppedEvent(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="WaveOut"/> instead (renamed from WaveOutEvent to WaveOut in NAudio 3.0)
    /// </summary>
    [Obsolete("WaveOutEvent has been renamed to WaveOut. Use WaveOut instead.")]
    public class WaveOutEvent : WaveOut
    {
    }
}
