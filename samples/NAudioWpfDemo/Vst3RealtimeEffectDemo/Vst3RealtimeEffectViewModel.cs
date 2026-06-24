using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Vst3;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.ViewModel;
using NAudioWpfDemo.Vst3Shared;

namespace NAudioWpfDemo.Vst3RealtimeEffectDemo;

/// <summary>
/// Drives the realtime VST 3 effect chain demo. Modelled on the managed-effects
/// <c>RealtimeEffectsViewModel</c> shape (ASIO duplex, prewarm + atomic chain swap,
/// monitoring toggle, feedback auto-mute, file render path), with the chain elements
/// swapped from <c>IAudioEffect</c> to live <see cref="Vst3Plugin"/> instances so each
/// slot keeps its native editor, state, and parameter automation queues. Per the
/// Phase 8 plan in <c>Docs/Architecture/Vst3Hosting.md</c>.
/// </summary>
class Vst3RealtimeEffectViewModel : ViewModelBase, IDisposable
{
    // Match the existing Vst3HostDemo: leave enough headroom for ASIO buffers up to 2048
    // frames; plug-ins that need more will block-loop internally.
    private const int MaxBlockSize = 2048;

    private readonly Vst3RealtimeAudioEngine engine = new Vst3RealtimeAudioEngine();
    private readonly DispatcherTimer timer;
    private readonly Dictionary<string, Vst3Module> loadedModules = new(StringComparer.Ordinal);

    private string selectedDriver;
    private int inputChannelsIndex = DemoSettings.LastInputChannelsIndex;
    private int inputChannelOffset = DemoSettings.LastInputChannelOffset;
    private int selectedEffectIndex;
    private bool monitoring;
    private double outputLevel;
    private string status;
    private string inputFilePath;
    private double masterVolumeDb;
    private bool disposed;

    public Vst3RealtimeEffectViewModel()
    {
        Drivers = new ObservableCollection<string>(SafeListDrivers());
        if (Drivers.Count > 0)
        {
            var remembered = DemoSettings.LastAsioDriver;
            selectedDriver = !string.IsNullOrEmpty(remembered) && Drivers.Contains(remembered)
                ? remembered : Drivers[0];
        }

        AvailableEffects = new ObservableCollection<Vst3InstalledPlugin>();
        Chain = new ObservableCollection<Vst3PluginSlotViewModel>();

        StartCommand = new DelegateCommand(Start) { IsEnabled = false };
        StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
        AddEffectCommand = new DelegateCommand(AddEffect) { IsEnabled = false };
        ChooseInputFileCommand = new DelegateCommand(ChooseInputFile);
        RenderCommand = new DelegateCommand(RenderToFile) { IsEnabled = false };
        RescanCommand = new DelegateCommand(Rescan) { IsEnabled = false };

        timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        timer.Tick += OnTick;
        timer.Start();

        // Plug-in catalogue is cached process-wide and on disk (see Vst3InstalledPlugins);
        // first-launch scan runs on a background thread so the panel comes up immediately.
        status = "Loading installed VST 3 effects…";
        _ = LoadEffectsAsync(Vst3InstalledPlugins.GetAsync());
    }

    public ObservableCollection<string> Drivers { get; }
    public ObservableCollection<Vst3InstalledPlugin> AvailableEffects { get; }
    public ObservableCollection<Vst3PluginSlotViewModel> Chain { get; }

    public string SelectedDriver
    {
        get => selectedDriver;
        set
        {
            selectedDriver = value;
            if (!string.IsNullOrEmpty(value)) DemoSettings.LastAsioDriver = value;
            OnPropertyChanged(nameof(SelectedDriver));
        }
    }

    public int InputChannelsIndex
    {
        get => inputChannelsIndex;
        set
        {
            inputChannelsIndex = value;
            DemoSettings.LastInputChannelsIndex = value;
            OnPropertyChanged(nameof(InputChannelsIndex));
        }
    }

    /// <summary>First ASIO input channel to use (1-based).</summary>
    public int InputChannelOffset
    {
        get => inputChannelOffset;
        set
        {
            inputChannelOffset = value < 1 ? 1 : value;
            DemoSettings.LastInputChannelOffset = inputChannelOffset;
            OnPropertyChanged(nameof(InputChannelOffset));
        }
    }

    public int SelectedEffectIndex
    {
        get => selectedEffectIndex;
        set { selectedEffectIndex = value; OnPropertyChanged(nameof(SelectedEffectIndex)); }
    }

    public bool Monitoring
    {
        get => monitoring;
        set
        {
            monitoring = value;
            engine.Muted = !value;
            OnPropertyChanged(nameof(Monitoring));
        }
    }

