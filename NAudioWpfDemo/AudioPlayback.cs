using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioWpfDemo
{
    class AudioPlayback : IDisposable
    {
        private IWavePlayer playbackDevice;
        private WaveStream fileStream;
        
        public event EventHandler<SampleEventArgs> OnSample;
        
        public AudioPlayback()
        {
        }

        public void Load(string fileName)
        {
            Stop();            
            EnsureDeviceCreated();
            CloseFile();
            OpenFile(fileName);
        }

        private void CloseFile()
        {
            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }
        }

        private void OpenFile(string fileName)
        {
            var inputStream = CreateInputStream(fileName);
            playbackDevice.Init(new SampleToWaveProvider(inputStream));
        }

        private ISampleProvider CreateInputStream(string fileName)
        {

            if (fileName.EndsWith(".wav"))
            {
                fileStream = OpenWavStream(fileName);
            }
            else if (fileName.EndsWith(".mp3"))
            {
                fileStream = new Mp3FileReader(fileName);
            }
            else
            {
                throw new InvalidOperationException("Unsupported extension");
            }
            var inputStream = new SampleChannel(fileStream);
            var sampleStream = new NotifyingSampleProvider(inputStream);
            sampleStream.Sample += new EventHandler<SampleEventArgs>(inputStream_Sample);
            return sampleStream;
        }

        void inputStream_Sample(object sender, SampleEventArgs e)
        {
            if (OnSample != null)
            {
                OnSample(this, e);
            }
        }

        private static WaveStream OpenWavStream(string fileName)
        {
            WaveStream readerStream = new WaveFileReader(fileName);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }
            return readerStream;
        }

        private void EnsureDeviceCreated()
        {
            if (playbackDevice == null)
            {
                CreateDevice();
            }
        }

        private void CreateDevice()
        {
            playbackDevice = new WaveOut();
        }

        public void Play()
        {
            if (playbackDevice != null && fileStream != null && playbackDevice.PlaybackState != PlaybackState.Playing)
            {
                playbackDevice.Play();
            }
        }

        public void Stop()
        {
            if (playbackDevice != null)
            {
                playbackDevice.Stop();
            }
            CloseFile();
        }

        public void Dispose()
        {
            Stop();
            if (playbackDevice != null)
            {
                playbackDevice.Dispose();
                playbackDevice = null;
            }
        }
    }
}
