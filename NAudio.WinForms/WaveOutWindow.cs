using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Plays audio using the winmm <c>waveOut</c> API with CALLBACK_WINDOW, so that buffer-done
    /// notifications are delivered on the UI thread's message pump. Useful when you want
    /// <see cref="PlaybackStopped"/> and internal buffer recycling to happen on the UI thread
    /// without any manual marshaling. For background-thread playback use
    /// <see cref="WaveOut"/> in the NAudio.WinMM package instead.
    /// </summary>
    public class WaveOutWindow : IWavePlayer, IWavePosition
    {
        private readonly object waveOutLock = new object();
        private readonly SynchronizationContext syncContext;
        private readonly WaveInterop.WaveCallback callback;
        private readonly WaveCallbackHost callbackHost;
        private IntPtr hWaveOut;
        private WaveOutBuffer[] buffers;
        private IWaveProvider waveStream;
        private volatile PlaybackState playbackState;
        private int queuedBuffers;
        private int nextBufferIndex;
        private bool isDisposed;

        /// <summary>
        /// Indicates playback has stopped automatically (end of stream or an error).
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Returns the number of waveOut devices available on the system.
        /// </summary>
        public static int DeviceCount => WaveInterop.waveOutGetNumDevs();

        /// <summary>
        /// Retrieves the capabilities of a waveOut device.
        /// </summary>
        public static WaveOutCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveOutCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Desired latency in milliseconds. Must be set before <see cref="Init"/>.
        /// </summary>
        public int DesiredLatency { get; set; } = 300;

        /// <summary>
        /// Number of buffers used. Must be set before <see cref="Init"/>.
        /// </summary>
        public int NumberOfBuffers { get; set; } = 2;

        /// <summary>
        /// Device number. -1 (default) tracks the current default device.
        /// </summary>
        public int DeviceNumber { get; set; } = -1;

        /// <summary>
        /// Creates a <see cref="WaveOutWindow"/> that owns a hidden callback window.
        /// Must be constructed on a thread with a running Windows Forms message loop.
        /// </summary>
        public WaveOutWindow()
        {
            syncContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException(
                    "WaveOutWindow requires a SynchronizationContext. Use WaveOut for background-thread playback.");
            callback = Callback;
            callbackHost = new WaveCallbackHost(callback);
        }

        /// <summary>
        /// Creates a <see cref="WaveOutWindow"/> that subclasses an existing window so its
        /// HWND receives waveOut callbacks. The handle must belong to a window whose message
        /// loop is running on the thread that constructs this instance.
        /// </summary>
        public WaveOutWindow(IntPtr windowHandle)
        {
            syncContext = SynchronizationContext.Current
                ?? throw new InvalidOperationException(
                    "WaveOutWindow requires a SynchronizationContext. Use WaveOut for background-thread playback.");
            callback = Callback;
            callbackHost = new WaveCallbackHost(callback, windowHandle);
        }

        /// <summary>
        /// Initialises the device with the provider to be played.
        /// </summary>
        public void Init(IWaveProvider waveProvider)
        {
            waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);

            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutOpenWindow(
                    out hWaveOut,
                    (IntPtr)DeviceNumber,
                    waveStream.WaveFormat,
                    callbackHost.Handle,
                    IntPtr.Zero,
                    WaveInterop.WaveInOutOpenFlags.CallbackWindow);
            }
            MmException.Try(result, "waveOutOpen");

            buffers = new WaveOutBuffer[NumberOfBuffers];
            playbackState = PlaybackState.Stopped;
            nextBufferIndex = 0;
            for (int n = 0; n < NumberOfBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        /// <summary>
        /// Start playing.
        /// </summary>
        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                Debug.Assert(queuedBuffers == 0, "Buffers already queued on play");
                nextBufferIndex = 0;
                EnqueueBuffers();
            }
            else if (playbackState == PlaybackState.Paused)
            {
                EnqueueBuffers();
                Resume();
                playbackState = PlaybackState.Playing;
            }
        }

        private void EnqueueBuffers()
        {
            for (int n = 0; n < NumberOfBuffers; n++)
            {
                if (!buffers[n].InQueue)
                {
                    if (buffers[n].OnDone())
                    {
                        Interlocked.Increment(ref queuedBuffers);
                    }
                    else
                    {
                        playbackState = PlaybackState.Stopped;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Pause playback.
        /// </summary>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
                MmResult result;
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
        /// Resume playback from a paused state.
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
        /// Stop and reset the device. <see cref="PlaybackStopped"/> is raised from the
        /// buffer-done callback once all outstanding buffers have been returned.
        /// </summary>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutReset(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutReset");
                }
            }
        }

        /// <summary>
        /// Current byte position as reported by waveOutGetPosition.
        /// </summary>
        public long GetPosition() => WaveOutUtils.GetPositionBytes(hWaveOut, waveOutLock);

        /// <summary>
        /// The format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat => waveStream.WaveFormat;

        /// <summary>
        /// Playback state.
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// Volume for this device. 1.0 is full scale.
        /// </summary>
        public float Volume
        {
            get => WaveOutUtils.GetWaveOutVolume(hWaveOut, waveOutLock);
            set => WaveOutUtils.SetWaveOutVolume(value, hWaveOut, waveOutLock);
        }

        /// <summary>
        /// Closes this device.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Closes the device and disposes of buffers.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            isDisposed = true;

            Stop();

            if (disposing && buffers != null)
            {
                foreach (var buffer in buffers)
                {
                    buffer?.Dispose();
                }
                buffers = null;
            }

            lock (waveOutLock)
            {
                if (hWaveOut != IntPtr.Zero)
                {
                    WaveInterop.waveOutClose(hWaveOut);
                    hWaveOut = IntPtr.Zero;
                }
            }
            if (disposing)
            {
                callbackHost.Dispose();
            }
        }

        /// <summary>
        /// Finaliser, called only if the user forgets to call <see cref="Dispose()"/>.
        /// </summary>
        ~WaveOutWindow()
        {
            Debug.Assert(false, "WaveOutWindow device was not closed");
            Dispose(false);
        }

        private void Callback(IntPtr hWaveOut, WaveInterop.WaveMessage uMsg, IntPtr dwInstance, WaveHeader wavhdr, IntPtr dwReserved)
        {
            if (uMsg != WaveInterop.WaveMessage.WaveOutDone) return;
            if (buffers == null) return;

            // Buffers always return in the order they were enqueued, so a cyclic index is enough
            // to identify which one just completed — no need to track buffers via WaveHeader.userData.
            var buffer = buffers[nextBufferIndex];
            nextBufferIndex = (nextBufferIndex + 1) % buffers.Length;
            Interlocked.Decrement(ref queuedBuffers);

            Exception exception = null;
            if (playbackState == PlaybackState.Playing)
            {
                lock (waveOutLock)
                {
                    try
                    {
                        if (buffer.OnDone())
                        {
                            Interlocked.Increment(ref queuedBuffers);
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }
            if (queuedBuffers == 0)
            {
                playbackState = PlaybackState.Stopped;
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void RaisePlaybackStoppedEvent(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler == null) return;
            if (syncContext == null)
            {
                handler(this, new StoppedEventArgs(e));
            }
            else
            {
                syncContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
            }
        }
    }
}
