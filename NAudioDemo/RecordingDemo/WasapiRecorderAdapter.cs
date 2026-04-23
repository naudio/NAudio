using System;
using NAudio.CoreAudioApi;
using NAudio.Wasapi;
using NAudio.Wave;

namespace NAudioDemo.RecordingDemo
{
    /// <summary>
    /// Presents a <see cref="WasapiRecorder"/> as an <see cref="IWaveIn"/> so the demo's
    /// recording panel can treat all capture APIs uniformly. Each WASAPI packet is copied
    /// into a fresh managed byte[] before the event fires so consumers that defer handling
    /// (e.g. <c>Control.BeginInvoke</c>) read stable data.
    /// </summary>
    internal sealed class WasapiRecorderAdapter : IWaveIn
    {
        private readonly WasapiRecorder recorder;

        public WasapiRecorderAdapter(WasapiRecorder recorder)
        {
            this.recorder = recorder;
            recorder.DataAvailable += OnDataAvailable;
            recorder.RecordingStopped += OnRecordingStopped;
        }

        public WaveFormat WaveFormat
        {
            get => recorder.WaveFormat;
            set => throw new NotSupportedException(
                "Set the capture format via WasapiRecorderBuilder.WithFormat() before Build().");
        }

        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        public void StartRecording() => recorder.StartRecording();
        public void StopRecording() => recorder.StopRecording();

        private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags)
        {
            var handler = DataAvailable;
            if (handler == null) return;
            var copy = buffer.ToArray();
            handler(this, new WaveInEventArgs(copy, copy.Length));
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            RecordingStopped?.Invoke(this, e);
        }

        public void Dispose()
        {
            recorder.DataAvailable -= OnDataAvailable;
            recorder.RecordingStopped -= OnRecordingStopped;
            recorder.Dispose();
        }
    }
}
