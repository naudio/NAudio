using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.EqualizationDemo;

internal class EqualizationDemoViewModel : ViewModelBase, IDisposable
{
    private AudioFileReader reader;
    private IWavePlayer player;
    private Equalizer equalizer;
    private VolumeSampleProvider volumeProvider;
    private string selectedFile;
    private readonly EqualizerBand[] bands;
    private readonly DispatcherTimer positionTimer = new();
    private double volumeDb;
    private double positionSeconds;
    private double durationSeconds;
    // Set while the timer writes the position back, so the resulting binding update doesn't
    // seek the reader to where it already is.
    private bool suppressSeek;

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
                    new() {Q = 0.8f, Frequency = 100, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 200, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 400, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 800, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 1200, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 2400, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 4800, GainDb = 0},
                    new() {Q = 0.8f, Frequency = 9600, GainDb = 0},
                };
        // One view model per band so each slider can label itself with the frequency it steers.
        Bands = bands.Select(b => new EqualizerBandViewModel(b, () => equalizer?.Update())).ToList();

        positionTimer.Interval = TimeSpan.FromMilliseconds(250);
        positionTimer.Tick += (_, _) => UpdatePosition();
    }

    /// <summary>The EQ bands, in ascending frequency order — one slider each.</summary>
    public IList<EqualizerBandViewModel> Bands { get; }

    public float MinimumGain => -30;

    public float MaximumGain => 30;

    /// <summary>Top-of-scale caption for the gain axis, e.g. "+30 dB".</summary>
    public string MaximumGainText => $"{MaximumGain:+0;-0;0} dB";

    /// <summary>Bottom-of-scale caption for the gain axis, e.g. "-30 dB".</summary>
    public string MinimumGainText => $"{MinimumGain:+0;-0;0} dB";

    /// <summary>Bottom of the volume slider's range, in dB — bound as the slider's Minimum.</summary>
    public double MinimumVolumeDb => TransportFormatting.MinVolumeDb;

    /// <summary>Output volume in dB (-60 = mute .. 0 = unity), applied after the EQ.</summary>
    public double VolumeDb
    {
        get => volumeDb;
        set
        {
            if (volumeDb == value) return;
            volumeDb = value;
            if (volumeProvider != null) volumeProvider.Volume = TransportFormatting.GainFromDb(value);
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
            if (!suppressSeek && reader != null)
            {
                reader.CurrentTime = TimeSpan.FromSeconds(value);
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
        if (reader == null) return;
        suppressSeek = true;
        PositionSeconds = reader.CurrentTime.TotalSeconds;
        suppressSeek = false;
    }

    private void Pause()
    {
        player?.Pause();
        positionTimer.Stop();
    }

    private void OpenFile()
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*";
        bool? result = openFileDialog.ShowDialog();
        if (result.HasValue && result.Value)
        {
            // Tear the previous graph down first — opening a second file used to leave the old
            // player and reader alive. Unhook the event before stopping so the pending
            // PlaybackStopped can't come back and poll a disposed reader.
            positionTimer.Stop();
            if (player != null)
            {
                player.PlaybackStopped -= OnPlaybackStopped;
                player.Stop();
                player.Dispose();
                player = null;
            }
            reader?.Dispose();
            reader = null;

            selectedFile = openFileDialog.FileName;
            reader = new AudioFileReader(selectedFile);
            equalizer = new Equalizer(bands);
            volumeProvider = new VolumeSampleProvider(new EffectSampleProvider(reader, equalizer))
            {
                Volume = TransportFormatting.GainFromDb(volumeDb)
            };
            player = new WaveOut();
            player.PlaybackStopped += OnPlaybackStopped;
            player.Init(volumeProvider);

            DurationSeconds = reader.TotalTime.TotalSeconds;
            UpdatePosition();
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        positionTimer.Stop();
        UpdatePosition();
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
            positionTimer.Start();
        }
    }

    private void Stop()
    {
        player?.Stop();
        positionTimer.Stop();
        if (reader != null)
        {
            reader.Position = 0;
            UpdatePosition();
        }
    }

    public void Dispose()
    {
        positionTimer.Stop();
        if (player != null) player.PlaybackStopped -= OnPlaybackStopped;
        player?.Dispose();
        reader?.Dispose();
    }
}
