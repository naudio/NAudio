using System;
using System.Windows;
using Avalonia.Threading;
using NAudio.Extras;
using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;
using NAudioAvaloniaDemo.Utils;

namespace NAudioAvaloniaDemo.AudioPlaybackDemo
{
    class AudioPlayback : IDisposable
    {
        private IWavePlayer playbackDevice;
        private WaveStream fileStream;

        public event EventHandler<FftEventArgs> FftCalculated;

        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;

        public void Load(string fileName)
        {
            Stop();
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(fileName);
        }

        private void CloseFile()
        {
            fileStream?.Dispose();
            fileStream = null;
        }

        private async void OpenFile(string fileName)
        {
            try
            {
                var inputStream = new AudioFileReader(fileName);
                fileStream = inputStream;
                var aggregator = new SampleAggregator(inputStream);
                aggregator.NotificationCount = inputStream.WaveFormat.SampleRate / 100;
                aggregator.PerformFFT = true;
                aggregator.FftCalculated += (s, a) => FftCalculated?.Invoke(this, a);
                aggregator.MaximumCalculated += (s, a) => MaximumCalculated?.Invoke(this, a); 
                playbackDevice.Init(aggregator, convertTo16Bit: true);
            }
            catch (Exception e)
            {
                await MessageBox.ShowAsync(e.Message, "Problem opening file");
                CloseFile();
            }
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
            playbackDevice = new WaveOutSdl
            {
                DesiredLatency = 200, 
                AudioConversion = AudioConversion.None
            };
        }

        public void Play()
        {
            if (playbackDevice != null && fileStream != null && playbackDevice.PlaybackState != PlaybackState.Playing)
            {
                playbackDevice.Play();
            }
        }

        public void Pause()
        {
            playbackDevice?.Pause();
        }

        public void Stop()
        {
            playbackDevice?.Stop();
            if (fileStream != null)
            {
                fileStream.Position = 0;
            }
        }

        public void Dispose()
        {
            Stop();
            CloseFile();
            playbackDevice?.Dispose();
            playbackDevice = null;
        }
    }
}
