using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.IO;
using NAudio.Utils;

namespace NAudioWpfDemo
{
    class AudioCapture : IDisposable
    {
        private IWaveIn captureDevice;
        private MemoryStream recordedStream;
        private WaveFileWriter writer;
        private int maxCaptureBytes;

        public AudioCapture()
        {
            CaptureSeconds = 30;
        }

        public bool IsCapturing { get; set; }
        public int CaptureSeconds { get; set; }

        public event EventHandler<SampleEventArgs> OnSample;
        public event EventHandler CaptureComplete;

        public void Capture(WaveFormat captureFormat)
        {
            if (IsCapturing)
            {
                throw new InvalidOperationException("Already Recording");
            }

            CreateCaptureStream(captureFormat);
            StartCapture(captureFormat);
        }

        private void StartCapture(WaveFormat captureFormat)
        {
            EnsureDeviceIsCreated();
            captureDevice.WaveFormat = captureFormat;
            captureDevice.StartRecording();
            IsCapturing = true;
        }

        private void CreateCaptureStream(WaveFormat captureFormat)
        {
            int maxSeconds = CaptureSeconds == 0 ? 30 : CaptureSeconds;
            int captureBytes = maxSeconds * captureFormat.AverageBytesPerSecond;
            this.maxCaptureBytes = CaptureSeconds == 0 ? 0 : captureBytes;
            recordedStream = new MemoryStream(captureBytes + 50);
            writer = new WaveFileWriter(new IgnoreDisposeStream(recordedStream), captureFormat);
        }

        private void EnsureDeviceIsCreated()
        {
            if (captureDevice == null)
            {
                captureDevice = new WaveIn();
                captureDevice.RecordingStopped += OnRecordingStopped;
                captureDevice.DataAvailable += OnDataAvailable;
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!IsCapturing)
            {
                return;
            }
            // first save the audio
            byte[] buffer = e.Buffer;
            writer.Write(buffer, 0, e.BytesRecorded);

            // now report each sample if necessary
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) | buffer[index + 0]);
                /* short sample2 = BitConverter.ToInt16(buffer, index);
                Debug.Assert(sample == sample2, "Oops"); */
                float sample32 = sample / 32768f;
                if (OnSample != null)
                {
                    OnSample(this, new SampleEventArgs(sample32, 0));
                }
            }

            // stop the recording if necessary
            if (maxCaptureBytes != 0 && recordedStream.Length >= maxCaptureBytes)
            {
                Stop();
            }
        }

        public void CloseRecording()
        {
            if (captureDevice != null)
            {
                captureDevice.StopRecording();
            }

            if (writer != null)
            {
                // this will fix up the data lengths in the recorded memory stream
                writer.Close();
                writer = null;
                recordedStream.Position = 0;
                RaiseCaptureStopped();
            }
        }

        void OnRecordingStopped(object sender, EventArgs e)
        {
            IsCapturing = false;
            CloseRecording();
            captureDevice.Dispose();
            captureDevice = null;
        }

        public void Stop()
        {
            if (captureDevice != null)
            {
                captureDevice.StopRecording();
            }
        }

        private void RaiseCaptureStopped()
        {
            if (CaptureComplete != null)
            {
                CaptureComplete(this, EventArgs.Empty);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (captureDevice != null)
            {
                captureDevice.Dispose();
                captureDevice = null;
            }
        }

        #endregion
    }
}
