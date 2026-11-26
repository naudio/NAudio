using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// WaveOut playback device using event callbacks
/// </summary>
public class WaveOut : IWavePlayer, IWavePosition, IWaveLatency
{
    private readonly Lock waveOutLock;
    private readonly SynchronizationContext? syncContext;
    private IntPtr hWaveOut; // WaveOut handle
    private WaveOutBuffer[]? buffers;
    private IWaveProvider? waveStream;
    private volatile PlaybackState playbackState;
    private AutoResetEvent? callbackEvent;
    private Thread? playbackThread;
    private bool isDisposed;

    /// <summary>
    /// Indicates playback has stopped automatically
    /// </summary>
    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

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
        MmException.Try(WaveInterop.waveOutGetDevCaps(devNumber, out caps, structSize), "waveOutGetDevCaps");
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
        waveOutLock = new Lock();
    }

    /// <summary>
    /// Initialises the WaveOut device
    /// </summary>
    /// <param name="waveProvider">The wave provider to play</param>
    public void Init(IWaveProvider waveProvider)
    {
        if (playbackState != PlaybackState.Stopped)
        {
            throw new InvalidOperationException("Can't re-initialize during playback");
        }
        if (hWaveOut != IntPtr.Zero)
        {
            // normally we don't allow calling Init twice, but as experiment, see if we can clean up and go again
            // try to allow reuse of this waveOut device
            WaitForPlaybackThread();
            ResetDevice();
            DisposeBuffers();
            CloseWaveOut();
        }

        callbackEvent = new AutoResetEvent(false);

        waveStream = waveProvider;
        int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize(BufferMilliseconds);

        MmResult result;
        lock (waveOutLock)
        {
            // A WAVEFORMATEX block built by hand: WaveFormat's subclasses add their fields by
            // inheritance, which the NativeAOT marshaller drops when a WaveFormat-typed
            // parameter is marshalled. See https://github.com/naudio/NAudio/issues/1425.
            IntPtr formatPointer = WaveFormat.MarshalToPtr(waveStream.WaveFormat);
            try
            {
                result = WaveInterop.waveOutOpenWindow(out hWaveOut, DeviceNumber, formatPointer, callbackEvent.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackEvent);
            }
            finally
            {
                Marshal.FreeHGlobal(formatPointer);
            }
        }
        MmException.Try(result, "waveOutOpen");

        buffers = new WaveOutBuffer[NumberOfBuffers];
        playbackState = PlaybackState.Stopped;
        for (var n = 0; n < NumberOfBuffers; n++)
        {
            buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock, CanQueueBuffer);
        }
    }

    /// <summary>
    /// Start playing the audio from the WaveStream
    /// </summary>
    public void Play()
    {
        if (buffers == null || waveStream == null || callbackEvent == null)
        {
            throw new InvalidOperationException("Must call Init first");
        }
        if (playbackState == PlaybackState.Stopped)
        {
            // After a Stop(), the previous playback thread can still be finishing a Read. Let it
            // exit first: otherwise it would see Playing again and keep queueing buffers, and its
            // exit would then set the state back to Stopped underneath the new thread.
            WaitForPlaybackThread();
            playbackState = PlaybackState.Playing;
            callbackEvent.Set(); // give the thread a kick
            var thread = new Thread(PlaybackThread)
            {
                IsBackground = true,
                Name = "NAudio WaveOut Playback",
                Priority = ThreadPriority.AboveNormal
            };
            playbackThread = thread;
            thread.Start();
        }
        else if (playbackState == PlaybackState.Paused)
        {
            Resume();
            callbackEvent.Set(); // give the thread a kick
        }
    }

    private void PlaybackThread()
    {
        Exception? exception = null;
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

    /// <summary>
    /// Passed to each <see cref="WaveOutBuffer"/> and evaluated under <see cref="waveOutLock"/>
    /// just before <c>waveOutWrite</c>. <see cref="Stop"/> sets <see cref="PlaybackState.Stopped"/>
    /// before it takes that lock to call <c>waveOutReset</c>, so once the reset has happened no
    /// buffer can be queued again. Without this, a buffer the playback thread was filling while
    /// Stop() ran went back into the device queue after the reset (issue #1515).
    /// Writes while paused stay allowed: they queue behind waveOutPause and play on resume.
    /// </summary>
    private bool CanQueueBuffer() => playbackState != PlaybackState.Stopped;

    /// <summary>
    /// Waits for the playback thread to exit, unless called from that thread
    /// (e.g. from a <see cref="PlaybackStopped"/> handler with no synchronization context),
    /// where waiting would deadlock. By then the thread has finished with the buffers.
    /// </summary>
    private void WaitForPlaybackThread()
    {
        var thread = playbackThread;
        if (thread != null && thread != Thread.CurrentThread)
        {
            thread.Join();
        }
    }

    private void DoPlayback()
    {
        // Cache the callback event and buffers in locals so a concurrent CloseWaveOut()
        // or DisposeBuffers() (which null out the fields) can't turn our dereferences
        // into a NullReferenceException mid-playback. See issue #804.
        var callback = callbackEvent;
        var currentBuffers = buffers;
        if (callback == null || currentBuffers == null) return;
        while (playbackState != PlaybackState.Stopped)
        {
            if (!callback.WaitOne(BufferMilliseconds * NumberOfBuffers))
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
                foreach (var buffer in currentBuffers)
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
                    callback.Set();
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
            callbackEvent?.Set(); // give the thread a kick, make sure we exit
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
    public WaveFormat OutputWaveFormat => waveStream?.WaveFormat ?? throw new InvalidOperationException("Must call Init first");

    /// <inheritdoc/>
    /// <remarks>
    /// Computed from the buffer configuration as <c>(NumberOfBuffers - 0.5) * BufferMilliseconds</c>:
    /// in steady state every queued buffer plus half of the one currently playing sits between
    /// the application and the speaker.
    /// </remarks>
    public TimeSpan AverageLatency =>
        TimeSpan.FromMilliseconds(BufferMilliseconds * (NumberOfBuffers - 0.5));

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the elapsed time since the oldest in-queue buffer was filled — that is the
    /// buffer the hardware is currently draining. Falls back to <see cref="AverageLatency"/>
    /// when no buffer timestamps are available (e.g. immediately after <see cref="Init"/>).
    /// </remarks>
    public TimeSpan CurrentLatency
    {
        get
        {
            var current = buffers;
            if (current == null) return AverageLatency;

            Span<long> stamps = stackalloc long[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                stamps[i] = current[i].InQueue ? current[i].FilledTimestamp : long.MinValue;
            }
            return WaveOutLatencyHelper.CurrentLatencyFromOldestFilledTimestamp(
                stamps, AverageLatency, Stopwatch.GetTimestamp(), Stopwatch.Frequency);
        }
    }

    /// <summary>
    /// Playback State
    /// </summary>
    public PlaybackState PlaybackState => playbackState;

    /// <summary>
    /// True if any buffer is currently queued on the device. For tests: after <see cref="Stop"/>
    /// has returned and the playback thread has exited, this must be false (issue #1515).
    /// </summary>
    internal bool AnyBufferQueued
    {
        get
        {
            var current = buffers;
            if (current == null) return false;
            foreach (var buffer in current)
            {
                if (buffer.InQueue) return true;
            }
            return false;
        }
    }

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
            // Don't release buffers or the device while the playback thread can still be using
            // them, or reading from a source the caller is about to dispose.
            WaitForPlaybackThread();
            ResetDevice();
            DisposeBuffers();
        }

        CloseWaveOut();
    }

    /// <summary>
    /// Returns any buffer the device still holds, so unpreparing the buffers and closing the
    /// device don't fail with WAVERR_STILLPLAYING (which leaks the buffer and leaves the
    /// device open). Normally nothing is queued by now; this is a safety net.
    /// </summary>
    private void ResetDevice()
    {
        lock (waveOutLock)
        {
            if (hWaveOut != IntPtr.Zero)
            {
                WaveInterop.waveOutReset(hWaveOut);
            }
        }
    }

    private void CloseWaveOut()
    {
        lock (waveOutLock)
        {
            if (hWaveOut != IntPtr.Zero)
            {
                WaveInterop.waveOutClose(hWaveOut);
                hWaveOut = IntPtr.Zero;
            }
        }
        // Only now: the device signals this event as buffers complete, so close it after the device.
        callbackEvent?.Close();
        callbackEvent = null;
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

    private void RaisePlaybackStoppedEvent(Exception? e)
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
