
using System;
using System.Threading;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

// Provide support code for the I/O procedure 
// that is used by the CoreAudioPlayer class.
internal sealed class CoreAudioOutOnlyIOProc : CoreAudioIOProcedure
{
    private Exception exception;
    private IWaveProvider provider;
    private bool shouldStopInNextCall;
    private AudioTimeStamp nowStamp, outTimeStamp;

    public CoreAudioOutOnlyIOProc(AudioDevice dev)
        : base(dev)
    {
        provider = null;
        exception = null;
        nowStamp = default;
        outTimeStamp = default;
        shouldStopInNextCall = false;
    }

    protected unsafe override void IOProcedure(nint inNow, nint inInputData, nint inInputTime, nint outOutputData, nint inOutputTime)
    {
        if (shouldStopInNextCall)
        {
            // This allows us to stop the playback, once the provided audio provider has been entirely consumed
            Stop();
            InvokeStoppedEvent();
            return;
        }
        int readBytes = 0;
        AudioBuffer b;
        uint cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(outOutputData);
        // Note: The below loop assumes that we will enable only one stream.
        // If we needed multiple ones, we would run through all the enabled buffers,
        // compute the max size, allocate a backing buffer, read from the provider,
        // read data from the provider to the backing buffer, and then perform the actual 
        // copy to all the enabled stream buffers, but that is very expensive and for the
        // usage we need it this is sufficient as we will only enable one output 
        // stream per a CoreAudioPlayer instance.
        // If consumers ever need such capability, they can create their own wrapper to a
        // custom I/O procedure that allows that. We prefer to not pay this extra cost because 
        // we are on the audio thread and need to be extremely careful, and return
        // as fast as possible, otherwise we risk losing playback control, and the processor
        // overloaded event will probably dispatch if we significantly delay here. 
        // We do already pay the cost of interop translation which is time-consuming, 
        // so try not to pay extra time and risk having processor overloading.
        for (uint I = 0U; I < cBuffers; I++)
        {
            b = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
            if (b.mData == IntPtr.Zero)
            {
                // Stream is disabled, skip it.
                continue;
            }
            var span = b.GetSpan();
            while (span.Length > 0)
            {
                try
                {
                    readBytes = provider.Read(span);
                }
                catch (Exception ex)
                {
                    // Process whatever we can process, then fire the stop event.
                    shouldStopInNextCall = true;
                    exception = ex;
                }
                if (readBytes == 0)
                {
                    // Make sure that the HAL plays all the data remaining into it's buffers before stopping it.
                    // So, first assign the flag, and in the next time, stop it.
                    shouldStopInNextCall = true;
                    return;
                }
                else if (readBytes == span.Length)
                {
                    // we read all the bytes at once, break the loop
                    break;
                }
                else
                {
                    span = span.Slice(readBytes);
                }
            }
        }
        nowStamp = *(AudioTimeStamp*)inNow.ToPointer();
        outTimeStamp = *(AudioTimeStamp*)inOutputTime.ToPointer();
    }

    // Make sure that the dispatch of the stopped event does not cause the HAL I/O
    // thread to be processor-overloaded.
    private void InvokeStoppedEvent()
    {
        // We can now remove the flag as the Stop call was executed.
        shouldStopInNextCall = false;
        new Thread(new ThreadStart(EventInvoker)).Start();
    }

    private void EventInvoker() => PlaybackStopped?.Invoke(null, new(exception));

    public IWaveProvider Provider
    {
        get => provider;
        set => provider = value;
    }

    public double CurrentLatencyInSeconds(WaveFormat fmt)
    {
        if (
            nowStamp.mFlags == AudioTimeStampFlags.kAudioTimeStampNothingValid ||
            outTimeStamp.mFlags == AudioTimeStampFlags.kAudioTimeStampNothingValid
        )
        {
            // We will return -1 here. The actual call in CurrentLatency will handle this special case.
            return -1d;
        }
        else if (
            nowStamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid) &&
            outTimeStamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid)
        )
        {
            return (outTimeStamp.mSampleTime - nowStamp.mSampleTime) / fmt.SampleRate;
        }
        else if (
            nowStamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid) &&
            outTimeStamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid)
        )
        {
            return CoreAudioFunctions.ConvertHostTimeToSeconds(outTimeStamp.mHostTime - nowStamp.mHostTime);
        }
        else
        {
            return MacUtils.TimeStampToSeconds(outTimeStamp, fmt) -
                    MacUtils.TimeStampToSeconds(nowStamp, fmt);
        }
    }

    public event EventHandler<StoppedEventArgs> PlaybackStopped;
}