    public double OutputLevel
    {
        get => outputLevel;
        private set { outputLevel = value; OnPropertyChanged(nameof(OutputLevel)); }
    }

    public string Status
    {
        get => status;
        private set { status = value; OnPropertyChanged(nameof(Status)); }
    }

    public string InputFilePath
    {
        get => inputFilePath;
        private set { inputFilePath = value; OnPropertyChanged(nameof(InputFilePath)); }
    }

    /// <summary>Master output volume in dB (0 dB = unity, lower attenuates). Applied
    /// post-chain in the audio thread — useful for taming hot effects like guitar amp sims.</summary>
    public double MasterVolumeDb
    {
        get => masterVolumeDb;
        set
        {
            masterVolumeDb = value;
            engine.MasterGain = (float)Math.Pow(10.0, value / 20.0);
            OnPropertyChanged(nameof(MasterVolumeDb));
        }
    }

    public DelegateCommand StartCommand { get; }
    public DelegateCommand StopCommand { get; }
    public DelegateCommand AddEffectCommand { get; }
    public DelegateCommand ChooseInputFileCommand { get; }
    public DelegateCommand RenderCommand { get; }
    public DelegateCommand RescanCommand { get; }

    private void AddEffect()
    {
        if (selectedEffectIndex < 0 || selectedEffectIndex >= AvailableEffects.Count) return;
        var listing = AvailableEffects[selectedEffectIndex];
        var slot = new Vst3PluginSlotViewModel(listing.Module, listing.Class, Remove, MoveUp, MoveDown);
        Chain.Add(slot);

        if (engine.IsRunning)
        {
            try
            {
                var module = LoadModule(listing.Module);
                slot.GoLive(module, engine.SampleRate, EngineBlockSize());
                RebuildLiveChain();
            }
            catch (Exception ex)
            {
                Chain.Remove(slot);
                slot.Dispose();
                Status = $"Add failed: {ex.Message}";
            }
        }
    }

    private void Remove(Vst3PluginSlotViewModel slot)
    {
        var index = Chain.IndexOf(slot);
        if (index < 0) return;
        Chain.RemoveAt(index);
        if (engine.IsRunning)
        {
            RebuildLiveChain();
            Vst3RealtimeAudioEngine.WaitForChainQuiesce();
        }
        slot.Dispose();
    }

    private void MoveUp(Vst3PluginSlotViewModel slot)
    {
        var i = Chain.IndexOf(slot);
        if (i > 0)
        {
            Chain.Move(i, i - 1);
            if (engine.IsRunning) RebuildLiveChain();
        }
    }

    private void MoveDown(Vst3PluginSlotViewModel slot)
    {
        var i = Chain.IndexOf(slot);
        if (i >= 0 && i < Chain.Count - 1)
        {
            Chain.Move(i, i + 1);
            if (engine.IsRunning) RebuildLiveChain();
        }
    }

