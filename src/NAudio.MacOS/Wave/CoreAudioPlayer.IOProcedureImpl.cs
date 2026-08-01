
using System;
using System.Threading;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    private unsafe class ProcedureImpl : CoreAudioIOProcedure
    {
        private Exception exception;
        private IPlayerSource source;
        private bool shouldStopInNextCall;
        private AudioTimeStamp nowStamp, outTimeStamp;

        public ProcedureImpl(AudioDevice dev)
            : base(dev)
        {
            source = null;
            exception = null;
            shouldStopInNextCall = false;
            nowStamp = outTimeStamp = default;
        }

        protected override void IOProcedure(nint inNow, nint inInputData, nint inInputTime, nint outOutputData, nint inOutputTime)
        {
            if (shouldStopInNextCall)
            {
                StopCode();
                return;
            }
            AudioBuffer buffer;
            uint cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(outOutputData);
            try
            {
                for (uint I = 0; I < cBuffers; I++)
                {
                    buffer = AudioBufferList.GetAudioBufferFromPointer(outOutputData, I);
                    if (buffer.mData == IntPtr.Zero)
                    {
                        // Unused stream, move to the next one
                        continue;
                    }
                    int read;
                    Span<byte> allocatedSpan = buffer.GetSpan();
                    while (allocatedSpan.Length > 0)
                    {
                        read = source.Read(allocatedSpan);
                        if (read == 0) { shouldStopInNextCall = true; break; }
                        allocatedSpan = allocatedSpan.Slice(read);
                    }
                    if (shouldStopInNextCall) { break; }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                shouldStopInNextCall = true;
                return;
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

        private void StopCode()
        {
            try
            {
                Stop();
                InvokeStoppedEvent();
            }
            catch { }
        }

        // source is only settable, it's value is provided by the player implementation.
        public IPlayerSource Source
        {
            set => source = value;
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public double CurrentLatencyInSeconds()
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
                return (outTimeStamp.mSampleTime - nowStamp.mSampleTime) / source.ReinterpretedFormat.mSampleRate;
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
                // We can't compute it.
                return 0d;
            }
        }
    }
}