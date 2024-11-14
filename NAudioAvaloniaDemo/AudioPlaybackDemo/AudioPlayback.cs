using System;
using System.IO;
using Avalonia.Platform.Storage;
using NAudio.Extras;
using NAudio.Sdl2;
using NAudio.Wave;
using NAudioAvaloniaDemo.Utils;

namespace NAudioAvaloniaDemo.AudioPlaybackDemo
{
    class AudioPlayback : IDisposable
    {
        private IWavePlayer playbackDevice;
        private WaveStream fileStream;
        private Stream storageFileStream;

        public event EventHandler<FftEventArgs> FftCalculated;

        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;

        public void Load(IStorageFile storageFile)
        {
            Stop();
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(storageFile);
        }

        private void CloseFile()
        {
            fileStream?.Dispose();
            fileStream = null;
            storageFileStream?.Dispose();
            storageFileStream = null;
        }

        private async void OpenFile(IStorageFile storageFile)
        {
            try
            {
                var fileName = storageFile.Name;
                storageFileStream = await storageFile.OpenReadAsync();
                var inputStream = new AudioFileStreamReader(fileName, storageFileStream);
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
                await MessageBox.ShowAsync(e.Message);
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
                DesiredLatency = 100
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
            storageFileStream?.Dispose();
            storageFileStream = null;
        }
    }
}
