
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.MacOS;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.CoreFoundationApi;
using NAudio.MacOS.AudioToolbox.Interop;

using static NAudio.MacOS.AudioToolbox.Interop.NativeMethods;

namespace NAudio.Wave;

/// <summary>
/// An audio recorder based on Audio Queue Services. Create it with <see cref="AudioQueueRecorderBuilder"/>. <br />
/// Audio is delivered as 32-bit float interleaved samples on a thread of the queue, so a garbage
/// collection or a slow handler does not lose audio unless it takes longer than the queued buffers.
/// The capture format is chosen when the recorder is built; the queue converts to it if the device changes.
/// </summary>
/// <seealso cref="AudioQueueRecorderBuilder"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueueRecorder : IDisposable, IAsyncDisposable, IWaveLatency
{
    private readonly Lock lockObject;
    private readonly SynchronizationContext? syncContext;
    private readonly PropertyListenerHandle? deviceWillGoAway;
    private readonly AudioStreamBasicDescription queueFormat;
    private readonly double bufferSeconds;
    private readonly IntPtr[] buffers;
    private IntPtr queue;
    private GCHandle queueHandle;
    private int runId;
    private bool disposed;
    private bool invalidated;
    private CaptureState state;
    // These are read by the callback of the queue.
    private volatile bool stopping;
    private volatile Exception? failure;

    internal unsafe AudioQueueRecorder(
        int latencyMilliseconds,
        int numberOfBuffers,
        AudioDevice? device,
        bool levelMeteringEnabled,
        SynchronizationContext? syncContext
    )
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        AudioDevice effectiveDevice = device ?? AudioSystemObject.Instance.DefaultInputDevice ?? throw new InvalidOperationException("No default input device is available.");
        if (!effectiveDevice.IsAlive)
        {
            throw new InvalidOperationException("This device will be removed soon, and as such cannot be used as a capture device.");
        }

        LatencyMilliseconds = latencyMilliseconds;
        NumberOfBuffers = numberOfBuffers;
        Device = device;
        LevelMeteringEnabled = levelMeteringEnabled;
        this.syncContext = syncContext;
        lockObject = new();
        state = CaptureState.Stopped;
        buffers = new IntPtr[numberOfBuffers];

        queueFormat = SelectFormat(effectiveDevice);
        CaptureFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)queueFormat.mSampleRate, (int)queueFormat.mChannelsPerFrame);
        long frames = Math.Max(1L, (long)queueFormat.mSampleRate * latencyMilliseconds / (1000L * numberOfBuffers));
        bufferSeconds = frames / queueFormat.mSampleRate;

        try
        {
            queueHandle = GCHandle.Alloc(this);
            AudioQueueException.ThrowIfError(AudioQueueNewInput(in queueFormat, &OnBufferFilled, GCHandle.ToIntPtr(queueHandle), IntPtr.Zero, IntPtr.Zero, 0, out queue));

            if (device is not null)
            {
                using CFString uid = CFString.CreateFromString(device.DeviceUID);
                IntPtr uidRef = uid.NativeObject;
                AudioQueueException.ThrowIfError(AudioQueueSetProperty(queue, AudioQueueProperties.kAudioQueueProperty_CurrentDevice, (IntPtr)(&uidRef), (uint)sizeof(IntPtr)));
            }

            if (levelMeteringEnabled)
            {
                uint enabled = 1U;
                AudioQueueException.ThrowIfError(AudioQueueSetProperty(queue, AudioQueueProperties.kAudioQueueProperty_EnableLevelMetering, (IntPtr)(&enabled), sizeof(uint)));
            }

            uint bytes = checked((uint)(frames * queueFormat.mBytesPerFrame));
            for (int I = 0; I < buffers.Length; I++)
            {
                AudioQueueException.ThrowIfError(AudioQueueAllocateBuffer(queue, bytes, out AudioQueueBuffer* buffer));
                buffers[I] = (IntPtr)buffer;
            }

            if (device is not null)
            {
                deviceWillGoAway = device.ConstructIsAliveChangedEvent();
                deviceWillGoAway.Event += OnDeviceWillBeDestroyed;
            }
        }
        catch
        {
            DestroyQueue();
            throw;
        }
    }

    #region Internal support code

    private static AudioStreamBasicDescription SelectFormat(AudioDevice device)
    {
        double sampleRate = 0d;
        uint channels = 0U;
        foreach (var stream in device.GetStreams(AudioObjectPropertyScopeConstants.Input))
        {
            var format = stream.VirtualFormatNative;
            if (format.mFormatID != AudioFormatIDs.kAudioFormatLinearPCM) { continue; }
            if (sampleRate == 0d) { sampleRate = format.mSampleRate; }
            channels += format.mChannelsPerFrame;
        }

        if (channels == 0U)
        {
            throw new InvalidOperationException("The specified device does not provide any streams to perform capture on!");
        }

        return AudioStreamBasicDescription.FillOutASBDForLPCM(sampleRate, channels, 32U, 32U, true, !BitConverter.IsLittleEndian);
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

        FireRecordingStopped(new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError));
    }

    private void DestroyQueue()
    {
        if (queue != IntPtr.Zero)
        {
            // An immediate disposal waits for a running callback and guarantees that no further ones occur.
            _ = AudioQueueDispose(queue, MacBoolean.True);
            queue = IntPtr.Zero;
        }

        if (queueHandle.IsAllocated) { queueHandle.Free(); }
    }

    private void StopQueue()
    {
        stopping = true;
        _ = AudioQueueStop(queue, MacBoolean.True);
        state = CaptureState.Stopped;
    }

    private double ToSeconds(in AudioTimeStamp stamp)
    {
        if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid))
        {
            return stamp.mSampleTime / queueFormat.mSampleRate;
        }

        return stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid)
            ? CoreAudioFunctions.ConvertHostTimeToSeconds(stamp.mHostTime)
            : 0d;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnBufferFilled(IntPtr userData, IntPtr queue, AudioQueueBuffer* buffer, AudioTimeStamp* startTime, uint packetCount, AudioStreamPacketDescription* packetDescriptions)
    {
        if (GCHandle.FromIntPtr(userData).Target is not AudioQueueRecorder recorder || recorder.failure is not null) { return; }

        try
        {
            if (buffer->mAudioDataByteSize != 0)
            {
                double firstByte = recorder.ToSeconds(*startTime);
                double current = firstByte + ((double)buffer->mAudioDataByteSize / recorder.queueFormat.mBytesPerFrame / recorder.queueFormat.mSampleRate);
                recorder.DataAvailable?.Invoke(new ReadOnlySpan<byte>(buffer->mAudioData, (int)buffer->mAudioDataByteSize), current, firstByte);
            }
        }
        catch (Exception ex)
        {
            recorder.failure = ex;
            _ = ThreadPool.UnsafeQueueUserWorkItem(static state => state.recorder.CompleteAfterFailure(state.run), (recorder, run: recorder.runId), false);
            return;
        }

        if (!recorder.stopping)
        {
            _ = AudioQueueEnqueueBuffer(queue, buffer, 0, null);
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

        FireRecordingStopped(exception);
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
    public int NumberOfBuffers { get; }

    /// <summary>The device to capture from, or <see langword="null"/> when following the default input device.</summary>
    public AudioDevice? Device { get; }

    /// <summary>Whether level metering is enabled.</summary>
    public bool LevelMeteringEnabled { get; }

    /// <summary>The format of the captured audio.</summary>
    public WaveFormat CaptureFormat { get; }

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

    /// <summary>
    /// Raised on a thread of the queue when a buffer of captured audio is available. <br />
    /// The handler must not stop or dispose the recorder, because both wait for the handler to return.
    /// </summary>
    public event CoreAudioCaptureDataAvailableHandler? DataAvailable;

    /// <summary>Raised when the capture stops.</summary>
    public event EventHandler<StoppedEventArgs>? RecordingStopped;

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
            stopping = false;

            try
            {
                foreach (IntPtr buffer in buffers)
                {
                    AudioQueueException.ThrowIfError(AudioQueueEnqueueBuffer(queue, (AudioQueueBuffer*)buffer, 0, null));
                }

                AudioQueueException.ThrowIfError(AudioQueueStart(queue, null));
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

        FireRecordingStopped(null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        bool wasCapturing;
        lockObject.Enter();
        try
        {
            if (disposed) { return; }
            disposed = true;
            wasCapturing = state != CaptureState.Stopped;
            stopping = true;
            state = CaptureState.Stopped;
            DestroyQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        // The listener is removed outside of the lock, because
        // removing it waits for a running notification, which itself takes the lock.
        deviceWillGoAway?.Dispose();
        if (wasCapturing) { FireRecordingStopped(null); }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(Task.Run(Dispose));

    #endregion
}
