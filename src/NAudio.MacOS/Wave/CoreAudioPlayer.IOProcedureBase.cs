
using System;
using System.Threading;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioPlayer
{
    // Provides the base class for the player I/O procedure.
    // It provides the logic required for the player 
    // and provides a solid foundation to work on for peculiarities
    // of the HAL regarding how the audio data should be provided.
    private unsafe abstract class PlayerProcedure : CoreAudioIOProcedure
    {
        private Exception exception;
        private IPlayerSource source;
        private bool shouldStopInNextCall;
        private AudioTimeStamp nowStamp, outTimeStamp;

        public PlayerProcedure(AudioDevice dev)
            : base(dev)
        {
            source = null;
            exception = null;
            shouldStopInNextCall = false;
            nowStamp = outTimeStamp = default;
        }

        protected sealed override void IOProcedure(nint inNow, nint inInputData, nint inInputTime, nint outOutputData, nint inOutputTime)
        {
            if (shouldStopInNextCall)
            {
                StopCode();
                return;
            }
            uint cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(outOutputData);
            try
            {
                shouldStopInNextCall = ProvideData(cBuffers, outOutputData, source);
            }
            catch (Exception ex)
            {
                exception = ex;
                shouldStopInNextCall = true;
                return;
            }

            // The below pointers are always non-null; 
            // This happens because we initialize the procedure 
            // with at least one stream. inNow is always valid.
            // If we have enabled at least one out stream 
            // (which we do so), inOutputTime is always valid.
            nowStamp = *(AudioTimeStamp*)inNow.ToPointer();
            outTimeStamp = *(AudioTimeStamp*)inOutputTime.ToPointer();
        }

        // The actual implementation that reads from a given 
        // source provider and transfers it's data to the HAL. 
        // When the return value is true, it indicates that
        // the source has no more data to read from and the
        // procedure should stop in the next I/O call.
        // Why not stopping immediately once returning true?
        // The HAL calls the data provider periodically
        // typically by a number of milliseconds. The HAL 
        // has the right to begin the next I/O cycle 
        // immediately after this one so that it can have
        // data ready for consumption by the audio device.
        // As such, if we stopped immediately, we could
        // risk losing any data still pending to be
        // consumed by the audio device.
        // So, we wait for a full I/O cycle so that to be
        // ensured that all the provider's data
        // were provided to the device.
        protected abstract bool ProvideData(uint cBuffers, nint outOutputData, IPlayerSource source);

        // Make sure that the dispatch of the stopped event does not cause the HAL I/O
        // thread to be processor-overloaded.
        private void InvokeStoppedEvent()
        {
            // We can now remove the flag as the Stop call was executed.
            shouldStopInNextCall = false;
            ThreadPool.UnsafeQueueUserWorkItem(static state => state.EventInvoker(), this, false);
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
        public virtual IPlayerSource Source
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