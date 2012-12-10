using System;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NAudioWpfDemo.MediaFoundationPlayback
{
    internal class MediaFoundationPlaybackViewModel : ViewModelBase, IDisposable
    {
        private int requestFloatOutput;
        private string inputFile;
        private string defaultDecompressionFormat;
        private IWavePlayer wavePlayer;
        private WaveStream reader;
        public ICommand LoadCommand { get; private set; }
        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        private DispatcherTimer timer = new DispatcherTimer();
        private double sliderPosition;

        public MediaFoundationPlaybackViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play);
            PauseCommand = new DelegateCommand(Pause);
            StopCommand = new DelegateCommand(Stop);
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += TimerOnTick;
        }

        const double sliderMax = 10.0;

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (reader != null)
            {
                sliderPosition = Math.Min(sliderMax, reader.Position * sliderMax / reader.Length);
                OnPropertyChanged("SliderPosition");
            }
        }

        public double SliderPosition
        {
            get { return sliderPosition; }
            set
            {
                if (sliderPosition != value)
                {
                    sliderPosition = value;
                    if (reader != null)
                    {
                        var pos = (long)(reader.Length * sliderPosition/sliderMax);
                        reader.Position = pos; // media foundation will worry about block align for us
                    }
                    OnPropertyChanged("SliderPosition");
                }
            }
        }


        public int RequestFloatOutput
        {
            get { return requestFloatOutput; }
            set
            {
                if (requestFloatOutput != value)
                {
                    requestFloatOutput = value;
                    OnPropertyChanged("RequestFloatOutput");
                }
            }
        }

        private void SelectInputFile()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                if (TryOpenInputFile(ofd.FileName))
                {
                    TryOpenInputFile(ofd.FileName);
                }
            }
        }

        private bool TryOpenInputFile(string file)
        {
            bool isValid = false;
            try
            {
                using (var tempReader = new MediaFoundationReader(file))
                {
                    DefaultDecompressionFormat = tempReader.WaveFormat.ToString();
                    inputFile = file;
                    isValid = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Not a supported input file ({0})", e.Message));
            }
            return isValid;
        }

        public string DefaultDecompressionFormat
        {
            get { return defaultDecompressionFormat; }
            set
            {
                defaultDecompressionFormat = value;
                OnPropertyChanged("DefaultDecompressionFormat");
            }
        }

        private void Stop()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Stop();
            }
        }

        private void Pause()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Pause();
            }
        }

        private void Play()
        {
            if (inputFile == null)
                return;
            if (wavePlayer == null)
            {
                CreatePlayer();
            }
            if (reader == null)
            {
                reader = new MediaFoundationReader(inputFile);
                wavePlayer.Init(reader);
            }
            wavePlayer.Play();
            timer.Start();
        }

        private void CreatePlayer()
        {
            wavePlayer = new WaveOutEvent();
            wavePlayer.PlaybackStopped += WavePlayerOnPlaybackStopped;
        }

        private void WavePlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (reader != null)
            {
                SliderPosition = 0;
                //reader.Position = 0;
                timer.Stop();
            }
        }

        private void Load()
        {
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            SelectInputFile();
        }

        public void Dispose()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Dispose();
            }
            if (reader != null)
            {
                reader.Dispose();
            }
        }
    }

}