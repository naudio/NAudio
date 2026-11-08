using System;
using System.Threading;
using System.Runtime.InteropServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreFoundationApi;
using NAudio.MacOS.CoreAudioTypes;

using static NAudio.MacOS.AudioToolbox.Interop.NativeMethods;

namespace NAudio.Wave;

// Owns the Audio Queue. The buffer callback runs on a thread of the queue and
// never takes lockObject, because stopping or disposing the queue waits for it.
public partial class CoreAudioRecorder
{
    private void OnDeviceWillBeDestroyed(AudioObject _)
    {
        flags |= CoreAudioRecorderStateFlags.Invalidated;

        lock (lockObject)
        {
            if (!capturing) { return; }
            StopQueue();
        }

        OnRecodingStopped(new CoreAudioException(
            "The device used to perform I/O is now gone.",
            MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError
        ));
    }

    private void OnVirtualFormatChanged(AudioObject stream)
    {
        // The user will restart the recording if needed.
        if (!Reinitialize()) { return; }
        CaptureFormatChanged?.Invoke(this);
    }

    private void OnStreamsChanged(AudioObject _)
    {
        bool wasRunning = capturing;
        if (Reinitialize() && wasRunning) { StartRecording(); }
    }

    private bool Reinitialize()
    {
        lock (lockObject)
        {
            if (state != CaptureState.Stopped)
            {
                StopRecordingInternal();
            }

            try
            {
                Initialize();
                return true;
            }
            catch (Exception e)
            {
                // Clear the initialized flag to avoid getting the recorder into an unspecified state.
                flags &= ~CoreAudioRecorderStateFlags.Initialized;
                OnRecodingStopped(e);
                return false;
            }
        }
    }

    private static void ThrowIfQueueError(int osStatus, string function)
    {
        if (osStatus != 0)
        {
            throw new CoreAudioException($"{function} failed with OSStatus {osStatus}.", osStatus);
        }
    }

    private unsafe void Initialize()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(DesiredLatency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(NumberOfBuffers);

        DestroyQueue();
        MacUtils.EnsureDisposableObjectsDisposed(virtualFormatChanged, streamsChanged);
        virtualFormatChanged = null;
        streamsChanged = null;

        var streams = selectedDevice.GetStreams(AudioObjectPropertyScopeConstants.Input);
        int selectedIndex = -1;
        double sampleRate = 0d;
        uint channels = 0U;
        for (int I = 0; I < streams.Length; I++)
        {
            var streamFormat = streams[I].VirtualFormatNative;
            if (streamFormat.mFormatID != AudioFormatIDs.kAudioFormatLinearPCM) { continue; }
            if (selectedIndex < 0)
            {
                selectedIndex = I;
                sampleRate = streamFormat.mSampleRate;
            }
            channels += streamFormat.mChannelsPerFrame;
        }

        if (selectedIndex == -1)
        {
            throw new InvalidOperationException("The specified device does not provide any streams to perform capture on!");
        }

        try
        {
            AudioStreamBasicDescription format = AudioStreamBasicDescription.FillOutASBDForLPCM(
                sampleRate, channels, 32U, 32U, true, !BitConverter.IsLittleEndian);
            queueFormat = format;

            queueHandle = GCHandle.Alloc(this);

            IntPtr created;
            ThrowIfQueueError(
                AudioQueueNewInput(&format, &OnBufferFilled, GCHandle.ToIntPtr(queueHandle), IntPtr.Zero, IntPtr.Zero, 0, &created),
                nameof(AudioQueueNewInput));
            queue = created;

            using (CFString uid = CFString.CreateFromString(selectedDevice.DeviceUID))
            {
                IntPtr uidRef = uid.NativeObject;
                ThrowIfQueueError(
                    AudioQueueSetProperty(queue, kAudioQueueProperty_CurrentDevice, &uidRef, (uint)sizeof(IntPtr)),
                    nameof(AudioQueueSetProperty));
            }

            long frames = Math.Max(1L, (long)sampleRate * DesiredLatency / (1000L * NumberOfBuffers));
            bufferSeconds = frames / sampleRate;
            uint bytes = checked((uint)(frames * format.mBytesPerFrame));
            buffers = new IntPtr[NumberOfBuffers];
            for (int i = 0; i < buffers.Length; i++)
            {
                AudioQueueBuffer* buffer;
                ThrowIfQueueError(AudioQueueAllocateBuffer(queue, bytes, &buffer), nameof(AudioQueueAllocateBuffer));
                buffers[i] = (IntPtr)buffer;
            }

            virtualFormatChanged = streams[selectedIndex].ConstructVirtualFormatChangedEvent();
            virtualFormatChanged.Event += OnVirtualFormatChanged;
            streamsChanged = selectedDevice.ConstructStreamsChangedEvent(AudioObjectPropertyScopeConstants.Input);
            streamsChanged.Event += OnStreamsChanged;
            flags |= CoreAudioRecorderStateFlags.Initialized;
        }
        catch
        {
            DestroyQueue();
            throw;
        }
    }

    private void DestroyQueue()
    {
        if (queue != IntPtr.Zero)
        {
            // An immediate disposal waits for a running callback and guarantees no further ones.
            _ = AudioQueueDispose(queue, true);
            queue = IntPtr.Zero;
        }

        if (queueHandle.IsAllocated)
        {
            queueHandle.Free();
        }
    }

    private unsafe void StartQueue(CoreAudioCaptureDataAvailableHandler handler, bool raiseStoppedOnFailure)
    {
        sink = handler;
        this.raiseStoppedOnFailure = raiseStoppedOnFailure;
        failure = null;
        stopping = false;
        runId++;

        try
        {
            foreach (IntPtr buffer in buffers)
            {
                ThrowIfQueueError(AudioQueueEnqueueBuffer(queue, (AudioQueueBuffer*)buffer, 0, IntPtr.Zero), nameof(AudioQueueEnqueueBuffer));
            }

            ThrowIfQueueError(AudioQueueStart(queue, IntPtr.Zero), nameof(AudioQueueStart));
            capturing = true;
        }
        catch
        {
            StopQueue();
            throw;
        }
    }

    // Returns once the queue has delivered what it had captured.
    private void StopQueue()
    {
        stopping = true;
        _ = AudioQueueStop(queue, true);
        capturing = false;
        sink = null;
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

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe void OnBufferFilled(IntPtr userData, IntPtr queue, AudioQueueBuffer* buffer, AudioTimeStamp* startTime, uint packetCount, IntPtr packetDescriptions)
    {
        if (GCHandle.FromIntPtr(userData).Target is not CoreAudioRecorder recorder || recorder.failure is not null)
        {
            return;
        }

        try
        {
            if (buffer->mAudioDataByteSize != 0 && recorder.sink is { } handler)
            {
                double firstByte = recorder.ToSeconds(*startTime);
                double current = firstByte + ((double)buffer->mAudioDataByteSize / recorder.queueFormat.mBytesPerFrame / recorder.queueFormat.mSampleRate);
                handler(new ReadOnlySpan<byte>(buffer->mAudioData, (int)buffer->mAudioDataByteSize), current, firstByte);
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
            _ = AudioQueueEnqueueBuffer(queue, buffer, 0, IntPtr.Zero);
        }
    }

    private void CompleteAfterFailure(int run)
    {
        Exception? exception;
        bool raise;
        lock (lockObject)
        {
            if (run != runId || !capturing || flags.HasFlag(CoreAudioRecorderStateFlags.Disposed)) { return; }
            raise = raiseStoppedOnFailure;
            exception = failure;
            StopQueue();
        }

        if (raise) { OnRecodingStopped(exception); }
    }
}
