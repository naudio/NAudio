using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Extras;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.AudioPlaybackDemo;

internal class AudioPlaybackViewModel : ViewModelBase, IDisposable
{
    private readonly AudioPlayback audioPlayback;
    private readonly List<IVisualizationPlugin> visualizations;
    private readonly DispatcherTimer positionTimer = new();
    private IVisualizationPlugin selectedVisualization;
    private string selectedFile;
    private double volumeDb;
    private double positionSeconds;
    private double durationSeconds;
    // Set while the timer is writing the position back, so the resulting binding update doesn't
    // seek the reader to where it already is.
    private bool suppressSeek;

    public ICommand OpenFileCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }

    public AudioPlaybackViewModel(IEnumerable<IVisualizationPlugin> visualizations)
    {
        this.visualizations = new List<IVisualizationPlugin>(visualizations);
        this.selectedVisualization = this.visualizations.FirstOrDefault();

        this.audioPlayback = new AudioPlayback();
        audioPlayback.MaximumCalculated += audioGraph_MaximumCalculated;
        audioPlayback.FftCalculated += audioGraph_FftCalculated;
        audioPlayback.PlaybackStopped += OnPlaybackStopped;

        PlayCommand = new DelegateCommand(Play);
        OpenFileCommand = new DelegateCommand(OpenFile);
        StopCommand = new DelegateCommand(Stop);
        PauseCommand = new DelegateCommand(Pause);

        positionTimer.Interval = TimeSpan.FromMilliseconds(250);
        positionTimer.Tick += (_, _) => UpdatePosition();
    }

    private void Pause()
    {
        audioPlayback.Pause();
        positionTimer.Stop();
    }

    public IList<IVisualizationPlugin> Visualizations { get { return this.visualizations; } }

    public IVisualizationPlugin SelectedVisualization
    {
        get
        {
            return this.selectedVisualization;
        }
        set
        {
            if (this.selectedVisualization != value)
            {
                this.selectedVisualization = value;
                OnPropertyChanged("SelectedVisualization");
                OnPropertyChanged("Visualization");
                // If a file is already loaded when the user switches visualization, make sure
                // the newly-selected one gets the sample rate too.
                if (this.selectedVisualization != null && audioPlayback.SampleRate > 0)
                {
                    this.selectedVisualization.OnSourceChanged(audioPlayback.SampleRate);
                }
            }
        }
    }

    public object Visualization
    {
        get
        {
            return this.selectedVisualization.Content;
        }
    }

    /// <summary>Bottom of the volume slider's range, in dB — bound as the slider's Minimum.</summary>
    public double MinimumVolumeDb => TransportFormatting.MinVolumeDb;

    /// <summary>Monitoring volume in dB (-60 = mute .. 0 = unity), applied live.</summary>
    public double VolumeDb
    {
        get => volumeDb;
        set
        {
            if (volumeDb == value) return;
            volumeDb = value;
            audioPlayback.Volume = TransportFormatting.GainFromDb(value);
            OnPropertyChanged(nameof(VolumeDb));
            OnPropertyChanged(nameof(VolumeText));
        }
    }

    public string VolumeText => TransportFormatting.FormatVolume(volumeDb);

    /// <summary>Current playback position in seconds. Setting it (dragging the slider) seeks.</summary>
    public double PositionSeconds
    {
        get => positionSeconds;
        set
        {
            if (positionSeconds == value) return;
            positionSeconds = value;
            if (!suppressSeek && audioPlayback.HasFile)
            {
                audioPlayback.CurrentTime = TimeSpan.FromSeconds(value);
            }
            OnPropertyChanged(nameof(PositionSeconds));
            OnPropertyChanged(nameof(PositionText));
        }
    }

    /// <summary>Length of the loaded file in seconds — the position slider's range.</summary>
    public double DurationSeconds
    {
        get => durationSeconds;
        private set
        {
            durationSeconds = value;
            OnPropertyChanged(nameof(DurationSeconds));
            OnPropertyChanged(nameof(PositionText));
        }
    }

    public string PositionText =>
        $"{TransportFormatting.FormatTime(TimeSpan.FromSeconds(positionSeconds))} / " +
        $"{TransportFormatting.FormatTime(TimeSpan.FromSeconds(durationSeconds))}";

    private void UpdatePosition()
    {
        if (!audioPlayback.HasFile) return;
        suppressSeek = true;
        PositionSeconds = audioPlayback.CurrentTime.TotalSeconds;
        suppressSeek = false;
    }

    private void OnPlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
    {
        positionTimer.Stop();
        UpdatePosition();
    }

    private void audioGraph_FftCalculated(object sender, FftEventArgs e)
    {
        if (this.SelectedVisualization != null)
        {
            Application.Current?.Dispatcher?.BeginInvoke(() =>
                this.SelectedVisualization?.OnFftCalculated(e.Result));
        }
    }

    private void audioGraph_MaximumCalculated(object sender, MaxSampleEventArgs e)
    {
        if (this.SelectedVisualization != null)
        {
            Application.Current?.Dispatcher?.BeginInvoke(() =>
                this.SelectedVisualization?.OnMaxCalculated(e.MinSample, e.MaxSample));
        }
    }

    private void OpenFile()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "All Supported Files|*.wav;*.aiff;*.mp3;*.wma;*.aac;*.mp4;*.m4a;*.flac;*.opus;*.ogg;*.mka;*.webm|All Files (*.*)|*.*";
        bool? result = openFileDialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            this.selectedFile = openFileDialog.FileName;
            audioPlayback.Load(this.selectedFile);
            // Now that we know the sample rate, let the visualization configure itself
            // (e.g. the spectrum analyser uses it for the frequency x-axis).
            this.selectedVisualization?.OnSourceChanged(audioPlayback.SampleRate);
            DurationSeconds = audioPlayback.TotalTime.TotalSeconds;
            UpdatePosition();
        }
    }

    private void Play()
    {
        if (this.selectedFile == null)
        {
            OpenFile();
        }
        if (this.selectedFile != null)
        {
            audioPlayback.Play();
            positionTimer.Start();
        }
    }

    private void Stop()
    {
        audioPlayback.Stop();
        positionTimer.Stop();
        UpdatePosition();
    }

    public void Dispose()
    {
        positionTimer.Stop();
        audioPlayback.Dispose();
    }
}
