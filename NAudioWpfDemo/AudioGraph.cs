using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Audio Graph
    /// </summary>
    class AudioGraph : IDisposable
    {
        private AudioCapture capture;
        private AudioPlayback playback;
        private SampleAggregator aggregator;

        public event EventHandler CaptureComplete
        {
            add { capture.CaptureStopped += value; }
            remove { capture.CaptureStopped -= value; }
        }

        public event EventHandler<MaxSampleEventArgs> MaximumCalculated
        {
            add { aggregator.MaximumCalculated += value; }
            remove { aggregator.MaximumCalculated -= value; }
        }

        public event EventHandler<FftEventArgs> FftCalculated
        {
            add { aggregator.FftCalculated += value; }
            remove { aggregator.FftCalculated -= value; }
        }

        public AudioGraph()
        {
            playback = new AudioPlayback();
            playback.OnSample += OnSample;
            capture = new AudioCapture();
            capture.OnSample += OnSample;
            aggregator = new SampleAggregator();
            aggregator.NotificationCount = 100;
            aggregator.PerformFFT = true;
        }

        void OnSample(object sender, SampleEventArgs e)
        {
            aggregator.Add(e.Left);
        }

        public int NotificationsPerSecond
        {
            get { return aggregator.NotificationCount; }
            set { aggregator.NotificationCount = value; }
        }

        public double RecordVolume
        {
            get { return capture.RecordVolume; }
            set { capture.RecordVolume = value; }
        }

        public bool HasCapturedAudio { get; private set; }

        public void PlayFile(string fileName)
        {
            CancelCurrentOperation();
            playback.Load(fileName);
            aggregator.NotificationCount = 882;
            playback.Play();
        }

        private void CancelCurrentOperation()
        {
            playback.Stop();
            capture.Stop();
        }

        public void Stop()
        {
            CancelCurrentOperation();
        }

        public void SaveRecordedAudio(string fileName)
        {
            throw new NotImplementedException();
        }

        public void PlayCapturedAudio()
        {
            throw new NotImplementedException();
        }

        public void StartCapture(int captureSeconds)
        {
            aggregator.NotificationCount = 200;
            capture.CaptureSeconds = captureSeconds;
            capture.Capture(new WaveFormat(8000, 1));
        }

        public void Dispose()
        {
            if (capture != null)
            {
                capture.Dispose();
                capture = null;
            }
            if (playback != null)
            {
                playback.Dispose();
                playback = null;
            }
        }
    }
}
