
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.Wave;

/// <summary>
/// An audio recorder based on Audio Queue Services. Create it with <see cref="AudioQueueRecorderBuilder"/>. <br />
/// The capture format is chosen when the recorder is built; the queue converts to it if the device changes.
/// </summary>
/// <seealso cref="AudioQueueRecorderBuilder"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueueRecorder : AudioQueueBase, IWaveLatency
{
    private readonly Lock lockObject;
    private readonly IntPtr[] buffers;
    private readonly double bufferSeconds;
    private readonly SynchronizationContext? syncContext;
    private readonly AudioStreamBasicDescription queueFormat;
    private readonly PropertyListenerHandle? deviceWillGoAway;

    private int runId;
    private bool disposed;
    private bool invalidated;
    private GCHandle thisQueueHandle;
    private volatile CaptureState state;

    // This field is read by the callback of the queue.
    private volatile Exception? failure;

    // Set while CaptureAsync runs; read by the callback of the queue.
    private volatile AudioQueueCaptureSink? captureSink;

    internal unsafe AudioQueueRecorder(
        int latencyMilliseconds,
        int numberOfBuffers,
        AudioDevice? device,
        SynchronizationContext? syncContext
    )
    {
        AudioDevice effectiveDevice = device ?? AudioSystemObject.Instance.DefaultInputDevice ?? throw new InvalidOperationException("No default input device is available.");
        if (!effectiveDevice.IsAlive)
        {
            throw new InvalidOperationException("This device will be removed soon, and as such cannot be used as a capture device.");
        }

        Device = device;
        state = CaptureState.Stopped;
        this.syncContext = syncContext;
        LatencyMilliseconds = latencyMilliseconds;

        lockObject = new();
        buffers = new IntPtr[numberOfBuffers];

        queueFormat = SelectFormat(effectiveDevice);
        long frames = Math.Max(1L, (long)queueFormat.mSampleRate * latencyMilliseconds / (TimeSpan.MillisecondsPerSecond * numberOfBuffers));
        bufferSeconds = frames / queueFormat.mSampleRate;

        try
        {
            thisQueueHandle = GCHandle.Alloc(this);

            AudioQueueException.ThrowIfError(NativeMethods.AudioQueueNewInput(in queueFormat, &OnBufferFilled, GCHandle.ToIntPtr(thisQueueHandle), IntPtr.Zero, IntPtr.Zero, 0U, out queueHandle));

            if (device is not null)
            {
                SetStringProperty(AudioQueueProperties.kAudioQueueProperty_CurrentDevice, device.DeviceUID);
            }

            int bytes = checked((int)(frames * queueFormat.mBytesPerFrame));
            for (int I = 0; I < buffers.Length; I++)
            {
                buffers[I] = (IntPtr)AllocateBuffer(bytes);
            }

            if (device is not null)
            {
                deviceWillGoAway = device.ConstructIsAliveChangedEvent();
                deviceWillGoAway.Event += OnDeviceWillBeDestroyed;
            }
        }
        catch
        {
            DestroyQueue(true);
            throw;
        }
    }

    #region Internal support code

    private static AudioStreamBasicDescription SelectFormat(AudioDevice device)
    {
        uint channels = 0U;
        AudioStreamBasicDescription description = default;

        foreach (var stream in device.GetStreams(AudioObjectPropertyScopeConstants.Input))
        {
            var format = stream.VirtualFormatNative;
            if (format.mFormatID != AudioFormatIDs.kAudioFormatLinearPCM) { continue; }
            if (description.mFormatID == 0U) { description = format; }
            channels += format.mChannelsPerFrame;
        }

        if (channels == 0U)
        {
            throw new InvalidOperationException("The specified device does not provide any streams to perform capture on!");
        }
        else if (description.mFormatID == 0U)
        {
            throw new InvalidOperationException("The specified device does not provide a proper PCM/IEEE floating-point audio format!");
        }

        if (description.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved))
        {
            description.mFormatFlags &= ~AudioFormatFlags.kAudioFormatFlagIsNonInterleaved;
            description.mBytesPerFrame = description.mBytesPerPacket = channels * description.mBytesPerFrame;
        }

        description.mChannelsPerFrame = channels;

        return description;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnBufferFilled(IntPtr userData, IntPtr queue, AudioQueueBuffer* buffer, AudioTimeStamp* startTime, uint packetCount, AudioStreamPacketDescription* packetDescriptions)
    {
        if (GCHandle.FromIntPtr(userData).Target is not AudioQueueRecorder recorder || recorder.failure is not null) { return; }

        try
        {
            if (buffer->mAudioDataByteSize != 0)
            {
                double sampleRate = recorder.queueFormat.mSampleRate;
                double firstByte = StampToSeconds(*startTime, sampleRate);
                double current = firstByte + ((double)buffer->mAudioDataByteSize / recorder.queueFormat.mBytesPerFrame / sampleRate);
                ReadOnlySpan<byte> audioData = buffer->GetSpanToReadBuffer();

                // The asynchronous capture is fed first and does not depend on the public event,
                // so a failing user handler cannot drop buffers that were already captured.
                recorder.captureSink?.Write(audioData, current, firstByte);
                recorder.DataAvailable?.Invoke(audioData, current, firstByte);
            }
        }
        catch (Exception ex)
        {
            recorder.failure = ex;
            _ = ThreadPool.UnsafeQueueUserWorkItem(static state => state.recorder.CompleteAfterFailure(state.run), (recorder, run: recorder.runId), false);
            return;
        }

        if (recorder.state == CaptureState.Capturing)
        {
            _ = NativeMethods.AudioQueueEnqueueBuffer(queue, buffer, 0, null);
        }
    }

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowIfInvalidOrDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (invalidated)
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
    }

    private void FireRecordingStopped(Exception? exception)
    {
        var handler = RecordingStopped;
        if (handler is null) { return; }
        var args = new StoppedEventArgs(exception);
        if (syncContext is null)
        {
            handler.Invoke(this, args);
        }
        else
        {
            syncContext.Post(_ => handler.Invoke(this, args), null);
        }
    }

    private void OnDeviceWillBeDestroyed(AudioObject _)
    {
        lockObject.Enter();
        try
        {
            invalidated = true;
            if (state == CaptureState.Stopped || disposed) { return; }
            StopQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        var exception = new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        captureSink?.Complete(exception);
        FireRecordingStopped(exception);
    }

    private void DestroyQueue(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (thisQueueHandle.IsAllocated) { thisQueueHandle.Free(); }
        }
    }

    private void StopQueue()
    {
        try
        {
            state = CaptureState.Stopping;
            Stop(true);
        }
        catch { }
        finally
        {
            state = CaptureState.Stopped;
        }
    }

    private void CompleteAfterFailure(int run)
    {
        Exception? exception;
        lockObject.Enter();
        try
        {
            if (run != runId || state == CaptureState.Stopped || disposed) { return; }
            exception = failure;
            StopQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        captureSink?.Complete(exception);
        FireRecordingStopped(exception);
    }

    /// <summary>
    /// Stops the capture started by <see cref="CaptureAsync(CancellationToken)"/> when it ends. <br />
    /// Unlike <see cref="StopRecording"/>, it never throws, so it cannot hide the exception that ended the enumeration,
    /// and it leaves alone a capture that was stopped and started again in the meantime.
    /// </summary>
    private void StopCaptureRun(int run)
    {
        lockObject.Enter();
        try
        {
            if (run != runId || state == CaptureState.Stopped || disposed) { return; }
            StopQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        captureSink?.Complete(null);
        FireRecordingStopped(null);
    }

    private double GetLatencyInSeconds()
    {
        AudioDevice device = Device ?? AudioSystemObject.Instance.DefaultInputDevice ?? throw new InvalidOperationException("No default input device is available.");
        return bufferSeconds + (device.GetDeviceLatency(AudioObjectPropertyScopeConstants.Input) / queueFormat.mSampleRate);
    }

    #endregion

    #region Public API

    /// <summary>The total amount of audio queued for capture, in milliseconds.</summary>
    public int LatencyMilliseconds { get; }

    /// <summary>The number of buffers that the latency is divided into.</summary>
    public int NumberOfBuffers => buffers.Length;

    /// <summary>The device to capture from, or <see langword="null"/> when following the default input device.</summary>
    public AudioDevice? Device { get; }

    /// <summary>The format of the captured audio.</summary>
    public WaveFormat CaptureFormat
    {
        get
        {
            ChannelLayoutHandle? handle;
            Speakers speakers = Speakers.None;
            if (queueHandle != IntPtr.Zero && (handle = GetChannelLayout()) is not null)
            {
                try
                {
                    speakers = MacUtils.ConstructSpeakersValue(
                        handle.DangerousGetHandle(),
                        out var needsTranslation,
                        out _
                    );
                    // Do not advertise speakers if they do not need translation.
                    if (needsTranslation) { speakers = Speakers.None; }
                }
                catch { speakers = Speakers.None; }
                finally
                {
                    handle.Dispose();
                }
            }
            return MacUtils.ConstructWaveFormatFromASBD(queueFormat, speakers);
        }
    }

    /// <summary>The current state of the capture.</summary>
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

    /// <summary>Raised when the capture stops.</summary>
    public event EventHandler<StoppedEventArgs>? RecordingStopped;

    /// <summary>
    /// Raised on a thread of the queue when a buffer of captured audio is available. <br />
    /// The handler must not stop or dispose the recorder, because both wait for the handler to return.
    /// </summary>
    public event AudioQueueCaptureDataAvailableHandler? DataAvailable;

    /// <summary>
    /// Starts capturing and returns the captured buffers asynchronously. <br />
    /// The capture stops when the enumeration ends: by leaving the loop, through <paramref name="cancellationToken"/>,
    /// or when the recorder stops (by <see cref="StopRecording"/>, by <see cref="AudioQueueBase.Dispose()"/>, or because of a failure).
    /// </summary>
    /// <remarks>
    /// The buffers are received directly from the queue rather than through <see cref="DataAvailable"/>,
    /// whose handlers keep running during the capture. <br />
    /// If the capture fails (a <see cref="DataAvailable"/> handler throws, or the device goes away), the buffers
    /// captured before the failure are returned first, and then the exception that stopped the capture is thrown.
    /// </remarks>
    /// <param name="cancellationToken">The token that stops the capture; cancelling it throws <see cref="OperationCanceledException"/>.</param>
    /// <returns>An enumerable instance returning capture buffers in an asynchronous manner.</returns>
    /// <exception cref="ObjectDisposedException">This recorder instance has been disposed of.</exception>
    /// <exception cref="InvalidOperationException">This recorder instance is already capturing.</exception>
    /// <exception cref="CoreAudioException">The device that is used by the recorder is not existing.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async IAsyncEnumerable<AudioQueueCaptureBuffer> CaptureAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        int run = -1;
        AudioQueueCaptureSink sink = new();

        lockObject.Enter();
        try
        {
            ThrowIfInvalidOrDisposed();
            if (state != CaptureState.Stopped || captureSink is not null)
            {
                throw new InvalidOperationException("The recorder is already capturing.");
            }
            captureSink = sink;
        }
        finally
        {
            lockObject.Exit();
        }

        try
        {
            StartRecording();
            run = runId;

            // The sink is completed by every path that stops the recorder, with the exception
            // that stopped it, if any; the reader then drains the remaining buffers and ends or throws.
            while (await sink.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (sink.Reader.TryRead(out var buffer)) { yield return buffer; }
            }
        }
        finally
        {
            StopCaptureRun(run);
            captureSink = null;
        }
    }

    /// <summary>Starts capturing. Does nothing if the capture is already running.</summary>
    public unsafe void StartRecording()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            if (state != CaptureState.Stopped) { return; }

            runId++;
            failure = null;

            try
            {
                foreach (var buffer in buffers) { EnqueueBuffer((AudioQueueBuffer*)buffer); }

                Start();
                state = CaptureState.Capturing;
            }
            catch
            {
                StopQueue();
                throw;
            }
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <summary>Stops capturing. Returns once the queue has delivered what it had captured.</summary>
    public void StopRecording()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            if (state == CaptureState.Stopped) { return; }
            StopQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        captureSink?.Complete(null);
        FireRecordingStopped(null);
    }

    #endregion

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2215:Dispose methods should call base class dispose",
        Justification = "Called via DisposeInternal"
    )]
    protected override void Dispose(bool disposing)
    {
        bool wasCapturing;
        lockObject.Enter();
        try
        {
            if (disposed) { return; }
            disposed = true;
            wasCapturing = state != CaptureState.Stopped;
            state = CaptureState.Stopping;
            DestroyQueue(disposing);
        }
        finally
        {
            lockObject.Exit();
            state = CaptureState.Stopped;
        }

        // The listener is removed outside of the lock, because
        // removing it waits for a running notification, which itself takes the lock.
        deviceWillGoAway?.Dispose();
        captureSink?.Complete(null);
        if (wasCapturing) { FireRecordingStopped(null); }
    }
}
