using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Sdl2;
using NAudio.Sdl2.Interop;
using NAudio.Sdl2.Structures;
using NAudio.Wave;
using NAudioAvaloniaDemo.Utils;
using NAudioAvaloniaDemo.ViewModel;

namespace NAudioAvaloniaDemo.WaveInSdlDemo
{
    internal class WaveInSdlViewModel : ViewModelBase, IDisposable
    {
        private List<WaveInSdlCapabilities> waveInSdlDevices;
        private WaveInSdlCapabilities selectedDevice;
        private int sampleRate;
        private int bitDepth;
        private int channelCount;
        private List<WaveFormatEncoding> sampleTypes;
        private WaveFormatEncoding waveFormatEncoding;
        private WaveInSdl waveInSdl;
        private WaveFileWriter writer;
        private string currentFileName;
        private string message;
        private float peak;
        public RecordingsViewModel RecordingsViewModel { get; }

        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }

        public WaveInSdlViewModel()
        {
            SampleTypes = new List<WaveFormatEncoding> { WaveFormatEncoding.IeeeFloat, WaveFormatEncoding.Pcm };
            WaveFormatEncoding = SampleTypes.FirstOrDefault();
            WaveInSdlDevices = WaveInSdl.GetCapabilitiesList();
            SelectedDevice = WaveInSdlDevices.FirstOrDefault();
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            RecordingsViewModel = new RecordingsViewModel();
        }

        private void Stop()
        {
            waveInSdl?.StopRecording();
        }

        private async void Record()
        {
            try
            {
                waveInSdl = new WaveInSdl();
                waveInSdl.WaveFormat = new WaveFormat(sampleRate, bitDepth, channelCount);
                currentFileName = String.Format("NAudioDemo {0:yyy-MM-dd HH-mm-ss}.wav", DateTime.Now);
                waveInSdl.StartRecording();
                waveInSdl.RecordingStopped += OnRecordingStopped;
                waveInSdl.DataAvailable += WaveInSdlOnDataAvailable;
                RecordCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                Message = "Recording...";
            }
            catch (Exception e)
            {
                await MessageBox.ShowAsync(e.Message);
            }
        }

        private void WaveInSdlOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            if (writer == null)
            {
                writer = new WaveFileWriter(Path.Combine(RecordingsViewModel.OutputFolder, 
                    currentFileName),
                    waveInSdl.WaveFormat);
            }

            writer.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            Peak = waveInSdl.PeakLevel;
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer.Dispose();
            writer = null;
            RecordingsViewModel.Recordings.Add(currentFileName);
            RecordingsViewModel.SelectedRecording = currentFileName;
            if (e.Exception == null)
                Message = "Recording Stopped";
            else
                Message = "Recording Error: " + e.Exception.Message;
            waveInSdl.Dispose();
            waveInSdl = null;
            RecordCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
        }

        public List<WaveInSdlCapabilities> WaveInSdlDevices
        {
            get => waveInSdlDevices;
            set => SetProperty(ref waveInSdlDevices, value);
        }

        public List<WaveFormatEncoding> SampleTypes
        {
            get => sampleTypes;
            set => SetProperty(ref sampleTypes, value);
        }

        public float Peak
        {
            get => peak;
            set => SetProperty(ref peak, value);
        }

        public WaveInSdlCapabilities SelectedDevice
        {
            get => selectedDevice;
            set
            {
                if (SetProperty(ref selectedDevice, value))
                    GetDefaultRecordingFormat(value);
            }
        }

        private void GetDefaultRecordingFormat(WaveInSdlCapabilities value)
        {
            WaveFormatEncoding = value.Format == SDL.AUDIO_F32SYS ? WaveFormatEncoding.IeeeFloat : WaveFormatEncoding.Pcm;
            SampleRate = value.Frequency;
            BitDepth = value.Bits;
            ChannelCount = value.Channels;
            Message = "";
        }

        public int SampleRate
        {
            get => sampleRate;
            set => SetProperty(ref sampleRate, value);
        }

        public int BitDepth
        {
            get => bitDepth;
            set => SetProperty(ref bitDepth, value);
        }

        public int ChannelCount
        {
            get => channelCount;
            set => SetProperty(ref channelCount, value);
        }

        public bool IsBitDepthConfigurable => WaveFormatEncoding == WaveFormatEncoding.Pcm;

        public WaveFormatEncoding WaveFormatEncoding
        {
            get => waveFormatEncoding;
            set
            {
                SetProperty(ref waveFormatEncoding, value);
                BitDepth = waveFormatEncoding == WaveFormatEncoding.Pcm ? 16 : 32;
                OnPropertyChanged(nameof(IsBitDepthConfigurable));
            }
        }

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}