using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private string inputPath;
        private string defaultDecompressionFormat;
        private IWavePlayer wavePlayer;
        private WaveStream reader;
        public RelayCommand LoadCommand { get; private set; }
        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        private DispatcherTimer timer = new DispatcherTimer();
        private double sliderPosition;
        private ObservableCollection<string> inputPathHistory;
        private string lastPlayed;

        public MediaFoundationPlaybackViewModel()
        {
            inputPathHistory = new ObservableCollection<string>();
            LoadCommand = new RelayCommand(Load, () => IsStopped);
            PlayCommand = new RelayCommand(Play, () => !IsPlaying);
            PauseCommand = new RelayCommand(Pause, () => IsPlaying);
            StopCommand = new RelayCommand(Stop, () => !IsStopped);
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += TimerOnTick;
        }

        public bool IsPlaying
        {
            get { return wavePlayer != null && wavePlayer.PlaybackState == PlaybackState.Playing; }
            
        }

        public bool IsStopped
        {
            get { return wavePlayer == null || wavePlayer.PlaybackState == PlaybackState.Stopped; }
        }


        public IEnumerable<string> InputPathHistory { get { return inputPathHistory; } }

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
                    InputPath = file;
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

        public string InputPath
        {
            get { return inputPath; }
            set
            {
                if (inputPath != value)
                {
                    inputPath = value;
                    AddToHistory(value);
                    OnPropertyChanged("InputPath");
                }
            }
        }

        private void AddToHistory(string value)
        {
            if (!inputPathHistory.Contains(value))
            {
                inputPathHistory.Add(value);
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
                OnPropertyChanged("IsPlaying");
                OnPropertyChanged("IsStopped");
            }
        }

        private void Play()
        {
            if (String.IsNullOrEmpty(InputPath))
            {
                MessageBox.Show("Select a valid input file or URL first");
                return;
            }
            if (wavePlayer == null)
            {
                CreatePlayer();
            }
            if (lastPlayed != inputPath && reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            if (reader == null)
            {
                reader = new MediaFoundationReader(inputPath);
                lastPlayed = inputPath;
                wavePlayer.Init(reader);
            }
            wavePlayer.Play();
            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsStopped");
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
            if (stoppedEventArgs.Exception != null)
            {
                MessageBox.Show(stoppedEventArgs.Exception.Message, "Error Playing File");
            }
            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsStopped");
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