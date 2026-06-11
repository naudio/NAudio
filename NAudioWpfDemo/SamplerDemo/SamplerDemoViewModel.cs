using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Sequencing;
using NAudio.Wave;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.SamplerDemo
{
    /// <summary>
    /// Loads a SoundFont (.sf2) and a MIDI file (.mid) and plays the MIDI through
    /// the SoundFont via <see cref="SoundFontSampler"/>, either live
    /// (<see cref="SequencedMidiPlayer"/> → <see cref="WasapiPlayer"/>, falling
    /// back to <see cref="WaveOut"/> if WASAPI is unavailable) or rendered
    /// offline to a WAV (<see cref="OfflineMidiRenderer"/>). A volume slider and a
    /// draggable position bar (seeking via the <see cref="Transport"/>) are wired in
    /// during live playback.
    /// </summary>
    class SamplerDemoViewModel : ViewModelBase, IDisposable
    {
        private const int SampleRate = 44100;

        private IWavePlayer player;
        private Transport transport;
        private SoundFontSampler sampler;
        private readonly DispatcherTimer positionTimer;

        private string soundFontPath;
        private string midiFilePath;
        private string status = "Choose a SoundFont (.sf2) and a MIDI file (.mid), then Play or Render.";
        private double volume = 1.0;
        private double positionSeconds;
        private double durationSeconds;
        private bool suppressSeek; // true while the timer updates the position (so it doesn't seek)

        public ICommand BrowseSoundFontCommand { get; }
        public ICommand BrowseMidiCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RenderToWavCommand { get; }

        public SamplerDemoViewModel()
        {
            BrowseSoundFontCommand = new DelegateCommand(BrowseSoundFont);
            BrowseMidiCommand = new DelegateCommand(BrowseMidi);
            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
            RenderToWavCommand = new DelegateCommand(RenderToWav);

            positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            positionTimer.Tick += (_, _) => UpdatePosition();
        }

        public string SoundFontPath
        {
            get => soundFontPath;
            set { soundFontPath = value; OnPropertyChanged(nameof(SoundFontPath)); }
        }

        public string MidiFilePath
        {
            get => midiFilePath;
            set { midiFilePath = value; OnPropertyChanged(nameof(MidiFilePath)); }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        /// <summary>Output volume, 0..1, applied live to the playing sampler.</summary>
        public double Volume
        {
            get => volume;
            set
            {
                volume = value;
                OnPropertyChanged(nameof(Volume));
                if (sampler != null) sampler.MasterGain = (float)value;
            }
        }

        /// <summary>Current playback position in seconds. Setting it (e.g. dragging the bar) seeks.</summary>
        public double PositionSeconds
        {
            get => positionSeconds;
            set
            {
                positionSeconds = value;
                OnPropertyChanged(nameof(PositionSeconds));
                OnPropertyChanged(nameof(PositionText));
                if (!suppressSeek) Seek(value);
            }
        }

        /// <summary>Total length of the loaded MIDI file in seconds (the position bar's range).</summary>
        public double DurationSeconds
        {
            get => durationSeconds;
            set { durationSeconds = value; OnPropertyChanged(nameof(DurationSeconds)); OnPropertyChanged(nameof(PositionText)); }
        }

        public string PositionText => $"{Format(positionSeconds)} / {Format(durationSeconds)}";

        private static string Format(double seconds) =>
            TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"m\:ss");

        private void BrowseSoundFont()
        {
            var dialog = new OpenFileDialog { Filter = "SoundFont (*.sf2)|*.sf2|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) SoundFontPath = dialog.FileName;
        }

        private void BrowseMidi()
        {
            var dialog = new OpenFileDialog { Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) MidiFilePath = dialog.FileName;
        }

        private void Play()
        {
            if (!FilesReady()) return;
            Stop();
            try
            {
                var sequence = MidiFileSequence.FromFile(midiFilePath);
                sampler = CreateSampler();
                sampler.MasterGain = (float)volume;
                transport = new Transport(sequence.TempoMap, sampler.WaveFormat.SampleRate);
                var instrument = new SequencedMidiPlayer(transport, sequence.Timeline, sampler);

                DurationSeconds = sequence.DurationFrames(SampleRate, tailSeconds: 1.0) / (double)SampleRate;
                suppressSeek = true;
                PositionSeconds = 0;
                suppressSeek = false;

                transport.Play();
                player = SamplerPlayback.Create(instrument, OnPlaybackError);
                player.Play();
                positionTimer.Start();
                Status = $"Playing {Path.GetFileName(midiFilePath)} through {Path.GetFileName(soundFontPath)}";
            }
            catch (Exception ex)
            {
                Stop();
                Status = $"Playback failed: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Playback error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Stop()
        {
            positionTimer.Stop();
            if (player != null)
            {
                player.Dispose();
                player = null;
                Status = "Stopped.";
            }
            transport = null;
            sampler = null;
        }

        private void OnPlaybackError(Exception ex)
        {
            Stop();
            Status = $"Playback stopped: {ex.Message}";
        }

        // moves the transport to a new position; silences sounding voices so a
        // seek doesn't leave notes hanging (their note-offs were before/after the jump)
        private void Seek(double seconds)
        {
            if (transport == null) return;
            transport.SeekFrames((long)(Math.Max(0, seconds) * SampleRate));
            sampler?.AllSoundOff();
        }

        // reflects the transport's position on the bar; auto-stops at the end
        private void UpdatePosition()
        {
            if (transport == null) return;
            double pos = transport.CurrentFrames / (double)SampleRate;
            suppressSeek = true;
            PositionSeconds = pos;
            suppressSeek = false;
            if (durationSeconds > 0 && pos >= durationSeconds) Stop();
        }

        private async void RenderToWav()
        {
            if (!FilesReady()) return;
            var dialog = new SaveFileDialog
            {
                Filter = "WAV files (*.wav)|*.wav",
                DefaultExt = ".wav",
                FileName = Path.GetFileNameWithoutExtension(midiFilePath) + ".wav"
            };
            if (dialog.ShowDialog() != true) return;

            // capture the paths so the background thread doesn't touch UI-bound state
            string outputPath = dialog.FileName;
            string sf = soundFontPath, mid = midiFilePath;

            // render off the UI thread so a long SoundFont/MIDI render doesn't freeze
            // the window; disable Play/Render while it runs
            SetCommandsEnabled(false);
            Status = $"Rendering {Path.GetFileName(mid)} to {Path.GetFileName(outputPath)}...";
            try
            {
                await Task.Run(() =>
                {
                    var sequence = MidiFileSequence.FromFile(mid);
                    var renderSampler = new SoundFontSampler(new NAudio.SoundFont.SoundFont(sf), SampleRate);
                    OfflineMidiRenderer.RenderToWaveFile(sequence, renderSampler, outputPath);
                });
                Status = $"Rendered to {Path.GetFileName(outputPath)}";
            }
            catch (Exception ex)
            {
                Status = $"Render failed: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Render error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetCommandsEnabled(true);
            }
        }

        private void SetCommandsEnabled(bool enabled)
        {
            ((DelegateCommand)PlayCommand).IsEnabled = enabled;
            ((DelegateCommand)RenderToWavCommand).IsEnabled = enabled;
        }

        private SoundFontSampler CreateSampler() =>
            new SoundFontSampler(new NAudio.SoundFont.SoundFont(soundFontPath), SampleRate);

        private bool FilesReady()
        {
            if (string.IsNullOrEmpty(soundFontPath) || !File.Exists(soundFontPath))
            {
                Status = "Please choose a SoundFont (.sf2) file.";
                return false;
            }
            if (string.IsNullOrEmpty(midiFilePath) || !File.Exists(midiFilePath))
            {
                Status = "Please choose a MIDI (.mid) file.";
                return false;
            }
            return true;
        }

        public void Dispose() => Stop();
    }
}
