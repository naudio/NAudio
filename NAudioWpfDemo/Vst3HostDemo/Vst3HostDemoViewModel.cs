using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Vst3;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.ViewModel;
using NAudioWpfDemo.Vst3Shared;

namespace NAudioWpfDemo.Vst3HostDemo
{
    /// <summary>
    /// Hosts a chosen VST 3 effect plug-in, embeds its editor, and plays a looping drum one-shot
    /// through it via <see cref="WasapiPlayer"/>. Host-side bypass and output gain are surfaced and
    /// work. The embedded editor <i>renders and resizes</i>, but <b>interacting with its controls
    /// currently crashes the host on .NET 9</b> — a known, unresolved issue documented in
    /// <c>Docs/Architecture/Vst3Hosting.md</c> (Phase 6). Treat the editor as display-only for now.
    /// </summary>
    class Vst3HostDemoViewModel : ViewModelBase, IDisposable
    {
        private const int MaxBlockSize = 512;
        // Bundled samples in SampleData/Drums are all 44.1 kHz, so the plug-in is created at
        // that rate once on Load and the chain rebuilds with the currently-selected sample on
        // each Play. WASAPI shared mode resamples to the device rate downstream.
        private const int SampleRate = 44100;

        private Vst3Module module;
        private Vst3Plugin plugin;
        private Vst3PluginView pluginView;
        private BypassableVst3SampleProvider effect;
        private VolumeSampleProvider volume;
        private WasapiPlayer player;
        private Vst3EditorHost editorHost;

        private Vst3InstalledPlugin selectedEffect;
        private IReadOnlyList<string> programs = Array.Empty<string>();
        private int selectedProgramIndex = -1;
        private string selectedSample = "snare";
        private string status = "Loading installed VST 3 effects…";
        private bool bypass;
        private double gainDb;
        private bool isLoaded;
        private bool disposed;

        public Vst3HostDemoViewModel()
        {
            Effects = new ObservableCollection<Vst3InstalledPlugin>();
            Samples = new[] { "snare", "kick", "closed-hat", "open-hat", "crash" };
            LoadCommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
            RescanCommand = new DelegateCommand(Rescan) { IsEnabled = false };
            SavePresetCommand = new DelegateCommand(SavePreset);
            LoadPresetCommand = new DelegateCommand(LoadPreset);
            _ = LoadEffectsAsync(Vst3InstalledPlugins.GetAsync());
        }

        public ObservableCollection<Vst3InstalledPlugin> Effects { get; }
        public IReadOnlyList<string> Samples { get; }

