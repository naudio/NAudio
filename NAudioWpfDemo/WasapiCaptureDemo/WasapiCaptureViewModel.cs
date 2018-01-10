using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.WasapiCaptureDemo
{
    internal class WasapiCaptureViewModel : ViewModelBase, IDisposable
    {
        private MMDevice selectedDevice;
        private int sampleRate;
        private int bitDepth;
        private int channelCount;
        private int sampleTypeIndex;
        private WasapiCapture capture;
        private WaveFileWriter writer;
        private string currentFileName;
        private string message;
        private float peak;
        private readonly SynchronizationContext synchronizationContext;

        private float recordLevel;

        public RecordingsViewModel RecordingsViewModel { get; }

        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopCommand { get; }

        private int shareModeIndex;

        public WasapiCaptureViewModel()
        {
            synchronizationContext = SynchronizationContext.Current;
            var enumerator = new MMDeviceEnumerator();
            CaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            SelectedDevice = CaptureDevices.FirstOrDefault(c => c.ID == defaultDevice.ID);
            RecordCommand = new DelegateCommand(Record);
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            RecordingsViewModel = new RecordingsViewModel();
        }

        private void Stop()
        {
            capture?.StopRecording();
        }

        private void Record()
        {
            try
            {
                capture = new WasapiCapture(SelectedDevice);
                capture.ShareMode = ShareModeIndex == 0 ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
                capture.WaveFormat =
                    SampleTypeIndex == 0 ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount) :
                    new WaveFormat(sampleRate, bitDepth, channelCount);
                currentFileName = String.Format("NAudioDemo {0:yyy-MM-dd HH-mm-ss}.wav", DateTime.Now);
                RecordLevel = SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                capture.StartRecording();
                capture.RecordingStopped += OnRecordingStopped;
                capture.DataAvailable += CaptureOnDataAvailable;
                RecordCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                Message = "Recording...";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            if (writer == null)
            {
                writer = new WaveFileWriter(Path.Combine(RecordingsViewModel.OutputFolder, 
                    currentFileName),
                    capture.WaveFormat);
            }

            writer.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

            UpdatePeakMeter();
        }

        void UpdatePeakMeter()
        {
            // can't access this on a different thread from the one it was created on, so get back to GUI thread
            synchronizationContext.Post(s => Peak = SelectedDevice.AudioMeterInformation
                .MasterPeakValue, null);
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
            capture.Dispose();
            capture = null;
            RecordCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
        }

        public IEnumerable<MMDevice> CaptureDevices { get; }

        public float Peak
        {
            get => peak;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (peak != value)
                {
                    peak = value;
                    OnPropertyChanged("Peak");
                }
            }
        }

        public MMDevice SelectedDevice
        {
            get => selectedDevice;
            set
            {
                if (selectedDevice != value)
                {
                    selectedDevice = value;
                    OnPropertyChanged("SelectedDevice");
                    GetDefaultRecordingFormat(value);
                }
            }
        }

        private void GetDefaultRecordingFormat(MMDevice value)
        {
            using (var c = new WasapiCapture(value))
            {
                SampleTypeIndex = c.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat ? 0 : 1;
                SampleRate = c.WaveFormat.SampleRate;
                BitDepth = c.WaveFormat.BitsPerSample;
                ChannelCount = c.WaveFormat.Channels;
                Message = "";
            }
        }

        public int SampleRate
        {
            get => sampleRate;
            set
            {
                if (sampleRate != value)
                {
                    sampleRate = value;
                    OnPropertyChanged("SampleRate");
                }
            }
        }

        public int BitDepth
        {
            get => bitDepth;
            set
            {
                if (bitDepth != value)
                {
                    bitDepth = value;
                    OnPropertyChanged("BitDepth");
                }
            }
        }

        public int ChannelCount
        {
            get => channelCount;
            set
            {
                if (channelCount != value)
                {
                    channelCount = value;
                    OnPropertyChanged("ChannelCount");
                }
            }
        }

        public bool IsBitDepthConfigurable => SampleTypeIndex == 1;

        public int SampleTypeIndex
        {
            get => sampleTypeIndex;
            set
            {
                if (sampleTypeIndex != value)
                {
                    sampleTypeIndex = value;
                    OnPropertyChanged("SampleTypeIndex");
                    BitDepth = sampleTypeIndex == 1 ? 16 : 32;
                    OnPropertyChanged("IsBitDepthConfigurable");
                }
            }
        }

        public string Message
        {
            get => message;
            set
            {
                if (message != value)
                {
                    message = value;
                    OnPropertyChanged("Message");
                }
            }
        }

        public int ShareModeIndex
        {
            get => shareModeIndex;
            set
            {
                if (shareModeIndex != value)
                {
                    shareModeIndex = value;
                    OnPropertyChanged("ShareModeIndex");
                }
            }
        }


        public void Dispose()
        {
            Stop();
        }

        public float RecordLevel
        {
            get => recordLevel;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (recordLevel != value)
                {
                    recordLevel = value;
                    if (capture != null)
                    {
                        SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                    }
                    OnPropertyChanged("RecordLevel");
                }
            }
        }
    }
}
