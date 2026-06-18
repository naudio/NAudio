using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Midi;
using NAudio.Sequencing;
using NAudio.Vst3;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;
using NAudioWpfDemo.Vst3Shared;

namespace NAudioWpfDemo.Vst3MidiFileDemo
{
    /// <summary>
    /// Plays a MIDI file through a chosen VST 3 instrument (VSTi), the VST3 counterpart of the
    /// SoundFont/MIDI player demo. Pick an instrument from the shared catalogue and a <c>.mid</c> file,
    /// then either play it live (<see cref="SequencedMidiPlayer"/> → <see cref="WasapiPlayer"/>, falling
    /// back to <see cref="WaveOut"/>) or render it offline to a WAV (<see cref="OfflineMidiRenderer"/>).
    /// Both drive a <see cref="Vst3MidiInstrument"/> over the shared <c>NAudio.Midi</c> pipeline. A volume
    /// slider and a draggable position bar (seeking via the <see cref="Transport"/>, which the instrument
    /// reads to keep its <c>ProcessContext</c> in step) are wired in during live playback, and the plug-in's
    /// native editor can be popped out.
    /// </summary>
    class Vst3MidiFileDemoViewModel : ViewModelBase, IDisposable
    {
        private const int SampleRate = 44100;
        private const int MaxBlockSize = 1024;

        private IWavePlayer player;
        private Transport transport;
        private Vst3Module module;
        private Vst3Plugin plugin;
        private Vst3MidiInstrument instrument;
        private Vst3PluginView view;
        private Vst3EditorWindow editorWindow;
        private VolumeSampleProvider volumeProvider;
        private readonly DispatcherTimer positionTimer;

        private Vst3InstalledPlugin selectedInstrument;
        private string midiFilePath;
        private string status = "Loading installed VST 3 instruments…";
        private double volumeDb;      // dB attenuation for the volume slider; 0 = unity
        private double positionSeconds;
        private double durationSeconds;
        private bool suppressSeek;    // true while the timer updates the position (so it doesn't seek)
        private int loadVersion;      // bumped by Stop so a load finishing afterwards is discarded
        private bool disposed;

        public ObservableCollection<Vst3InstalledPlugin> Instruments { get; } = new();

        public ICommand BrowseMidiCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RenderToWavCommand { get; }
        public ICommand RescanInstrumentsCommand { get; }
        public ICommand ShowEditorCommand { get; }

        public Vst3MidiFileDemoViewModel()
        {
            BrowseMidiCommand = new DelegateCommand(BrowseMidi);
            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
            RenderToWavCommand = new DelegateCommand(RenderToWav);
            RescanInstrumentsCommand = new DelegateCommand(RescanInstruments) { IsEnabled = false };
            ShowEditorCommand = new DelegateCommand(ShowEditor) { IsEnabled = false };

            positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            positionTimer.Tick += (_, _) => UpdatePosition();

            _ = LoadInstrumentsAsync(Vst3InstalledPlugins.GetAsync());
        }

