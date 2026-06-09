using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Dsp;
using NAudio.Sampler;
using NAudio.SoundFile;
using NAudio.Wave;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.SampleEditorDemo
{
    /// <summary>
    /// Loads a single sample (WAV/FLAC/Ogg), maps it across the keyboard via a
    /// <see cref="SingleSampleSampler"/>, and lets you edit its loop points, root
    /// key, tuning, level, pan and amplitude envelope while auditioning live from
    /// an on-screen keyboard. Because the sampler rebuilds the region from the
    /// instrument on each note-on, every edit is heard on the next key pressed.
    /// </summary>
    class SampleEditorViewModel : ViewModelBase, IDisposable
    {
        private const int SampleRate = 44100;

        private IWavePlayer waveOut;
        private LiveMidiInstrument live;
        private SingleSampleSampler sampler;

        private string status = "Load a sample (.wav/.flac/.ogg) to begin.";
        private string sampleInfo = "(no sample loaded)";
        private bool loaded;

        public ICommand LoadCommand { get; }
        public ICommand ResetLoopCommand { get; }

        public SampleEditorViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            ResetLoopCommand = new DelegateCommand(ResetLoop);
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        public string SampleInfo
        {
            get => sampleInfo;
            set { sampleInfo = value; OnPropertyChanged(nameof(SampleInfo)); }
        }

        public bool Loaded
        {
            get => loaded;
            set { loaded = value; OnPropertyChanged(nameof(Loaded)); }
        }

        // --- editable instrument parameters (write straight through to the live instrument) ---

        private SingleSampleInstrument Inst => sampler?.Instrument;

        public int RootKey
        {
            get => Inst?.RootKey ?? 60;
            set { if (Inst != null) { Inst.RootKey = value; OnPropertyChanged(nameof(RootKey)); } }
        }

        public double TuneCents
        {
            get => Inst?.TuneCents ?? 0;
            set { if (Inst != null) { Inst.TuneCents = value; OnPropertyChanged(nameof(TuneCents)); } }
        }

        public double VolumeDb
        {
            get => Inst?.VolumeDb ?? 0;
            set { if (Inst != null) { Inst.VolumeDb = (float)value; OnPropertyChanged(nameof(VolumeDb)); } }
        }

        public double Pan
        {
            get => Inst?.Pan ?? 0;
            set { if (Inst != null) { Inst.Pan = (float)value; OnPropertyChanged(nameof(Pan)); } }
        }

        public LoopMode LoopMode
        {
            get => Inst?.LoopMode ?? LoopMode.None;
            set { if (Inst != null) { Inst.LoopMode = value; OnPropertyChanged(nameof(LoopMode)); } }
        }

        public LoopMode[] LoopModes { get; } = (LoopMode[])Enum.GetValues(typeof(LoopMode));

        public double LoopCrossfadeMs
        {
            get => (Inst?.LoopCrossfadeSeconds ?? 0) * 1000.0;
            set { if (Inst != null) { Inst.LoopCrossfadeSeconds = (float)(value / 1000.0); OnPropertyChanged(nameof(LoopCrossfadeMs)); } }
        }

        public double AttackSeconds
        {
            get => Inst?.AttackSeconds ?? 0;
            set { if (Inst != null) { Inst.AttackSeconds = (float)value; OnPropertyChanged(nameof(AttackSeconds)); } }
        }

        public double HoldSeconds
        {
            get => Inst?.HoldSeconds ?? 0;
            set { if (Inst != null) { Inst.HoldSeconds = (float)value; OnPropertyChanged(nameof(HoldSeconds)); } }
        }

        public double DecaySeconds
        {
            get => Inst?.DecaySeconds ?? 0;
            set { if (Inst != null) { Inst.DecaySeconds = (float)value; OnPropertyChanged(nameof(DecaySeconds)); } }
        }

        public double SustainLevel
        {
            get => Inst?.SustainLevel ?? 1;
            set { if (Inst != null) { Inst.SustainLevel = (float)value; OnPropertyChanged(nameof(SustainLevel)); } }
        }

        public double ReleaseSeconds
        {
            get => Inst?.ReleaseSeconds ?? 0.01;
            set { if (Inst != null) { Inst.ReleaseSeconds = (float)value; OnPropertyChanged(nameof(ReleaseSeconds)); } }
        }

        // --- markers: sample indices, mirrored to the read-out labels ---

        public int StartIndex { get => Inst?.Start ?? 0; private set => SetMarkerProperty(nameof(StartIndex), value); }
        public int EndIndex { get => Inst?.End ?? 0; private set => SetMarkerProperty(nameof(EndIndex), value); }
        public int LoopStartIndex { get => Inst?.LoopStart ?? 0; private set => SetMarkerProperty(nameof(LoopStartIndex), value); }
        public int LoopEndIndex { get => Inst?.LoopEnd ?? 0; private set => SetMarkerProperty(nameof(LoopEndIndex), value); }

        public string StartText => FormatPosition(StartIndex);
        public string EndText => FormatPosition(EndIndex);
        public string LoopStartText => FormatPosition(LoopStartIndex);
        public string LoopEndText => FormatPosition(LoopEndIndex);

        private void SetMarkerProperty(string name, int value)
        {
            OnPropertyChanged(name);
            OnPropertyChanged(name.Replace("Index", "Text"));
        }

        private string FormatPosition(int index)
        {
            if (Inst == null) return "-";
            double ms = index * 1000.0 / Inst.SampleRate;
            return $"{index:N0} ({ms:N1} ms)";
        }

        /// <summary>Applies a marker change coming from the waveform (drag).</summary>
        public void SetMarkerFromWaveform(SampleMarker marker, int index)
        {
            if (Inst == null) return;
            switch (marker)
            {
                case SampleMarker.Start: Inst.Start = index; OnPropertyChanged(nameof(StartIndex)); OnPropertyChanged(nameof(StartText)); break;
                case SampleMarker.End: Inst.End = index; OnPropertyChanged(nameof(EndIndex)); OnPropertyChanged(nameof(EndText)); break;
                case SampleMarker.LoopStart: Inst.LoopStart = index; OnPropertyChanged(nameof(LoopStartIndex)); OnPropertyChanged(nameof(LoopStartText)); break;
                case SampleMarker.LoopEnd: Inst.LoopEnd = index; OnPropertyChanged(nameof(LoopEndIndex)); OnPropertyChanged(nameof(LoopEndText)); break;
            }
        }

        /// <summary>The mono mix of the loaded sample for waveform display, or null.</summary>
        public float[] WaveformSamples { get; private set; }

        /// <summary>Raised after a new sample loads, so the view can refresh the waveform.</summary>
        public event Action SampleLoaded;

        /// <summary>Plays a note from the on-screen keyboard.</summary>
        public void PlayNote(int note, int velocity) => live?.NoteOn(0, note, velocity);

        /// <summary>Stops a note from the on-screen keyboard.</summary>
        public void StopNote(int note) => live?.NoteOff(0, note);

        /// <summary>
        /// Fills <paramref name="dest"/> with the source-sample read position of each
        /// sounding voice (for the waveform playback indicator), returning the count.
        /// </summary>
        public int ReadPlaybackPositions(double[] dest) => sampler?.GetActivePlaybackPositions(dest) ?? 0;

        private void Load()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio (*.wav;*.flac;*.ogg)|*.wav;*.flac;*.ogg|WAV (*.wav)|*.wav|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                if (!TryLoadSample(dialog.FileName, out var left, out var right, out var rate))
                {
                    Status = $"Could not load '{Path.GetFileName(dialog.FileName)}'.";
                    return;
                }

                Stop();
                var instrument = new SingleSampleInstrument(left, rate, rootKey: 60, dataRight: right)
                {
                    End = left.Length,
                    LoopStart = 0,
                    LoopEnd = left.Length
                };
                sampler = new SingleSampleSampler(instrument, SampleRate);
                live = new LiveMidiInstrument(sampler);

                waveOut = SamplerPlayback.Create(live, OnPlaybackError);
                waveOut.Play();

                WaveformSamples = BuildMono(left, right);
                Loaded = true;
                RefreshAll();
                SampleLoaded?.Invoke();
                SampleInfo = $"{Path.GetFileName(dialog.FileName)} — {rate} Hz, {(right != null ? "stereo" : "mono")}, {left.Length:N0} samples";
                Status = "Loaded. Play the on-screen keyboard; edits are heard on the next note.";
            }
            catch (Exception ex)
            {
                Stop();
                Status = $"Load failed: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Sample editor error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool TryLoadSample(string path, out float[] left, out float[] right, out int rate)
        {
            if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                return WaveSampleLoader.TryLoad(path, out left, out right, out rate);
            using var reader = new SoundFileReader(path);
            return WaveSampleLoader.TryLoad(reader, out left, out right, out rate);
        }

        private static float[] BuildMono(float[] left, float[] right)
        {
            if (right == null) return left;
            var mono = new float[left.Length];
            for (int i = 0; i < left.Length; i++) mono[i] = 0.5f * (left[i] + right[i]);
            return mono;
        }

        private void ResetLoop()
        {
            if (Inst == null) return;
            Inst.Start = 0;
            Inst.End = Inst.Length;
            Inst.LoopStart = 0;
            Inst.LoopEnd = Inst.Length;
            RefreshAll();
            SampleLoaded?.Invoke();
        }

        // pushes every value from the instrument back out to the bindings (and labels)
        private void RefreshAll()
        {
            OnPropertyChanged(nameof(RootKey));
            OnPropertyChanged(nameof(TuneCents));
            OnPropertyChanged(nameof(VolumeDb));
            OnPropertyChanged(nameof(Pan));
            OnPropertyChanged(nameof(LoopMode));
            OnPropertyChanged(nameof(LoopCrossfadeMs));
            OnPropertyChanged(nameof(AttackSeconds));
            OnPropertyChanged(nameof(HoldSeconds));
            OnPropertyChanged(nameof(DecaySeconds));
            OnPropertyChanged(nameof(SustainLevel));
            OnPropertyChanged(nameof(ReleaseSeconds));
            foreach (var name in new[] { nameof(StartIndex), nameof(EndIndex), nameof(LoopStartIndex), nameof(LoopEndIndex) })
                SetMarkerProperty(name, 0);
        }

        private void OnPlaybackError(Exception ex)
        {
            Status = $"Playback stopped: {ex.Message}";
            MessageBox.Show(ex.ToString(), "Playback error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Stop()
        {
            waveOut?.Dispose();
            waveOut = null;
            live = null;
            sampler = null;
            Loaded = false;
        }

        public void Dispose() => Stop();
    }
}