    /// <summary>
    /// Rebuilds the chain array from <see cref="Chain"/> (using each slot's existing live
    /// instance) and publishes it to the engine atomically. Use after a reorder / add /
    /// remove that does not change which plug-ins are alive — for that, instantiate first.
    /// </summary>
    private void RebuildLiveChain()
    {
        var array = new Vst3ChainSlot[Chain.Count];
        for (var i = 0; i < Chain.Count; i++) array[i] = Chain[i].LiveSlot;
        engine.SetChain(array);
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(selectedDriver)) return;
        try
        {
            engine.Start(selectedDriver, inputChannelsIndex == 0 ? 1 : 2, inputChannelOffset - 1);
            // Bring all chain slots live at the engine's negotiated sample rate / buffer size.
            foreach (var slot in Chain)
            {
                var module = LoadModule(slot.ModuleInfo);
                slot.GoLive(module, engine.SampleRate, EngineBlockSize());
            }
            RebuildLiveChain();

            Monitoring = false;
            Status = $"Running on '{selectedDriver}' at {engine.SampleRate} Hz, " +
                     $"{engine.FramesPerBuffer} frames/buffer. Output muted — enable monitoring.";
            StartCommand.IsEnabled = false;
            StopCommand.IsEnabled = true;
            RenderCommand.IsEnabled = false; // file render shares the chain
        }
        catch (Exception ex)
        {
            Status = "Failed to start: " + ex.Message;
            // Best-effort cleanup of any partially-lived slots.
            foreach (var slot in Chain) slot.GoOffline();
            try { engine.Stop(); } catch { /* already stopped */ }
        }
    }

    private void Stop()
    {
        engine.Stop();
        foreach (var slot in Chain) slot.GoOffline();
        Monitoring = false;
        OutputLevel = 0;
        StartCommand.IsEnabled = Drivers.Count > 0;
        StopCommand.IsEnabled = false;
        RenderCommand.IsEnabled = !string.IsNullOrEmpty(inputFilePath);
        Status = "Stopped.";
    }

    private void ChooseInputFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            InputFilePath = dialog.FileName;
            RenderCommand.IsEnabled = !engine.IsRunning;
            Status = "Input file selected.";
        }
    }

    private void RenderToFile()
    {
        if (engine.IsRunning)
        {
            Status = "Stop ASIO first — file render uses fresh plug-in instances at the file's sample rate.";
            return;
        }
        if (Chain.Count == 0)
        {
            Status = "Add at least one effect to the chain first.";
            return;
        }
        var saveDialog = new SaveFileDialog { Filter = "WAV file (*.wav)|*.wav" };
        if (saveDialog.ShowDialog() != true) return;

        var temporaries = new List<Vst3Plugin>();
        try
        {
            using var reader = new AudioFileReader(inputFilePath);
            var inputFormat = reader.WaveFormat;
            ISampleProvider tail = reader;

            // Instantiate each non-bypassed slot's plug-in fresh at the file's sample rate,
            // restore its saved state, and stack them as ISampleProviders.
            foreach (var slot in Chain)
            {
                if (slot.Bypass) continue;
                var module = LoadModule(slot.ModuleInfo);
                var plugin = module.CreatePlugin(slot.ClassInfo, inputFormat.SampleRate, MaxBlockSize);
                temporaries.Add(plugin);
                if (slot.SavedState != null)
                {
                    try { plugin.LoadState(slot.SavedState); } catch { /* tolerate */ }
                }
                var adapted = AdaptChannels(tail, plugin.InputChannelCount);
                tail = new Vst3EffectSampleProvider(adapted, plugin);
            }

            using var writer = new WaveFileWriter(saveDialog.FileName, tail.WaveFormat);
            var buffer = new float[tail.WaveFormat.SampleRate * tail.WaveFormat.Channels];
            int read;
            while ((read = tail.Read(buffer.AsSpan())) > 0)
            {
                writer.WriteSamples(buffer, 0, read);
            }
            Status = $"Rendered to {saveDialog.FileName}";
        }
        catch (Exception ex)
        {
            Status = "Render failed: " + ex.Message;
        }
        finally
        {
            foreach (var p in temporaries) p.Dispose();
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        OutputLevel = engine.OutputLevel;
        if (engine.ConsumeAutoMuted())
        {
            monitoring = false;
            OnPropertyChanged(nameof(Monitoring));
            Status = "Feedback detected — output auto-muted. Use headphones, then re-enable monitoring.";
        }
    }

    /// <summary>
    /// Loads (or returns the cached) <see cref="Vst3Module"/> for the given info. Modules are
    /// retained for the lifetime of the viewmodel — each plug-in instance ties back to its
    /// loaded module DLL, and unloading would break still-living instances elsewhere.
    /// </summary>
    private Vst3Module LoadModule(Vst3ModuleInfo info)
    {
        if (loadedModules.TryGetValue(info.Path, out var existing)) return existing;
        var module = Vst3Module.Load(info.Path);
        loadedModules[info.Path] = module;
        return module;
    }

    private int EngineBlockSize() => Math.Max(MaxBlockSize, engine.FramesPerBuffer);

    private static IEnumerable<string> SafeListDrivers()
    {
        try { return AsioDevice.GetDriverNames(); }
        catch { return Array.Empty<string>(); }
    }

    private void Rescan()
    {
        AvailableEffects.Clear();
        RescanCommand.IsEnabled = false;
        AddEffectCommand.IsEnabled = false;
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
        foreach (var e in effects) AvailableEffects.Add(e);
        AddEffectCommand.IsEnabled = effects.Count > 0;
        StartCommand.IsEnabled = Drivers.Count > 0;
        RescanCommand.IsEnabled = true;
        Status = effects.Count == 0
            ? "No VST 3 effect plug-ins found in the standard install folders."
            : Drivers.Count > 0
                ? "Pick effects, press Start, then enable monitoring. Or render a file offline."
                : "No ASIO drivers found — file render still works.";
    }

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

    public void Dispose()
    {
        disposed = true;
        timer.Stop();
        Stop();
        foreach (var slot in Chain) slot.Dispose();
        foreach (var module in loadedModules.Values) module.Dispose();
        loadedModules.Clear();
        engine.Dispose();
    }
}
