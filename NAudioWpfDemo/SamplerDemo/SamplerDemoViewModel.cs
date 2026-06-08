using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Sampler;
using NAudio.Sequencing;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.SamplerDemo
{
    /// <summary>
    /// Loads a SoundFont (.sf2) and a MIDI file (.mid) and plays the MIDI through
    /// the SoundFont via <see cref="SoundFontSampler"/>, either live
    /// (<see cref="SequencedMidiInstrument"/> → <see cref="WaveOut"/>) or rendered
    /// offline to a WAV (<see cref="OfflineMidiRenderer"/>).
    /// </summary>
    class SamplerDemoViewModel : ViewModelBase, IDisposable
    {
        private const int SampleRate = 44100;

        private IWavePlayer waveOut;
        private string soundFontPath;
        private string midiFilePath;
        private string status = "Choose a SoundFont (.sf2) and a MIDI file (.mid), then Play or Render.";

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
                var sampler = CreateSampler();
                var transport = new Transport(sequence.TempoMap, sampler.WaveFormat.SampleRate);
                var instrument = new SequencedMidiInstrument(transport, sequence.Timeline, sampler);

                transport.Play();
                waveOut = new WaveOut();
                waveOut.Init(instrument);
                waveOut.Play();
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
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
                Status = "Stopped.";
            }
        }

        private void RenderToWav()
        {
            if (!FilesReady()) return;
            var dialog = new SaveFileDialog
            {
                Filter = "WAV files (*.wav)|*.wav",
                DefaultExt = ".wav",
                FileName = Path.GetFileNameWithoutExtension(midiFilePath) + ".wav"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var sequence = MidiFileSequence.FromFile(midiFilePath);
                OfflineMidiRenderer.RenderToWaveFile(sequence, CreateSampler(), dialog.FileName);
                Status = $"Rendered to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                Status = $"Render failed: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Render error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
