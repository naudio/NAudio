
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

/// <summary>
/// Provides a way to capture data from an audio device based on Apple's Audio Queue Services. <br />
/// Audio is delivered as 32-bit float interleaved samples on a thread of the queue, so a garbage
/// collection or a slow handler does not lose audio unless it takes longer than the queued buffers. <br />
/// The recorder dispatches the <see cref="CaptureFormatChanged"/> event when the
/// format of the device changes, because the capture format follows the device's.
/// Use <see cref="DesiredLatency"/> to trade latency against the risk of dropouts.
/// </summary>
/// <seealso cref="MacOS.CoreAudio"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed partial class CoreAudioRecorder : IDisposable, IAsyncDisposable, IWaveLatency
{
    private CaptureState state;
    private readonly Lock lockObject;
    private CoreAudioRecorderStateFlags flags;
    private readonly AudioDevice selectedDevice;
    private PropertyListenerHandle? streamsChanged;
    private PropertyListenerHandle? virtualFormatChanged;
    private readonly SynchronizationContext? syncContext;
    private readonly PropertyListenerHandle deviceWasRemoved;
    private IntPtr queue;
    private GCHandle queueHandle;
    private IntPtr[] buffers = [];
    private AudioStreamBasicDescription queueFormat;
    private double bufferSeconds;
    private int runId;
    private bool raiseStoppedOnFailure;
    // Read by the queue's callback.
    private volatile bool capturing;
    private volatile bool stopping;
    private volatile Exception? failure;
    private volatile CoreAudioCaptureDataAvailableHandler? sink;

    /// <summary>
    /// Initializes a new Core Audio recoder instance, using the default input
    /// device to capture audio data. <br />
    /// If a synchronization context is assigned for the thread where this instance
    /// is created to, it is used when a recording stopped event is dispatching.
    /// </summary>
    public CoreAudioRecorder() : this(AudioSystemObject.Instance.DefaultInputDevice ?? throw new InvalidOperationException("No default input device is available.")) { }

    /// <summary>
    /// Initializes a new Core Audio recoder instance from the specified device. <br />
    /// If a synchronization context is assigned for the thread where this instance
    /// is created to, it is used when a recording stopped event is dispatching.
    /// </summary>
    /// <param name="device">The <see cref="AudioDevice"/> to capture data from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public CoreAudioRecorder(AudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.IsAlive)
        {
            throw new InvalidOperationException("This device will be removed soon, and as such cannot be used as a capture device.");
        }
        lockObject = new();
        streamsChanged = null;
        selectedDevice = device;
        virtualFormatChanged = null;
        state = CaptureState.Stopped;
        syncContext = SynchronizationContext.Current;
        deviceWasRemoved = device.ConstructIsAliveChangedEvent();
        deviceWasRemoved.Event += OnDeviceWillBeDestroyed;
    }

    #region Internal support code

    /// <summary>
    /// Behavioral flags that affect the recorder's state.
    /// </summary>
    [Flags]
    private enum CoreAudioRecorderStateFlags : byte
    {
        Disposed = 1 << 0,
        Initialized = 1 << 1,
        // When the device has been removed.
        Invalidated = 1 << 2
    }

    /// <summary>
    /// Provides the data required to invoke the
    /// recording stopped event from a SynchronizationContext.
    /// </summary>
    /// <param name="Recorder">The recorder to pass to the handler as the 'sender'</param>
    /// <param name="Handler">The handler to invoke.</param>
    /// <param name="Args">The StoppedEventArgs that describe the recording state.</param>
    private record SyncContextStoppedEventData(
        CoreAudioRecorder Recorder,
        EventHandler<StoppedEventArgs> Handler,
        StoppedEventArgs Args
    )
    {
        private void PostEvent() => Handler.Invoke(Recorder, Args);

        public static void UnwrapContextAndDispatch(object? state)
            => ((SyncContextStoppedEventData)state!).PostEvent();
    }

    private void FireDataAvailableEvent(
        ReadOnlySpan<byte> audioData,
        double currentTime,
        double firstAcquiredByteFromHAL
    ) => DataAvailable?.Invoke(audioData, currentTime, firstAcquiredByteFromHAL);

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowIfInvalidOrDisposed()
    {
        ObjectDisposedException.ThrowIf(flags.HasFlag(CoreAudioRecorderStateFlags.Disposed), this);
        if (!flags.HasFlag(CoreAudioRecorderStateFlags.Initialized))
        {
            throw new InvalidOperationException("The Core Audio recorder has not been initialized yet.");
        }
        else if (flags.HasFlag(CoreAudioRecorderStateFlags.Invalidated))
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
    }

    private void OnRecodingStopped(Exception? ex) => OnRecodingStopped(new StoppedEventArgs(ex));

    private void OnRecodingStopped(StoppedEventArgs e)
    {
        state = CaptureState.Stopped;
        var handler = RecordingStopped;
        if (handler is null) { return; }
        if (syncContext is null)
        {
            handler.Invoke(this, e);
        }
        else
        {
            syncContext.Post(
                new(SyncContextStoppedEventData.UnwrapContextAndDispatch),
                new SyncContextStoppedEventData(this, handler, e)
            );
        }
    }

    private double GetLatencyInSeconds() =>
        bufferSeconds + (selectedDevice.GetDeviceLatency(AudioObjectPropertyScopeConstants.Input) / queueFormat.mSampleRate);

    private void StopRecordingInternal(bool dispatchRecordingStopped = false)
    {
        try
        {
            state = CaptureState.Stopping;
            StopQueue();
            if (dispatchRecordingStopped)
            {
                OnRecodingStopped(ex: null);
            }
        }
        catch (Exception ex)
        {
            OnRecodingStopped(ex);
        }
        finally
        {
            state = CaptureState.Stopped;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets the selected audio device where the recorder retrieves data from.
    /// </summary>
    public AudioDevice Device => selectedDevice;

    /// <summary>
    /// The total amount of audio the queue can hold before it loses audio, in milliseconds.
    /// Read when the recorder is initialized. The default is 100.
    /// </summary>
    public int DesiredLatency { get; set; } = 100;

    /// <summary>
    /// The number of buffers that <see cref="DesiredLatency"/> is divided into.
    /// Read when the recorder is initialized. The default is 4.
    /// </summary>
    public int NumberOfBuffers { get; set; } = 4;

    /// <summary>Call this method once per instance to set up the recorder for recording.</summary>
    /// <remarks>
    /// This method can be called multiple times, 
    /// it is thread-safe and only the first thread that manages 
    /// to take the lock will perform the initialization task.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The recorder could not be set up (invalid device, invalid streams, etc.)</exception>
    public void InitializeRecording()
    {
        lockObject.Enter();
        try
        {
            if (flags.HasFlag(CoreAudioRecorderStateFlags.Initialized)) { return; }
            Initialize();
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <summary>
    /// Call this method once per instance to set up the recorder for recording asynchronously.
    /// </summary>
    /// <remarks>
    /// This method can be called multiple times, 
    /// it is thread-safe and only the first thread that manages 
    /// to take the lock will perform the initialization task.
    /// </remarks>
    /// <returns>A new <see cref="ValueTask"/> that represents the code to execute for initializing the recoder.</returns>
    public ValueTask InitializeRecordingAsync() => new(Task.Run(new Action(InitializeRecording)));

    /// <summary>
    /// Starts the recording. 
    /// This API uses the <see cref="DataAvailable"/> event to pull the captured audio data.
    /// </summary>
    /// <seealso cref="CaptureAsync"/>
    /// <exception cref="InvalidOperationException">The recorder is not initialized.</exception>
    /// <exception cref="ObjectDisposedException">This recorder instance has been disposed of.</exception>
    /// <exception cref="CoreAudioException">The device that is used by the recorder is not existing.</exception>
    public void StartRecording()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            // Prefer to exit cleanly rather than racing the startup.
            if (state != CaptureState.Stopped) { return; }
            state = CaptureState.Starting;
            try
            {
                StartQueue(FireDataAvailableEvent, true);
                state = CaptureState.Capturing;
            }
            catch
            {
                state = CaptureState.Stopped;
                throw;
            }
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <summary>
    /// Stops the recording, previously started by the <see cref="StartRecording"/> method.
    /// </summary>
    /// <exception cref="InvalidOperationException">The recorder is not initialized.</exception>
    /// <exception cref="ObjectDisposedException">This recorder instance has been disposed of.</exception>
    /// <exception cref="CoreAudioException">The device that is used by the recorder is not existing.</exception>
    public void StopRecording()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            // Prefer to exit cleanly rather than racing the stop.
            if (state == CaptureState.Stopped) { return; }
            StopRecordingInternal(true);
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <summary>
    /// Asynchronously captures audio data from the specified input device, 
    /// initializing the recording if needed in the asynchronous code path.
    /// </summary>
    /// <param name="cancellationToken">The token that can be used to manually stop recording (Although it is also possible by calling the <see cref="StopRecording"/> method).</param>
    /// <returns>An enumerable instance returning capture buffers in an asynchronous manner.</returns>
    /// <exception cref="ObjectDisposedException">This recorder instance has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">This recorder instance is already been in use.</exception>
    /// <exception cref="CoreAudioException">The device that is used by the recorder is not existing.</exception>
    public async IAsyncEnumerable<CoreAudioCaptureBuffer> CaptureAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(flags.HasFlag(CoreAudioRecorderStateFlags.Disposed), this);
        if (flags.HasFlag(CoreAudioRecorderStateFlags.Invalidated))
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
        else if (state != CaptureState.Stopped)
        {
            throw new InvalidOperationException("The recorder is attempting to initialize, or already recording!");
        }

        state = CaptureState.Starting;

        // First, do any initialization if not explicitly done by the user,
        // and defer it's execution to the async path instead.
        await InitializeRecordingAsync();

        // Check for captured buffers about twice per queue buffer.
        int pollMilliseconds = Math.Max(1, (int)(bufferSeconds * TimeSpan.MillisecondsPerSecond / 2d));

        SingleLinkedCaptureBufferQueue captured = new();

        try
        {
            lock (lockObject) { StartQueue(captured.EnqueueFromHandler, false); }
            state = CaptureState.Capturing;

            CoreAudioCaptureBuffer? buffer;
            // The loop also ends when capturing stops by itself: StopRecording, a handler
            // failure, or a change of the device's format or stream list.
            while ((!cancellationToken.IsCancellationRequested) && capturing)
            {
                while ((buffer = captured.Dequeue()) is not null)
                {
                    yield return buffer;
                }
                await Task.Delay(pollMilliseconds, cancellationToken);
            }

            lock (lockObject) { if (capturing) { StopQueue(); } }
            while ((buffer = captured.Dequeue()) is not null) { yield return buffer; }
        }
        finally
        {
            lock (lockObject)
            {
                if (capturing) { StopQueue(); }
                state = CaptureState.Stopped;
            }
            captured.Dispose();
        }

        var ex = failure;
        failure = null;
        if (ex is not null) { throw ex; }
    }

    /// <summary>
    /// Provides an event that is fired when new capture data are retrieved from the HAL.
    /// </summary>
    public event CoreAudioCaptureDataAvailableHandler? DataAvailable;

    /// <summary>
    /// Provides an event that is fired when the recorder's capture format has been changed.
    /// </summary>
    /// <remarks>
    /// If capture is already running, capture will be stopped. <br />
    /// If you want to restart recording, explicitly call the <see cref="StartRecording"/> method.
    /// </remarks>
    public event CoreAudioCaptureFormatChangedHandler? CaptureFormatChanged;

    /// <summary>
    /// Provides an event that is fired when the recording has been stopped.
    /// </summary>
    public event EventHandler<StoppedEventArgs>? RecordingStopped;

    /// <summary>
    /// Provides the audio format the HAL uses to capture audio. <br />
    /// This is the format of the audio data that are provided during the dispatch of the <see cref="DataAvailable"/> event.
    /// </summary>
    /// <exception cref="InvalidOperationException">This <see cref="CoreAudioRecorder"/> instance has not yet been initialized.</exception>
    public WaveFormat CaptureFormat
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            // Construct the capture format.
            var virtualFormat = queueFormat;

            // Query channel layout
            Speakers spk = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Input, out var needsTranslation, out var needsExtensible);

            // Do not report speakers if they need translation - 
            // the user should have access to all the channels
            // and can exclude any channel from the buffer(s) 
            // at his will.
            // Note also that we won't report the speakers if it
            // is a very common layout that can be handled without
            // WaveFormatExtensible.
            if (needsTranslation || (!needsExtensible)) { spk = Speakers.None; }

            return MacUtils.ConstructWaveFormatFromASBD(virtualFormat, spk);
        }
    }

    /// <summary>
    /// Gets a value whether the recorder is recording from the initialized device. <br />
    /// Will return <see cref="CaptureState.Stopped"/> once disposed
    /// </summary>
    public CaptureState CaptureState => state;

    /// <inheritdoc />
    public TimeSpan AverageLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimeSpan.FromSeconds(GetLatencyInSeconds());
        }
    }

    /// <inheritdoc />
    public TimeSpan CurrentLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimeSpan.FromSeconds(GetLatencyInSeconds());
        }
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioRecorder"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    /// <remarks>
    /// Note that this method will probably throw when the device
    /// used to initialize the recorder is gone; so, expect to catch
    /// <see cref="AggregateException"/> for this particular case.
    /// </remarks>
    /// <exception cref="AggregateException">One or more native objects were failed to be disposed of.</exception>
    public void Dispose()
    {
        lockObject.Enter();
        try
        {
            if (flags.HasFlag(CoreAudioRecorderStateFlags.Disposed)) { return; }
            if (flags.HasFlag(CoreAudioRecorderStateFlags.Initialized) && state == CaptureState.Capturing) { StopRecordingInternal(); }
            if (state != CaptureState.Stopped) { state = CaptureState.Stopping; }
            DestroyQueue();
            MacUtils.EnsureDisposableObjectsDisposed(
                streamsChanged,
                virtualFormatChanged,
                deviceWasRemoved
            );

            streamsChanged = null;
            virtualFormatChanged = null;
        }
        finally
        {
            flags |= CoreAudioRecorderStateFlags.Disposed;
            state = CaptureState.Stopped;
            lockObject.Exit();
        }
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioRecorder"/> instance asynchronously.
    /// </summary>
    /// <returns>
    /// A new <see cref="ValueTask"/> instance providing the task 
    /// to execute for freeing the held resources of this 
    /// <see cref="CoreAudioRecorder"/> instance asynchronously.
    /// </returns>
    // No special implementation required;
    // just route to the Dispose method implementation and execute it asynchronously.
    public ValueTask DisposeAsync() => new(Task.Run(new Action(Dispose)));

    #endregion

}
