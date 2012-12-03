using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.MediaFoundationResample
{
    internal class MediaFoundationResampleViewModel : ViewModelBase
    {
        private string inputFile;
        private int selectedSampleRate;
        private int selectedBitDepthIndex;
        private int selectedChannelCountIndex;
        private string inputFileFormat;

        public MediaFoundationResampleViewModel()
        {
            SelectInputFileCommand = new DelegateCommand(SelectInputFile);
            ResampleCommand = new DelegateCommand(Resample);
            SampleRates = new int[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000 };
            BitDepths = new string[] {"Unchanged", "8", "16", "24", "IEEE float"};
            ChannelCounts = new string[] { "Unchanged", "mono", "stereo" };
            selectedSampleRate = 16000;
        }

        public ICommand SelectInputFileCommand { get; private set; }
        public ICommand ResampleCommand { get; private set; }
        public int[] SampleRates { get; private set; }
        public string[] BitDepths { get; private set; }
        public string[] ChannelCounts { get; private set; }

        public string InputFile
        {
            get { return inputFile; }
            set
            {
                if (inputFile != value)
                {
                    inputFile = value;
                    OnPropertyChanged("InputFile");
                }
            }
        }

        public string InputFileFormat
        {
            get { return inputFileFormat; }
            set
            {
                if (inputFileFormat != value)
                {
                    inputFileFormat = value;
                    OnPropertyChanged("InputFileFormat");
                }
            }
        }

        public int SampleRate
        {
            get { return selectedSampleRate; }
            set
            {
                if (selectedSampleRate != value)
                {
                    selectedSampleRate = value;
                    OnPropertyChanged("SampleRate");
                }
            }
        }

        public int SelectedBitDepthIndex
        {
            get { return selectedBitDepthIndex; }
            set
            {
                if (selectedBitDepthIndex != value)
                {
                    selectedBitDepthIndex = value;
                    OnPropertyChanged("SelectedBitDepthIndex");
                }
            }
        }

        public int SelectedChannelCountIndex
        {
            get { return selectedChannelCountIndex; }
            set
            {
                if (selectedChannelCountIndex != value)
                {
                    selectedChannelCountIndex = value;
                    OnPropertyChanged("SelectedChannelCountIndex");
                }
            }
        }

        private void SelectInputFile()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Audio files|*.mp3;*.wav;*.wma;*.aiff;*.aac";
            if (ofd.ShowDialog() == true)
            {
                if (TryOpenInputFile(ofd.FileName))
                {
                    InputFile = ofd.FileName;
                }
            }
        }

        private bool TryOpenInputFile(string file)
        {
            bool isValid = false;
            try
            {
                using (var reader = new MediaFoundationReader(file))
                {
                    InputFileFormat = reader.WaveFormat.ToString();
                    isValid = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Not a supported input file ({0})", e.Message));
            }
            return isValid;
        }

        private string SelectSaveFile()
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = String.Format("{0} resampled {1}kHz.wav",
                                         Path.GetFileNameWithoutExtension(InputFile),
                                         SampleRate/1000M);
            sfd.Filter = "WAV File|*.wav";
            //return (sfd.ShowDialog() == true) ? new Uri(sfd.FileName).AbsoluteUri : null;
            return (sfd.ShowDialog() == true) ? sfd.FileName : null;
        }

        private void Resample()
        {
            if (String.IsNullOrEmpty(InputFile))
            {
                MessageBox.Show("Select a file first");
                return;
            }
            var saveFile = SelectSaveFile();
            if (saveFile == null)
            {
                return;
            }

            // do the resample
            using (var reader = new MediaFoundationReader(InputFile))
            using (var resampler = new MediaFoundationResampler(reader, CreateOutputFormat(reader.WaveFormat)))
            {
                WaveFileWriter.CreateWaveFile(saveFile, resampler);
            }
            MessageBox.Show("Resample complete");
        }

        private WaveFormat CreateOutputFormat(WaveFormat inputFormat)
        {
            bool isIeeeFloat = inputFormat.Encoding == WaveFormatEncoding.IeeeFloat && SelectedBitDepthIndex == 0 ||
                              SelectedBitDepthIndex == 4;
            int channels = SelectedChannelCountIndex == 0 ? inputFormat.Channels : selectedChannelCountIndex;
            WaveFormat waveFormat;
            if (isIeeeFloat)
            {
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, channels);
            }
            else
            {
                int bitDepth = inputFormat.BitsPerSample;
                switch (SelectedBitDepthIndex)
                {
                    case 1: 
                        bitDepth = 8;
                        break;
                    case 2:
                        bitDepth = 16;
                        break;
                    case 3:
                        bitDepth = 24;
                        break;
                }
                waveFormat = new WaveFormat(SampleRate, bitDepth, channels);
            }
            return waveFormat;
        }

    }
}