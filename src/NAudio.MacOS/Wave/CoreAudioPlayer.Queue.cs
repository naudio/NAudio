using System;
using System.Threading;
using System.Runtime.InteropServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreFoundationApi;
using NAudio.MacOS.CoreAudioTypes;

using static NAudio.MacOS.AudioToolbox.Interop.NativeMethods;

namespace NAudio.Wave;

// Owns the Audio Queue. The buffer callback and the property listener run on threads of the
// queue and never take lockObject, because stopping or disposing the queue waits for them.
public partial class CoreAudioPlayer
{
    private static void ThrowIfQueueError(int osStatus, string function)
    {
        if (osStatus != 0)
        {
            throw new CoreAudioException($"{function} failed with OSStatus {osStatus}.", osStatus);
        }
    }

    private unsafe void CreateQueue(WaveFormat format)
    {
        AudioStreamBasicDescription description = MacUtils.ConstructASBDFromWaveFormat(format);
        int bufferBytes = GetBufferBytes(format);

        queueHandle = GCHandle.Alloc(this);

        IntPtr created;
        ThrowIfQueueError(
            AudioQueueNewOutput(&description, &OnBufferCompleted, GCHandle.ToIntPtr(queueHandle), IntPtr.Zero, IntPtr.Zero, 0, &created),
            nameof(AudioQueueNewOutput));
        queue = created;

        ThrowIfQueueError(
            AudioQueueAddPropertyListener(queue, kAudioQueueProperty_IsRunning, &OnIsRunningChanged, GCHandle.ToIntPtr(queueHandle)),
            nameof(AudioQueueAddPropertyListener));

        if (!followsDefaultDevice)
        {
            using CFString uid = CFString.CreateFromString(selectedDevice.DeviceUID);
            IntPtr uidRef = uid.NativeObject;
            ThrowIfQueueError(
                AudioQueueSetProperty(queue, kAudioQueueProperty_CurrentDevice, &uidRef, (uint)sizeof(IntPtr)),
                nameof(AudioQueueSetProperty));
        }

        if (format is WaveFormatExtensible extensible && extensible.ChannelMask != 0)
        {
            AudioChannelLayout layout = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)extensible.ChannelMask);
            ThrowIfQueueError(
                AudioQueueSetProperty(queue, kAudioQueueProperty_ChannelLayout, &layout, (uint)sizeof(AudioChannelLayout)),
                nameof(AudioQueueSetProperty));
        }

        buffers = new IntPtr[NumberOfBuffers];
        for (int i = 0; i < buffers.Length; i++)
        {
            AudioQueueBuffer* buffer;
            ThrowIfQueueError(AudioQueueAllocateBuffer(queue, (uint)bufferBytes, &buffer), nameof(AudioQueueAllocateBuffer));
            buffers[i] = (IntPtr)buffer;
        }
    }

    private int GetBufferBytes(WaveFormat format)
    {
        long frames = Math.Max(1L, format.SampleRate * (long)DesiredLatency / (1000L * NumberOfBuffers));
        return checked((int)(frames * format.BlockAlign));
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

    // Fills a buffer and hands it to the queue. Returns false when nothing was enqueued.
    private unsafe bool FillAndEnqueue(AudioQueueBuffer* buffer)
    {
        int read;
        try
        {
            var provider = originalProvider!;
            read = provider.Read(new Span<byte>(buffer->mAudioData, (int)buffer->mAudioDataBytesCapacity));
            read = Math.Clamp(read, 0, (int)buffer->mAudioDataBytesCapacity);
            read -= read % blockAlign;
        }
        catch (Exception ex)
        {
            failure = ex;
            sourceEnded = true;
            return false;
        }

        if (read == 0)
        {
            sourceEnded = true;
            return false;
        }

        buffer->mAudioDataByteSize = (uint)read;
        Interlocked.Add(ref submittedFrames, read / blockAlign);
        return AudioQueueEnqueueBuffer(queue, buffer, 0, IntPtr.Zero) == 0;
    }

    private void ScheduleCompletion()
    {
        _ = ThreadPool.UnsafeQueueUserWorkItem(static state => state.player.CompleteRun(state.run), (player: this, run: runId), false);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe void OnBufferCompleted(IntPtr userData, IntPtr queue, AudioQueueBuffer* buffer)
    {
        if (GCHandle.FromIntPtr(userData).Target is not CoreAudioPlayer player || player.stopping || player.sourceEnded)
        {
            return;
        }

        if (player.FillAndEnqueue(buffer) || !player.sourceEnded)
        {
            return;
        }

        if (player.failure is not null)
        {
            player.ScheduleCompletion();
        }
        else
        {
            // Lets the buffers that are still queued play out; the listener reports the end.
            player.draining = true;
            _ = AudioQueueStop(queue, false);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe void OnIsRunningChanged(IntPtr userData, IntPtr queue, uint propertyId)
    {
        if (GCHandle.FromIntPtr(userData).Target is not CoreAudioPlayer player || !player.draining)
        {
            return;
        }

        uint isRunning = 1;
        uint size = sizeof(uint);
        if (AudioQueueGetProperty(queue, kAudioQueueProperty_IsRunning, &isRunning, &size) == 0 && isRunning == 0)
        {
            player.ScheduleCompletion();
        }
    }

    private unsafe void UpdatePlayedFrames()
    {
        AudioTimeStamp stamp = default;
        if (queue != IntPtr.Zero &&
            AudioQueueGetCurrentTime(queue, IntPtr.Zero, &stamp, null) == 0 &&
            stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid))
        {
            long frames = (long)stamp.mSampleTime;
            playedFrames = Math.Clamp(frames, playedFrames, Interlocked.Read(ref submittedFrames));
        }
    }

    // Ends a run that finished by itself or failed, off the queue's threads.
    private void CompleteRun(int run)
    {
        Exception? exception;
        lock (lockObject)
        {
            if (run != runId || state == PlaybackState.Stopped || HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed))
            {
                return;
            }

            exception = failure;
            UpdatePlayedFrames();
            if (exception is null)
            {
                playedFrames = Interlocked.Read(ref submittedFrames);
            }

            StopQueue();
        }

        FirePlaybackStopped(new(exception));
    }

    private void StopQueue()
    {
        stopping = true;
        _ = AudioQueueStop(queue, true);
        state = PlaybackState.Stopped;
    }
}
