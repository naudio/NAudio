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
        private int inputSampleRate;

        public MediaFoundationResampleViewModel()
        {
            SelectInputFileCommand = new DelegateCommand(SelectInputFile);
            ResampleCommand = new DelegateCommand(Resample);
            SampleRates = new int[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000 };
            selectedSampleRate = 16000;
        }

        public ICommand SelectInputFileCommand { get; private set; }
        public ICommand ResampleCommand { get; private set; }
        public int[] SampleRates { get; private set; }

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

        public int InputSampleRate
        {
            get { return inputSampleRate; }
            set
            {
                if (inputSampleRate != value)
                {
                    inputSampleRate = value;
                    OnPropertyChanged("InputSampleRate");
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
                    InputSampleRate = reader.WaveFormat.SampleRate;
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
            using(var reader = new MediaFoundationReader(InputFile))
            using (var resampler = new MediaFoundationResampler(reader, SampleRate))
            {
                WaveFileWriter.CreateWaveFile(saveFile, resampler);
            }
            MessageBox.Show("Resample complete");
        }
    }
}