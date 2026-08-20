
using System;
using System.Threading;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

public partial class CoreAudioRecorder
{
    // Provides the base class for the recorder I/O procedure.
    // It provides the logic required for the recorder 
    // and provides a solid foundation to work on for peculiarities
    // of the HAL regarding how the audio data should be retrieved.
    private abstract class RecorderProcedure : CoreAudioIOProcedure
    {
        private Exception exception;
        private uint streamsLatency;
        private AudioStreamBasicDescription virtualFormat;
        private AudioTimeStamp inNowStamp, inInputTimeStamp;

        protected RecorderProcedure(AudioDevice dev) : base(dev) { }

        protected sealed override unsafe void IOProcedure(nint inNow, nint inInputData, nint inInputTime, nint outOutputData, nint inOutputTime)
        {
            uint cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(inInputData);
            try
            {
                inNowStamp = *(AudioTimeStamp*)inNow.ToPointer();
                inInputTimeStamp = *(AudioTimeStamp*)inInputTime.ToPointer();
                OnProvideData(cBuffers, inInputData);
            }
            catch (Exception ex)
            {
                TryStopRecording(ex);
                return;
            }
        }

        // The method that the special I/O procedures do implement.
        // This implementation must call the OnDataAvailable method at some time,
        // providing the audio data in an interleaved audio format.
        protected abstract void OnProvideData(uint bufferCount, IntPtr inInputData);

        private void TryStopRecording(Exception ex)
        {
            // Make sure to store any found exception (and remove any previous one)
            exception = ex;
            try
            {
                // Attempt to stop the recording.
                Stop();
                // Try putting the playback stopped event into the .NET thread pool to avoid thread allocations.
                _ = ThreadPool.UnsafeQueueUserWorkItem(static state => state.InvokeStoppedEvent(), this, false);
            }
            catch { }
        }

        protected void OnDataAvailable(ReadOnlySpan<byte> buffer)
        {
            Event?.Invoke(
                buffer,
                ConstructStampTime(inNowStamp),
                ConstructStampTime(inInputTimeStamp)
            );
        }

        private double ConstructStampTime(AudioTimeStamp stamp)
        {
            if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid))
            {
                return stamp.mSampleTime / virtualFormat.mSampleRate;
            }
            else if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid))
            {
                return CoreAudioFunctions.ConvertHostTimeToSeconds(stamp.mHostTime);
            }
            else
            {
                return 0d;
            }
        }

        public double GetCurrentLatency()
        {
            if (
                inNowStamp.mFlags == AudioTimeStampFlags.kAudioTimeStampNothingValid &&
                inInputTimeStamp.mFlags == AudioTimeStampFlags.kAudioTimeStampNothingValid
            )
            {
                return -1d;
            }
            else
            {
                return ConstructStampTime(inNowStamp) - ConstructStampTime(inInputTimeStamp);
            }
        }

        private void InvokeStoppedEvent() => RecordingStopped?.Invoke(this, new(exception));

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

        public uint StreamLatency
        {
            get => streamsLatency;
            set => streamsLatency = value;
        }

        public virtual AudioStreamBasicDescription VirtualFormat
        {
            get => virtualFormat;
            set => virtualFormat = value;
        }

        public event CoreAudioCaptureDataAvailableHandler Event;

        public event EventHandler<StoppedEventArgs> RecordingStopped;
    }
}