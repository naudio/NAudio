
using System;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

internal sealed class CoreAudioCaptureOnlyIOProc : CoreAudioIOProcedure
{
    private Exception exception;
    private WaveFormat virtualFormat;
    private readonly bool usesLoopback;

    public CoreAudioCaptureOnlyIOProc(AudioDevice dev, WaveFormat captureFormat, bool useLoopback)
        : base(dev)
    {
        usesLoopback = useLoopback;
        virtualFormat = captureFormat;
    }

    protected unsafe override void IOProcedure(nint inNow, nint inInputData, nint inInputTime, nint outOutputData, nint inOutputTime)
    {
        AudioBuffer inputBuffer = default;
        uint cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(inInputData);
        for (uint I = 0U; I < cBuffers; I++)
        {
            inputBuffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
            if (inputBuffer.mData == IntPtr.Zero) { continue; }
            try
            {
                Event?.Invoke(
                    inputBuffer.GetSpan(),
                    MacUtils.TimeStampToSeconds(*(AudioTimeStamp*)inNow.ToPointer(), virtualFormat),
                    MacUtils.TimeStampToSeconds(*(AudioTimeStamp*)inInputTime.ToPointer(), virtualFormat)
                );
                break;
            }
            catch (Exception ex)
            {
                Stop();
                RecordingStopped?.Invoke(this, new(exception = ex));
                return;
            }
        }
        if (cBuffers != 0 && usesLoopback)
        {
            cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(outOutputData);
            for (uint I = 0U; I < cBuffers; I++)
            {
                var buffer = AudioBufferList.GetAudioBufferFromPointer(inInputData, I);
                if (buffer.mData == IntPtr.Zero) { continue; }
                Unsafe.CopyBlockUnaligned(
                    buffer.mData.ToPointer(),
                    inputBuffer.mData.ToPointer(),
                    // Just to be ensured that we won't cause a target buffer overrun.
                    Math.Min(inputBuffer.mDataByteSize, buffer.mDataByteSize)
                );
            }
        }
    }

    public Exception Exception
    {
        get
        {
            // Make sure that the exception object is nulled out, if the user manages to recover recording somehow.
            // This would cause an exception to be thrown if the recording was recovered and was successfully 
            // stopped.
            var e = exception;
            exception = null;
            return e;
        }
    }

    public WaveFormat VirtualFormat
    {
        get => virtualFormat;
        set => virtualFormat = value;
    }

    public event CoreAudioCaptureDataAvailableHandler Event;

    public event EventHandler<StoppedEventArgs> RecordingStopped;
}