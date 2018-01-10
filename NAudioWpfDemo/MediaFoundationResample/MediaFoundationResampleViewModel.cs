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
            RepositionTestCommand = new DelegateCommand(RepositionTest);
            SampleRates = new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000 };
            BitDepths = new[] {"Unchanged", "8", "16", "24", "IEEE float"};
            ChannelCounts = new[] { "Unchanged", "mono", "stereo" };
            selectedSampleRate = 16000;
        }

        public ICommand SelectInputFileCommand { get; }
        public ICommand ResampleCommand { get; }
        public ICommand RepositionTestCommand { get; }
        public int[] SampleRates { get; }
        public string[] BitDepths { get; }
        public string[] ChannelCounts { get; }

        public string InputFile
        {
            get => inputFile;
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
            get => inputFileFormat;
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
            get => selectedSampleRate;
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
            get => selectedBitDepthIndex;
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
            get => selectedChannelCountIndex;
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

        private string SelectSaveFile(string desc)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = String.Format("{0} {1} {2}kHz.wav",
                                         Path.GetFileNameWithoutExtension(InputFile),
                                         desc,
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
            var saveFile = SelectSaveFile("resampled");
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

        private void RepositionTest()
        {
            if (String.IsNullOrEmpty(InputFile))
            {
                MessageBox.Show("Select a file first");
                return;
            }
            var saveFile = SelectSaveFile("reposition");
            if (saveFile == null)
            {
                return;
            }
            // do the resample
            using (var reader = new MediaFoundationReader(InputFile))
            using (var resampler = new MediaFoundationResampler(reader, CreateOutputFormat(reader.WaveFormat)))
            {
                CreateRepositionTestFile(saveFile, resampler, () =>
                                                        {
                                                            // tell the reader to go back to the start (we're trusting it not to have leftovers)
                                                            reader.Position = 0;
                                                            // tell the resampler that we have repositioned and it should drain all its buffers
                                                            resampler.Reposition();
                                                        });
            }

            // use the following to test that just the reader is doing clean repositions:
            /*
            using (var reader = new MediaFoundationReader(InputFile))
            {
                CreateRepositionTestFile(saveFile, reader, () =>
                                                        {
                                                            // tell the reader to go back to the start (we're trusting it not to have leftovers)
                                                            reader.Position = 0;
                                                        });
            }*/

            MessageBox.Show("Resample complete");
        }

        private void CreateRepositionTestFile(string saveFile, IWaveProvider source, Action reposition)
        {
            using (var writer = new WaveFileWriter(saveFile, source.WaveFormat))
            {
                // half-second buffer
                var buffer = new byte[writer.WaveFormat.AverageBytesPerSecond / 2];
                // read three and a half seconds (half a second is to ensure Resampler has some leftovers to drain)
                for (int n = 0; n < 7; n++)
                {
                    var read = source.Read(buffer, 0, buffer.Length);
                    writer.Write(buffer, 0, read);
                }
                Array.Clear(buffer, 0, buffer.Length);
                // two seconds of absolute silence
                for (int n = 0; n < 4; n++)
                {
                    writer.Write(buffer, 0, buffer.Length);
                }
                // do the reposition
                reposition();
                // now read some more out
                for (int n = 0; n < 6; n++)
                {
                    var read = source.Read(buffer, 0, buffer.Length);
                    writer.Write(buffer, 0, read);
                }
            }
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