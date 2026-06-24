using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Effects;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.EqualizationDemo;

class EqualizationDemoViewModel : ViewModelBase, IDisposable
{
    private AudioFileReader reader;
    private IWavePlayer player;
    private Equalizer equalizer;
    private string selectedFile;
    private readonly EqualizerBand[] bands;

    public ICommand OpenFileCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }

    public EqualizationDemoViewModel()
    {
        PlayCommand = new DelegateCommand(Play);
        OpenFileCommand = new DelegateCommand(OpenFile);
        StopCommand = new DelegateCommand(Stop);
        PauseCommand = new DelegateCommand(Pause);
        bands = new EqualizerBand[]
                {
                    new EqualizerBand {Q = 0.8f, Frequency = 100, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 200, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 400, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 800, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 1200, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 2400, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 4800, GainDb = 0},
                    new EqualizerBand {Q = 0.8f, Frequency = 9600, GainDb = 0},
                };
        this.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        equalizer?.Update();
    }

    public float MinimumGain => -30;

    public float MaximumGain => 30;

    public float Band1
    {
        get => bands[0].GainDb;
        set
        {
            if (bands[0].GainDb != value)
            {
                bands[0].GainDb = value;
                OnPropertyChanged("Band1");
            }
        }
    }

    public float Band2
    {
        get => bands[1].GainDb;
        set
        {
            if (bands[1].GainDb != value)
            {
                bands[1].GainDb = value;
                OnPropertyChanged("Band2");
            }
        }
    }

    public float Band3
    {
        get => bands[2].GainDb;
        set
        {
            if (bands[2].GainDb != value)
            {
                bands[2].GainDb = value;
                OnPropertyChanged("Band3");
            }
        }
    }

    public float Band4
    {
        get => bands[3].GainDb;
        set
        {
            if (bands[3].GainDb != value)
            {
                bands[3].GainDb = value;
                OnPropertyChanged("Band4");
            }
        }
    }

    public float Band5
    {
        get => bands[4].GainDb;
        set
        {
            if (bands[4].GainDb != value)
            {
                bands[4].GainDb = value;
                OnPropertyChanged("Band5");
            }
        }
    }

    public float Band6
    {
        get => bands[5].GainDb;
        set
        {
            if (bands[5].GainDb != value)
            {
                bands[5].GainDb = value;
                OnPropertyChanged("Band6");
            }
        }
    }


    public float Band7
    {
        get => bands[6].GainDb;
        set
        {
            if (bands[6].GainDb != value)
            {
                bands[6].GainDb = value;
                OnPropertyChanged("Band7");
            }
        }
    }

    public float Band8
    {
        get => bands[7].GainDb;
        set
        {
            if (bands[7].GainDb != value)
            {
                bands[7].GainDb = value;
                OnPropertyChanged("Band8");
            }
        }
    }

    private void Pause()
    {
        player?.Pause();
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
            equalizer = new Equalizer(bands);
            player = new WaveOut();
            player.Init(new EffectSampleProvider(reader, equalizer));
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
        player?.Stop();
    }

    public void Dispose()
    {
        player?.Dispose();
        reader?.Dispose();
    }
}
