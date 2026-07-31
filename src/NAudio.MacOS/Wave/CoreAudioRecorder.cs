
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// Provides a class of capturing audio from a specified audio device using the 
/// macOS HAL API for accessing the captured data.
/// </summary>
public sealed class CoreAudioRecorder : IAsyncDisposable, IDisposable, IWaveLatency
{
    private CaptureState state;
    private RecordingFlags flags;
    private AudioStream selectedStream;
    private readonly AudioDevice selectedDevice;
    private PropertyListenerHandle streamsChanged;
    private CoreAudioCaptureOnlyIOProc ioProcedure;
    private PropertyListenerHandle virtualFormatChanged;
    private readonly PropertyListenerHandle deviceWasRemoved;
    private readonly Func<AudioStream[], int> streamSelector;

    /// <summary>
    /// Initializes a new Core Audio recoder instance from the specified device.
    /// </summary>
    /// <param name="device">The <see cref="AudioDevice"/> to capture data from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public CoreAudioRecorder(AudioDevice device) : this(device, null) { }

    /// <summary>
    /// Initializes a new Core Audio recoder instance from the default input device.
    /// </summary>
    public CoreAudioRecorder() : this(AudioSystemObject.Instance.DefaultInputDevice) { }

    /// <summary>
    /// Initializes a new Core Audio recoder instance from the specified device, 
    /// and a selector that selectsthe input stream that will be used for the capture.
    /// </summary>
    /// <param name="device">The <see cref="AudioDevice"/> to capture data from.</param>
    /// <param name="streamSelector">A function that selects the input stream to use for recording. Can be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public CoreAudioRecorder(AudioDevice device, [MaybeNull] Func<AudioStream[], int> streamSelector)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.IsAlive)
        {
            throw new InvalidOperationException("This device will be shortly removed, and as such cannot be used as a capture device.");
        }
        streamsChanged = null;
        selectedDevice = device;
        flags = RecordingFlags.None;
        state = CaptureState.Stopped;
        this.streamSelector = streamSelector;
        deviceWasRemoved = device.ConstructIsAliveChangedEvent();
        deviceWasRemoved.Event += OnDeviceWillBeDestroyed;
    }

    #region Private support code

    private enum RecordingFlags : byte
    {
        None = 0,
        Initialized = 1 << 0,
        EventHandleRegistered = 1 << 1,
        // mdcdi1315: Defer loopback for now - it needs the aggregate device API's to be ported for this to work,
        // and it might even be separate class.
        // IsUsingLoopback = 1 << 2,
        Invalid = 1 << 3
    }

    private void FireDataAvailableEvent(
        ReadOnlySpan<byte> audioData,
        double currentTime,
        double firstAcquiredByteFromHAL
    ) => DataAvailable?.Invoke(audioData, currentTime, firstAcquiredByteFromHAL);

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowIfInvalid()
    {
        if (!flags.HasFlag(RecordingFlags.Initialized))
        {
            throw new InvalidOperationException("The Core Audio recorder has not been initialized yet.");
        }
        else if (flags.HasFlag(RecordingFlags.Invalid))
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
    }

    private void OnVirtualFormatChanged(AudioObject _)
    {
        // Stop recording if is running. 
        // The user will restart the recording if so he needs.
        if (flags.HasFlag(RecordingFlags.Initialized)) { StopRecording(); }
        if (ioProcedure is not null) { ioProcedure.VirtualFormat = selectedStream.VirtualFormat; }
        CaptureFormatChanged?.Invoke(this);
    }

    private void OnStreamsChanged(AudioObject _)
    {
        bool oldState = ioProcedure.IsRunning;
        ioProcedure.Stop();
        state = CaptureState.Stopped;
        InitRecordingInternal();
        if (oldState) { ioProcedure.Start(); }
    }

    private void OnDeviceWillBeDestroyed(AudioObject _)
    {
        flags |= RecordingFlags.Invalid;
        selectedStream = null;

        // If we have an I/O procedure attached, do the following.
        if (ioProcedure is not null && ioProcedure.IsRunning)
        {
            // Now, dispatch the recording stopped event with the event args containing
            // a Core Audio exception saying the device is no longer available.
            OnRecodingStopped(new CoreAudioException(
                "The device used to perform I/O is now gone.",
                MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError
            ));
        }
    }

    private void StopRecordingInternal()
    {
        try
        {
            state = CaptureState.Stopping;
            if (flags.HasFlag(RecordingFlags.EventHandleRegistered))
            {
                ioProcedure.Event -= FireDataAvailableEvent;
                ioProcedure.RecordingStopped -= OnRecordingStoppedHandlerFromIOProc;
            }
            ioProcedure.Stop();
            OnRecodingStopped(ex: null);
        }
        catch (Exception ex)
        {
            OnRecodingStopped(ex);
        }
        finally
        {
            flags &= ~RecordingFlags.EventHandleRegistered;
            state = CaptureState.Stopped;
        }
    }

    private void InitRecordingInternal()
    {
        MacUtils.EnsureDisposableObjectsDisposed(ioProcedure, streamsChanged, virtualFormatChanged);
        int selectedIndex;
        var streams = selectedDevice.GetStreams(AudioObjectPropertyScopeConstants.Input);
        if (streams.Length == 0)
        {
            throw new InvalidOperationException("The specified device does not provide any streams to perform capture on!");
        }

        if (streamSelector is null)
        {
            selectedStream = streams[selectedIndex = 0];
        }
        else
        {
            selectedIndex = streamSelector.Invoke(streams);
            try
            {
                selectedStream = streams[selectedIndex];
            }
            catch (IndexOutOfRangeException e)
            {
                throw new InvalidOperationException("The stream selector returned an invalid stream index: " + selectedIndex, e);
            }
        }

        InitializeIOProcedure(streams.Length, selectedIndex);
        virtualFormatChanged = selectedStream.ConstructVirtualFormatChangedEvent();
        virtualFormatChanged.Event += OnVirtualFormatChanged;
        try
        {
            streamsChanged = selectedDevice.ConstructStreamsChangedEvent(AudioObjectPropertyScopeConstants.Input);
            streamsChanged.Event += OnStreamsChanged;
        }
        catch
        {
            virtualFormatChanged.Dispose();
            throw;
        }
    }

    private void InitializeIOProcedure(int streamCount, int selectedIndex)
    {
        var virtualFormat = ConstructCaptureFormat();
        // bool hasLoopback = flags.HasFlag(RecordingFlags.IsUsingLoopback);
        ioProcedure = new(selectedDevice, virtualFormat, false);
        try
        {
            int I = 0;
            bool[] selectedStreams = new bool[streamCount];
            for (; I < streamCount; I++)
            {
                selectedStreams[I] = I == selectedIndex;
            }
            selectedDevice.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Input, selectedStreams);
        }
        catch
        {
            ioProcedure.Dispose();
            throw;
        }
    }

    private double GetLatencyInSeconds()
    {
        // Here, the direct virtual format from the stream suffices.
        return (
            selectedDevice.GetDeviceLatency(AudioObjectPropertyScopeConstants.Input) +
            selectedStream.Latency
        ) / selectedStream.VirtualFormat.SampleRate;
    }

    private WaveFormat ConstructCaptureFormat()
    {
        var vf = selectedStream.VirtualFormat;
        // mdcdi1315: We need to revisit this at the future, to see what we will do when
        // the resampler is needed for the speakers enum value.
        Speakers spk = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output, out _, out var needsExtensible);
        if (needsExtensible)
        {
            return vf is WaveFormatExtensible ext
                ? new WaveFormatExtensible(
                    ext.SampleRate,
                    ext.BitsPerSample,
                    ext.Channels,
                    ext.SubFormat,
                    ext.ValidBitsPerSample,
                    spk
                )
                : new WaveFormatExtensible(
                    vf.SampleRate,
                    vf.BitsPerSample,
                    vf.Channels,
                    vf.Encoding == WaveFormatEncoding.IeeeFloat,
                    vf.BitsPerSample,
                    spk
                );
        }
        return vf;
    }

    private void OnRecordingStoppedHandlerFromIOProc(object sender, StoppedEventArgs e) => OnRecodingStopped(e);

    private void OnRecodingStopped(Exception ex) => OnRecodingStopped(new StoppedEventArgs(ex));

    private void OnRecodingStopped(StoppedEventArgs e)
    {
        state = CaptureState.Stopped;
        RecordingStopped?.Invoke(this, e);
    }

    #endregion

    #region Public surface

    /// <summary>Call this method once per instance to set up the recorder for recording.</summary>
    /// <remarks>
    /// While this method can be called multiple times, 
    /// it is thread-safe and only the first thread that manages 
    /// to take the lock will perform the initialization task.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The recoder could not be set up (invalid device, invalid streams, etc.)</exception>
    public void InitializeRecording()
    {
        Monitor.Enter(this);
        try
        {
            if (flags.HasFlag(RecordingFlags.Initialized)) { return; }
            InitRecordingInternal();
            flags |= RecordingFlags.Initialized;
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    /// <summary>
    /// Call this method once per instance to set up the recorder for recording asynchronously.
    /// </summary>
    /// <returns>A new <see cref="ValueTask"/> that represents the code to execute for initializing the recoder.</returns>
    public ValueTask InitializeRecordingAsync() => new(Task.Run(InitializeRecording));

    /// <summary>
    /// Starts the recording. 
    /// This API uses the <see cref="DataAvailable"/> event to pull the captured audio data.
    /// </summary>
    /// <seealso cref="CaptureAsync"/>
    public void StartRecording()
    {
        ThrowIfInvalid();
        Monitor.Enter(this);
        try
        {
            // Prefer to exit cleanly rather than racing the startup.
            if (state != CaptureState.Stopped) { return; }
            state = CaptureState.Starting;
            ioProcedure.Event += FireDataAvailableEvent;
            ioProcedure.RecordingStopped += OnRecordingStoppedHandlerFromIOProc;
            ioProcedure.Start();
            state = CaptureState.Capturing;
            flags |= RecordingFlags.EventHandleRegistered;
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    /// <summary>
    /// Stops the recording, previously started by the <see cref="StartRecording"/> method.
    /// </summary>
    public void StopRecording()
    {
        ThrowIfInvalid();
        Monitor.Enter(this);
        try
        {
            // Prefer to exit cleanly rather than racing the stop.
            if (state == CaptureState.Stopped) { return; }
            StopRecordingInternal();
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    /// <summary>
    /// Asynchronously captures audio data from the specified input device, 
    /// initializing the recording if needed in the asynchronous code path.
    /// </summary>
    /// <param name="cancellationToken">The token that can be used to manually stop recording (Although it is also possible by calling the <see cref="StopRecording"/> method).</param>
    /// <returns>An enumerable instance returning capture buffers in an asynchronous manner.</returns>
    public async IAsyncEnumerable<CoreAudioCaptureBuffer> CaptureAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        state = CaptureState.Starting;

        // First, do any initialization if not explicitly done by the user,
        // and defer it's execution to the async path instead.
        await InitializeRecordingAsync();

        // Compute the latency of the device and the stream.
        // Use half of that latency to perform buffer dequeue checks more frequent.
        int averageLatency = (int)(GetLatencyInSeconds() * TimeSpan.MillisecondsPerSecond) / 2;

        // Create the buffer queue.
        SingleLinkedCaptureBufferQueue queue = new();

        try
        {
            ioProcedure.Event += queue.EnqueueFromHandler;
            ioProcedure.Start();
            state = CaptureState.Capturing;
            CoreAudioCaptureBuffer buffer;
            // Note that the below loop may be also stopped by using the IsRunning property.
            // This might happen for a couple of reasons:
            // 1. An exception occurred on the I/O thread (although practically impossible because we just enqueue to a queue)
            // 2. Recording stopped externally via the StopRecording method.
            // 3. Virtual stream format changed.
            // 4. Stream list has changed.
            while ((!cancellationToken.IsCancellationRequested) && ioProcedure.IsRunning)
            {
                // See whether we have any capture buffer(s) available.
                // Make sure we drain the whole output.
                while ((buffer = queue.Dequeue()) is not null)
                {
                    // Return the buffer.
                    yield return buffer;
                }
                // Wait for half the average latency.
                await Task.Delay(averageLatency, cancellationToken);
            }
            // Make sure that the buffer queue does not contain any buffers that we did not submitted
            while ((buffer = queue.Dequeue()) is not null) { yield return buffer; }
        }
        finally
        {
            state = CaptureState.Stopping;
            try
            {
                ioProcedure.Event -= queue.EnqueueFromHandler;
                queue.Dispose();
            }
            finally
            {
                state = CaptureState.Stopped;
                ioProcedure.Stop();
            }
        }
        // If we have an exception, make sure to throw it
        var ex = ioProcedure.Exception;
        if (ex is not null) { throw ex; }
        // Flag to the enumerator that we won't return additional buffers
        yield break;
    }

    /// <summary>
    /// Provides an event that is fired when new capture data are retrieved from the HAL.
    /// </summary>
    public event CoreAudioCaptureDataAvailableHandler DataAvailable;

    /// <summary>
    /// Provides an event that is fired when the recorder's capture format has been changed.
    /// </summary>
    /// <remarks>
    /// If capture is already running on this instance, capture will be stopped. <br />
    /// If you want to restart recording, explicitly call the <see cref="StartRecording"/> method.
    /// </remarks>
    public event CaptureFormatChangedHandler CaptureFormatChanged;

    /// <summary>
    /// Provides an event that is fired when the recording has been stopped.
    /// </summary>
    public event EventHandler<StoppedEventArgs> RecordingStopped;

    /// <summary>
    /// Provides the audio format the HAL uses to capture audio. <br />
    /// This is the format the audio data are provided in the <see cref="DataAvailable"/>.
    /// </summary>
    public WaveFormat CaptureFormat
    {
        get
        {
            ThrowIfInvalid();
            return ConstructCaptureFormat();
        }
    }

    /// <summary>
    /// Gets a value whether the recorder is recording from the initialized device.
    /// </summary>
    public CaptureState CaptureState
    {
        get
        {
            ThrowIfInvalid();
            return state;
        }
    }

    /// <inheritdoc />
    public TimeSpan AverageLatency
    {
        get
        {
            ThrowIfInvalid();
            return TimeSpan.FromSeconds(GetLatencyInSeconds());
        }
    }

    /// <inheritdoc />
    // Latency can be acquired when the DataAvailable event dispatches.
    public TimeSpan CurrentLatency => AverageLatency;

    /// <summary>
    /// Disposes this <see cref="CoreAudioRecorder"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        Monitor.Enter(this);
        try
        {
            if (selectedStream is null) { return; }
            selectedStream = null;
            if (flags.HasFlag(RecordingFlags.Initialized) && state == CaptureState.Capturing) { StopRecordingInternal(); }
            MacUtils.EnsureDisposableObjectsDisposed(
                ioProcedure,
                streamsChanged,
                virtualFormatChanged,
                deviceWasRemoved
            );
            selectedStream = null;
        }
        finally
        {
            Monitor.Exit(this);
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
    public ValueTask DisposeAsync() => new(Task.Run(Dispose));

    #endregion
}