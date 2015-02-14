using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.EqualizationDemo
{
    class EqualizationDemoViewModel : ViewModelBase, IDisposable
    {
        private AudioFileReader reader;
        private IWavePlayer player;
        private Equalizer equalizer;
        private string selectedFile;
        private readonly EqualizerBand[] bands;
        
        public ICommand OpenFileCommand { get; private set; }
        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public EqualizationDemoViewModel()
        {
            PlayCommand = new DelegateCommand(Play);
            OpenFileCommand = new DelegateCommand(OpenFile);
            StopCommand = new DelegateCommand(Stop);
            PauseCommand = new DelegateCommand(Pause);
            bands = new EqualizerBand[]
                    {
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 100, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 200, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 400, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 800, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 1200, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 2400, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 4800, Gain = 0},
                        new EqualizerBand {Bandwidth = 0.8f, Frequency = 9600, Gain = 0},
                    };
            this.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (equalizer!=null) equalizer.Update();
        }

        public float MinimumGain
        {
            get { return -30; }
        }

        public float MaximumGain
        {
            get { return 30; } 
        }

        public float Band1
        {
            get { return bands[0].Gain; }
            set
            {
                if (bands[0].Gain != value)
                {
                    bands[0].Gain = value;
                    OnPropertyChanged("Band1");
                }
            }
        }

        public float Band2
        {
            get { return bands[1].Gain; }
            set
            {
                if (bands[1].Gain != value)
                {
                    bands[1].Gain = value;
                    OnPropertyChanged("Band2");
                }
            }
        }

        public float Band3
        {
            get { return bands[2].Gain; }
            set
            {
                if (bands[2].Gain != value)
                {
                    bands[2].Gain = value;
                    OnPropertyChanged("Band3");
                }
            }
        }

        public float Band4
        {
            get { return bands[3].Gain; }
            set
            {
                if (bands[3].Gain != value)
                {
                    bands[3].Gain = value;
                    OnPropertyChanged("Band4");
                }
            }
        }

        public float Band5
        {
            get { return bands[4].Gain; }
            set
            {
                if (bands[4].Gain != value)
                {
                    bands[4].Gain = value;
                    OnPropertyChanged("Band5");
                }
            }
        }

        public float Band6
        {
            get { return bands[5].Gain; }
            set
            {
                if (bands[5].Gain != value)
                {
                    bands[5].Gain = value;
                    OnPropertyChanged("Band6");
                }
            }
        }


        public float Band7
        {
            get { return bands[6].Gain; }
            set
            {
                if (bands[6].Gain != value)
                {
                    bands[6].Gain = value;
                    OnPropertyChanged("Band7");
                }
            }
        }

        public float Band8
        {
            get { return bands[7].Gain; }
            set
            {
                if (bands[7].Gain != value)
                {
                    bands[7].Gain = value;
                    OnPropertyChanged("Band7");
                }
            }
        }

        private void Pause()
        {
            if (player != null)
            {
                player.Pause();
            }
        }

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                selectedFile = openFileDialog.FileName;
                reader = new AudioFileReader(selectedFile);
                equalizer = new Equalizer(reader, bands);
                player = new WaveOutEvent();
                player.Init(equalizer);
            }
        }

        private void Play()
        {
            if (selectedFile == null)
            {
                OpenFile();
            }
            if (selectedFile != null)
            {
                player.Play();
            }
        }

        private void Stop()
        {
            if (player != null)
            {
                player.Stop();
            }
        }

        public void Dispose()
        {
            if (player != null)
            {
                player.Dispose();
            }
            if (reader != null)
            {
                reader.Dispose();
            }
            
        }
    }
}