        public Vst3InstalledPlugin SelectedInstrument
        {
            get => selectedInstrument;
            set { selectedInstrument = value; OnPropertyChanged(nameof(SelectedInstrument)); }
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

        // perceived loudness is logarithmic, so the slider works in dB: equal slider travel then means
        // equal audible change. The bottom of the range is treated as mute.
        private const double MinVolumeDb = -60;

        private static float GainFromDb(double db) =>
            db <= MinVolumeDb ? 0f : (float)Math.Pow(10.0, db / 20.0);

        /// <summary>Output volume in dB (-60 = mute .. 0 = unity), applied live to the playing instrument.</summary>
        public double VolumeDb
        {
            get => volumeDb;
            set
            {
                volumeDb = value;
                OnPropertyChanged(nameof(VolumeDb));
                OnPropertyChanged(nameof(VolumeText));
                if (volumeProvider != null) volumeProvider.Volume = GainFromDb(value);
            }
        }

        /// <summary>The volume readout ("-12.0 dB", or "Mute" at the bottom of the range).</summary>
        public string VolumeText => volumeDb <= MinVolumeDb ? "Mute" : $"{volumeDb:F1} dB";

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

        private async Task LoadInstrumentsAsync(Task<IReadOnlyList<Vst3InstalledPlugin>> source)
        {
            IReadOnlyList<Vst3InstalledPlugin> instruments;
            try
            {
                var all = await source.ConfigureAwait(true);
                instruments = all.Where(p => p.Class.IsInstrument).ToList();
            }
            catch (Exception ex)
            {
                if (disposed) return;
                Status = "Plug-in scan failed: " + ex.Message;
                ((DelegateCommand)RescanInstrumentsCommand).IsEnabled = true;
                return;
            }
            if (disposed) return;
            Instruments.Clear();
            foreach (var i in instruments) Instruments.Add(i);
            ((DelegateCommand)RescanInstrumentsCommand).IsEnabled = true;
            Status = instruments.Count == 0
                ? "No VST 3 instruments found in the standard install folders."
                : "Pick an instrument and a MIDI file, then Play or Render.";
        }

        private void RescanInstruments()
        {
            SelectedInstrument = null;
            ((DelegateCommand)RescanInstrumentsCommand).IsEnabled = false;
            Status = "Rescanning installed VST 3 plug-ins…";
            _ = LoadInstrumentsAsync(Vst3InstalledPlugins.RescanAsync());
        }

        private void BrowseMidi()
        {
            var dialog = new OpenFileDialog { Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) MidiFilePath = dialog.FileName;
        }

        private void Play()
        {
            if (!Ready()) return;
            Stop();

            // The instrument and its editor view live on HWNDs, so — unlike the SoundFont demo, whose
            // heavy SF2 parse runs off-thread — we create the plug-in on the UI thread (the pattern the
            // other VST3 demos use). MIDI parsing is light enough to sit here too.
            try
            {
                var sequence = MidiFileSequence.FromFile(midiFilePath);

                module = Vst3Module.Load(selectedInstrument.Module.Path);
                plugin = module.CreatePlugin(selectedInstrument.Class, SampleRate, MaxBlockSize);
                if (!plugin.IsInstrument)
                    throw new InvalidOperationException($"{selectedInstrument.Class.Name} did not initialise as an instrument.");
                view = TryCreateView(plugin);

                transport = new Transport(sequence.TempoMap, SampleRate);
                instrument = new Vst3MidiInstrument(plugin, sequence.TempoMap, sequence.TimeSignatureMap, transport);
                var sequencedPlayer = new SequencedMidiPlayer(transport, sequence.Timeline, instrument);
                volumeProvider = new VolumeSampleProvider(sequencedPlayer) { Volume = GainFromDb(volumeDb) };

                DurationSeconds = sequence.DurationFrames(SampleRate, tailSeconds: 1.0) / (double)SampleRate;
                suppressSeek = true;
                PositionSeconds = 0;
                suppressSeek = false;

                transport.Play();
                player = SamplerPlayback.Create(volumeProvider, OnPlaybackError);
                player.Play();
                positionTimer.Start();
                ((DelegateCommand)ShowEditorCommand).IsEnabled = view != null;
                Status = $"Playing {Path.GetFileName(midiFilePath)} through {selectedInstrument.Display}";
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
            loadVersion++; // discard any render still in flight
            positionTimer.Stop();
            CloseEditorWindow();
            ((DelegateCommand)ShowEditorCommand).IsEnabled = false;

            if (player != null)
            {
                player.Dispose();
                player = null;
                Status = "Stopped.";
            }
            view?.Dispose();
            view = null;
            plugin?.Dispose();
            plugin = null;
            module?.Dispose();
            module = null;
            transport = null;
            instrument = null;
            volumeProvider = null;
        }

        private void OnPlaybackError(Exception ex)
        {
            Stop();
            Status = $"Playback stopped: {ex.Message}";
        }

        // moves the transport to a new position; silences sounding voices so a seek doesn't leave notes
        // hanging (their note-offs were before/after the jump). The instrument reads the transport, so its
        // ProcessContext position follows automatically.
        private void Seek(double seconds)
        {
            if (transport == null) return;
            transport.SeekFrames((long)(Math.Max(0, seconds) * SampleRate));
            instrument?.AllSoundOff();
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
            if (!Ready()) return;
            var dialog = new SaveFileDialog
            {
                Filter = "WAV files (*.wav)|*.wav",
                DefaultExt = ".wav",
                FileName = Path.GetFileNameWithoutExtension(midiFilePath) + ".wav"
            };
            if (dialog.ShowDialog() != true) return;

            // capture selections so the background thread doesn't touch UI-bound state
            string outputPath = dialog.FileName, mid = midiFilePath;
            var moduleInfo = selectedInstrument.Module;
            var classInfo = selectedInstrument.Class;
            int version = loadVersion;

            SetCommandsEnabled(false);
            Status = $"Rendering {Path.GetFileName(mid)} to {Path.GetFileName(outputPath)}…";
            try
            {
                // a fresh, headless plug-in instance (no editor) renders faster-than-real-time off the UI thread
                await Task.Run(() =>
                {
                    var sequence = MidiFileSequence.FromFile(mid);
                    using var renderModule = Vst3Module.Load(moduleInfo.Path);
                    using var renderPlugin = renderModule.CreatePlugin(classInfo, SampleRate, MaxBlockSize);
                    if (!renderPlugin.IsInstrument)
                        throw new InvalidOperationException($"{classInfo.Name} did not initialise as an instrument.");
                    var renderInstrument = new Vst3MidiInstrument(renderPlugin, sequence.TempoMap);
                    OfflineMidiRenderer.RenderToWaveFile(sequence, renderInstrument, outputPath);
                });
                if (version != loadVersion) return; // stopped while rendering
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

        private void ShowEditor()
        {
            if (view == null) return;
            if (editorWindow != null) { editorWindow.Activate(); return; }
            editorWindow = new Vst3EditorWindow(selectedInstrument?.Display ?? "VST 3 Instrument", view);
            editorWindow.ClosedByUser += (_, _) => editorWindow = null;
            editorWindow.Show();
        }

        private void CloseEditorWindow()
        {
            if (editorWindow == null) return;
            var w = editorWindow;
            editorWindow = null;
            try { w.Close(); } catch { /* already closing */ }
        }

        private static Vst3PluginView TryCreateView(Vst3Plugin plugin)
        {
            try { return plugin.CreateView(); }
            catch { return null; } // plug-in has no editor, or it failed to create — playback still works
        }

        private void SetCommandsEnabled(bool enabled)
        {
            ((DelegateCommand)PlayCommand).IsEnabled = enabled;
            ((DelegateCommand)RenderToWavCommand).IsEnabled = enabled;
        }

        private bool Ready()
        {
            if (selectedInstrument == null)
            {
                Status = "Please choose a VST 3 instrument.";
                return false;
            }
            if (string.IsNullOrEmpty(midiFilePath) || !File.Exists(midiFilePath))
            {
                Status = "Please choose a MIDI (.mid) file.";
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            disposed = true;
            Stop();
        }
    }
}