        public ICommand LoadCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }
        public DelegateCommand RescanCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }

        public Vst3InstalledPlugin SelectedEffect
        {
            get => selectedEffect;
            set { selectedEffect = value; OnPropertyChanged(nameof(SelectedEffect)); }
        }

        public string SelectedSample
        {
            get => selectedSample;
            set { selectedSample = value; OnPropertyChanged(nameof(SelectedSample)); }
        }

        /// <summary>Factory program names from the plug-in's active program list (empty if none).</summary>
        public IReadOnlyList<string> Programs
        {
            get => programs;
            private set
            {
                programs = value;
                OnPropertyChanged(nameof(Programs));
                OnPropertyChanged(nameof(HasPrograms));
            }
        }

        /// <summary>Whether the loaded plug-in exposes a selectable program list.</summary>
        public bool HasPrograms => programs.Count > 0;

        public int SelectedProgramIndex
        {
            get => selectedProgramIndex;
            set
            {
                selectedProgramIndex = value;
                OnPropertyChanged(nameof(SelectedProgramIndex));
                if (value >= 0 && plugin != null && plugin.SupportsProgramChange)
                {
                    // SendProgramChange drives the program-change parameter; it takes effect on the
                    // next Process block (i.e. while playing).
                    plugin.SendProgramChange(value);
                    var name = value < programs.Count ? programs[value] : string.Empty;
                    Status = $"Program {value}: {name}";
                }
            }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        /// <summary>The embedded editor element, or null when the loaded plug-in has no GUI.</summary>
        public Vst3EditorHost EditorHost
        {
            get => editorHost;
            private set { editorHost = value; OnPropertyChanged(nameof(EditorHost)); }
        }

        public bool Bypass
        {
            get => bypass;
            set
            {
                bypass = value;
                if (effect != null) effect.Bypass = value;
                OnPropertyChanged(nameof(Bypass));
            }
        }

        public double GainDb
        {
            get => gainDb;
            set
            {
                gainDb = value;
                if (volume != null) volume.Volume = DbToLinear(value);
                OnPropertyChanged(nameof(GainDb));
            }
        }

        private void Load()
        {
            Stop();
            DisposePlugin();

            if (selectedEffect == null)
            {
                Status = "Pick an effect first.";
                return;
            }

            try
            {
                module = Vst3Module.Load(selectedEffect.Module.Path);
                plugin = module.CreatePlugin(selectedEffect.Class, SampleRate, MaxBlockSize);

                pluginView = plugin.CreateView();
                EditorHost = pluginView != null ? new Vst3EditorHost(pluginView) : null;

                // Surface the plug-in's factory programs, if any. Set the index field directly so
                // reflecting the current program doesn't fire a redundant program change.
                Programs = plugin.ActiveProgramList is { } list ? list.Programs : Array.Empty<string>();
                selectedProgramIndex = plugin.CurrentProgram;
                OnPropertyChanged(nameof(SelectedProgramIndex));

                isLoaded = true;
                Status = pluginView != null
                    ? $"Loaded {selectedEffect.Display} ({plugin.InputChannelCount}-in / {plugin.OutputChannelCount}-out). Pick a sample and press Play."
                    : $"Loaded {selectedEffect.Display} — this plug-in has no editor GUI. Pick a sample and press Play.";
            }
            catch (Exception ex)
            {
                Status = $"Load failed: {ex.Message}";
                DisposePlugin();
            }
        }

        private void Play()
        {
            if (!isLoaded || plugin == null)
            {
                Status = "Load a plug-in first.";
                return;
            }
            if (player != null)
            {
                return; // already playing
            }
            try
            {
                // Build the source chain from whichever sample is currently selected. Sample is
                // re-read on every Play so the dropdown picks fresh without re-loading the plug-in.
                var samplePath = SamplePath(selectedSample);
                if (!File.Exists(samplePath))
                {
                    Status = $"Sample not found: {samplePath}";
                    return;
                }

                float[] buffer;
                WaveFormat sourceFormat;
                using (var reader = new WaveFileReader(samplePath))
                {
                    if (reader.WaveFormat.SampleRate != SampleRate)
                    {
                        Status = $"Sample is {reader.WaveFormat.SampleRate} Hz but the plug-in is fixed at {SampleRate} Hz. Pick another sample.";
                        return;
                    }
                    var adapted = AdaptChannels(reader.ToSampleProvider(), plugin.InputChannelCount);
                    sourceFormat = adapted.WaveFormat;
                    buffer = ReadAll(adapted);
                }

                var looped = new LoopedSampleProvider(buffer, sourceFormat, TimeSpan.FromSeconds(1));
                effect = new BypassableVst3SampleProvider(looped, plugin) { Bypass = bypass };
                volume = new VolumeSampleProvider(effect) { Volume = DbToLinear(gainDb) };

                player = new WasapiPlayerBuilder().Build();
                player.PlaybackStopped += OnPlaybackStopped;
                player.Init(volume.ToWaveProvider());
                player.Play();
                Status = $"Playing '{selectedSample}'. Use the host Bypass / Gain controls or the plug-in's own editor. Stop, change sample, and Play to switch.";
            }
            catch (Exception ex)
            {
                Status = $"Playback failed: {ex.Message}";
                player?.Dispose();
                player = null;
                effect = null;
                volume = null;
            }
        }

        private void SavePreset()
        {
            if (!isLoaded || plugin == null)
            {
                Status = "Load a plug-in first.";
                return;
            }
            var dialog = new SaveFileDialog
            {
                Filter = "VST 3 preset (*.vstpreset)|*.vstpreset",
                DefaultExt = ".vstpreset",
                FileName = selectedEffect?.Display ?? "preset",
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                plugin.SavePreset(dialog.FileName);
                Status = $"Saved preset to {Path.GetFileName(dialog.FileName)}.";
            }
            catch (Exception ex)
            {
                Status = $"Save preset failed: {ex.Message}";
            }
        }

        private void LoadPreset()
        {
            if (!isLoaded || plugin == null)
            {
                Status = "Load a plug-in first.";
                return;
            }
            var dialog = new OpenFileDialog
            {
                Filter = "VST 3 preset (*.vstpreset)|*.vstpreset",
                DefaultExt = ".vstpreset",
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                plugin.LoadPreset(dialog.FileName);
                Status = $"Loaded preset {Path.GetFileName(dialog.FileName)}.";
            }
            catch (Exception ex)
            {
                Status = $"Load preset failed: {ex.Message}";
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            // WasapiPlayer raises this (on the captured UI context) if the render loop threw — turns
            // an otherwise-silent audio-thread failure into something visible.
            if (e.Exception != null)
            {
                Status = $"Playback stopped on error: {e.Exception.Message}";
            }
        }

        private void Stop()
        {
            if (player != null)
            {
                player.PlaybackStopped -= OnPlaybackStopped;
                player.Dispose(); // blocks until the playback thread exits — safe to touch the plug-in after
                player = null;
                // Drop the chain so a subsequent Play picks the current sample fresh; the plug-in
                // (and editor) stay alive so the user keeps their tweaks.
                effect = null;
                volume = null;
                Status = "Stopped.";
            }
        }

        public void Dispose()
        {
            disposed = true;
            Stop();
            DisposePlugin();
        }

        private void Rescan()
        {
            Effects.Clear();
            SelectedEffect = null;
            RescanCommand.IsEnabled = false;
            Status = "Rescanning installed VST 3 plug-ins…";
            _ = LoadEffectsAsync(Vst3InstalledPlugins.RescanAsync());
        }

        private async System.Threading.Tasks.Task LoadEffectsAsync(
            System.Threading.Tasks.Task<IReadOnlyList<Vst3InstalledPlugin>> source)
        {
            IReadOnlyList<Vst3InstalledPlugin> effects;
            try
            {
                var all = await source.ConfigureAwait(true);
                effects = all.Where(p => p.Class.IsEffect).ToList();
            }
            catch (Exception ex)
            {
                if (disposed) return;
                Status = "Plug-in scan failed: " + ex.Message;
                RescanCommand.IsEnabled = true;
                return;
            }
            if (disposed) return;
            foreach (var e in effects) Effects.Add(e);
            RescanCommand.IsEnabled = true;
            Status = effects.Count == 0
                ? "No VST 3 effect plug-ins found in the standard install folders."
                : "Pick an effect and a sample, then click Load.";
        }

        private void DisposePlugin()
        {
            EditorHost?.Dispose();
            EditorHost = null;
            pluginView?.Dispose();
            pluginView = null;
            effect = null;
            volume = null;
            plugin?.Dispose();
            plugin = null;
            module?.Dispose();
            module = null;
            Programs = Array.Empty<string>();
            selectedProgramIndex = -1;
            OnPropertyChanged(nameof(SelectedProgramIndex));
            isLoaded = false;
        }

        private static string SamplePath(string sample)
            => Path.Combine(AppContext.BaseDirectory, "Samples", $"{sample}-trimmed.wav");

        private static ISampleProvider AdaptChannels(ISampleProvider source, int targetChannels)
        {
            if (source.WaveFormat.Channels == targetChannels) return source;
            return targetChannels switch
            {
                1 => source.ToMono(),
                2 => source.ToStereo(),
                _ => throw new NotSupportedException($"Cannot adapt to {targetChannels} channels."),
            };
        }

        private static float[] ReadAll(ISampleProvider source)
        {
            var samples = new List<float>();
            var chunk = new float[source.WaveFormat.SampleRate * source.WaveFormat.Channels];
            int read;
            while ((read = source.Read(chunk.AsSpan())) > 0)
            {
                samples.AddRange(chunk.AsSpan(0, read).ToArray());
            }
            return samples.ToArray();
        }

        private static float DbToLinear(double db) => (float)Math.Pow(10.0, db / 20.0);
    }
}
