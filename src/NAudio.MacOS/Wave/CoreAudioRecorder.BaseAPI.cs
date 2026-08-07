
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// Provides a class for capturing audio from a specified audio device using the 
/// macOS HAL API for accessing the captured data.
/// </summary>
/// <seealso cref="MacOS.CoreAudio"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed partial class CoreAudioRecorder : IDisposable, IAsyncDisposable, IWaveLatency
{
    private CaptureState state;
    private readonly object lockObject;
    private RecorderProcedure ioProcedure;
    private CoreAudioRecorderStateFlags flags;
    private readonly AudioDevice selectedDevice;
    private PropertyListenerHandle streamsChanged;
    private PropertyListenerHandle virtualFormatChanged;
    private readonly PropertyListenerHandle deviceWasRemoved;

    /// <summary>
    /// Initializes a new Core Audio recoder instance, using the default input
    /// device to capture audio data.
    /// </summary>
    public CoreAudioRecorder() : this(AudioSystemObject.Instance.DefaultInputDevice) { }

    /// <summary>
    /// Initializes a new Core Audio recoder instance from the specified device.
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
        ioProcedure = null;
        lockObject = new();
        streamsChanged = null;
        selectedDevice = device;
        virtualFormatChanged = null;
        state = CaptureState.Stopped;
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
        Invalidated = 1 << 2,
        NonInterleaved = 1 << 3,
        EventHandleWasRegistered = 1 << 4
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
        if (!flags.HasFlag(CoreAudioRecorderStateFlags.Initialized))
        {
            throw new InvalidOperationException("The Core Audio recorder has not been initialized yet.");
        }
        else if (flags.HasFlag(CoreAudioRecorderStateFlags.Invalidated))
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
    }

    private void OnRecordingStoppedHandlerFromIOProc(object sender, StoppedEventArgs e) => OnRecodingStopped(e);

    private void OnRecodingStopped(Exception ex) => OnRecodingStopped(new StoppedEventArgs(ex));

    private void OnRecodingStopped(StoppedEventArgs e)
    {
        state = CaptureState.Stopped;
        RecordingStopped?.Invoke(this, e);
    }

    private double GetLatencyInSeconds()
    {
        // Here, the direct virtual format from the selected stream suffices.
        return (
            selectedDevice.GetDeviceLatency(AudioObjectPropertyScopeConstants.Input) +
            ioProcedure.StreamLatency
        ) / ioProcedure.VirtualFormat.mSampleRate;
    }

    private void StopRecordingInternal()
    {
        try
        {
            state = CaptureState.Stopping;
            if (flags.HasFlag(CoreAudioRecorderStateFlags.EventHandleWasRegistered))
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
            flags &= ~CoreAudioRecorderStateFlags.EventHandleWasRegistered;
            state = CaptureState.Stopped;
        }
    }

    // After full initialization of the procedure, 
    // implementers must call this to finalize initialization
    // and register any crucial playback events.
    private void OnInitializationComplete()
    {
        streamsChanged?.Dispose();
        streamsChanged = selectedDevice.ConstructStreamsChangedEvent(AudioObjectPropertyScopeConstants.Input);
        streamsChanged.Event += OnStreamsChanged;
        flags |= CoreAudioRecorderStateFlags.Initialized;
    }

    // Creates a WaveFormat that is equivalent to the format that the 
    // HAL is capturing data into. 
    // The method does all the necessary things
    // to translate the format to the actual one,
    // after all the conversions this class does
    // have taken place.
    private WaveFormat ConstructCaptureFormat()
    {
        var virtualFormat = ioProcedure.VirtualFormat;
        Speakers spk = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Input, out _, out var needsExtensible);
        int bits = (int)virtualFormat.mBitsPerChannel;
        int sampleRate = (int)virtualFormat.mSampleRate;
        int channels = (int)virtualFormat.mChannelsPerFrame;
        bool IeeeFloat = virtualFormat.mFormatFlags.HasFlag(MacOS.CoreAudioTypes.AudioFormatFlags.kAudioFormatFlagIsFloat);
        if (needsExtensible)
        {
            return new WaveFormatExtensible(
                sampleRate,
                bits,
                channels,
                IeeeFloat,
                bits,
                spk
            );
        }
        else
        {
            int blkAlign = channels * (bits / 8);
            return WaveFormat.CreateCustomFormat(
                IeeeFloat ? WaveFormatEncoding.IeeeFloat : WaveFormatEncoding.Pcm,
                sampleRate,
                channels,
                sampleRate * blkAlign,
                blkAlign,
                bits
            );
        }
    }

    #endregion

    #region Public API

    /// <summary>Call this method once per instance to set up the recorder for recording.</summary>
    /// <remarks>
    /// This method can be called multiple times, 
    /// it is thread-safe and only the first thread that manages 
    /// to take the lock will perform the initialization task.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The recorder could not be set up (invalid device, invalid streams, etc.)</exception>
    public void InitializeRecording()
    {
        Monitor.Enter(lockObject);
        try
        {
            if (flags.HasFlag(CoreAudioRecorderStateFlags.Initialized)) { return; }
            Initialize();
            // The below exception is thrown if the we forget to
            // call the OnInitializationComplete method inside Initialize().
            if (!flags.HasFlag(CoreAudioRecorderStateFlags.Initialized))
            {
                throw new InvalidOperationException("The recorder instance was not initialized successfully!");
            }
        }
        finally
        {
            Monitor.Exit(lockObject);
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
    public void StartRecording()
    {
        ThrowIfInvalid();
        Monitor.Enter(lockObject);
        try
        {
            // Prefer to exit cleanly rather than racing the startup.
            if (state != CaptureState.Stopped) { return; }
            state = CaptureState.Starting;
            // Attach event handlers to the I/O procedure.
            // This is the DataAvailable event mode.
            ioProcedure.Event += FireDataAvailableEvent;
            ioProcedure.RecordingStopped += OnRecordingStoppedHandlerFromIOProc;
            ioProcedure.Start();
            state = CaptureState.Capturing;
            flags |= CoreAudioRecorderStateFlags.EventHandleWasRegistered;
        }
        finally
        {
            Monitor.Exit(lockObject);
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
    /// <exception cref="InvalidOperationException">This recorder instance is already been in use.</exception>
    public async IAsyncEnumerable<CoreAudioCaptureBuffer> CaptureAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        ThrowIfInvalid();
        if (state != CaptureState.Stopped)
        {
            throw new InvalidOperationException("The recorder is attempting to initialize, or already recording!");
        }

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
    /// This is the format the audio data are provided in the <see cref="DataAvailable"/> event.
    /// </summary>
    /// <remarks>
    /// It is important to note that the capture format may be an instance
    /// of the <see cref="WaveFormatExtensible"/> class and could report
    /// a channel mask - however the mask might be unreliable because
    /// it is deduced by the internal translation API's and the actual 
    /// channels might be in different order in the audio data than what 
    /// the <see cref="Speakers"/> enumeration requires; however, the 
    /// number of channels is a reliable option to work with, 
    /// so you can use it to only record the channels that you actually need.
    /// </remarks>
    /// <exception cref="InvalidOperationException">This <see cref="CoreAudioRecorder"/> instance has not yet been initialized.</exception>
    public WaveFormat CaptureFormat
    {
        get
        {
            ThrowIfInvalid();
            return ConstructCaptureFormat();
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
            ThrowIfInvalid();
            return TimeSpan.FromSeconds(GetLatencyInSeconds());
        }
    }

    /// <inheritdoc />
    public TimeSpan CurrentLatency
    {
        get
        {
            ThrowIfInvalid();
            double latencySeconds = ioProcedure.GetCurrentLatency();
            if (latencySeconds == -1d)
            {
                latencySeconds = GetLatencyInSeconds();
            }
            return TimeSpan.FromSeconds(latencySeconds);
        }
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioRecorder"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        Monitor.Enter(lockObject);
        try
        {
            if (flags.HasFlag(CoreAudioRecorderStateFlags.Disposed)) { return; }
            if (flags.HasFlag(CoreAudioRecorderStateFlags.Initialized) && state == CaptureState.Capturing) { StopRecordingInternal(); }
            state = CaptureState.Stopping;
            MacUtils.EnsureDisposableObjectsDisposed(
                ioProcedure,
                streamsChanged,
                virtualFormatChanged,
                deviceWasRemoved
            );
            flags |= CoreAudioRecorderStateFlags.Disposed;
        }
        finally
        {
            state = CaptureState.Stopped;
            Monitor.Exit(lockObject);
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