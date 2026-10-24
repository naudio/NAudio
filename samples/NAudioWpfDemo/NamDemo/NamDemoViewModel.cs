using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Effects;
using NAudio.Nam;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.RealtimeEffectsDemo;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.NamDemo;

/// <summary>
/// Neural Amp Modeler test bench: load a .nam capture and a cabinet IR, then play a
/// guitar through them live over ASIO, or run a DI recording through them (play or
/// render to WAV). The chain is input gain → amp (NamEffect) → cabinet (CabinetEffect)
/// → output gain. Amp and cabinet sit in swappable slots, so changing the model, its
/// size or the IR takes effect on the running audio.
/// </summary>
internal class NamDemoViewModel : ViewModelBase, IDisposable
{
    private readonly RealtimeAudioEngine engine = new();
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private readonly DispatcherTimer timer;
    private readonly GainEffect inputGain = new();
    private readonly GainEffect outputGain = new();
    private readonly SwappableEffect ampSlot = new();
    private readonly SwappableEffect cabinetSlot = new();
    private readonly IAudioEffect[] effects;

    private NamModel model;
    private NamEffect amp;
    private CabinetEffect cabinet;
    private string modelPath;
    private string modelInfo = "No model loaded.";
    private IReadOnlyList<NetworkOption> networks = [];
    private NetworkOption selectedNetwork;
    private bool ampEnabled = true;
    private string irPath;
    private string irInfo = "No cabinet IR loaded: the amp's output is heard directly.";
    private bool cabinetEnabled = true;
    private double inputGainDb;
    private double outputGainDb = -6;

    private string selectedDriver;
    private int inputChannel = Math.Max(1, DemoSettings.LastInputChannelOffset);
    private bool monitoring;
    private double outputLevel;

    private string inputFilePath;
    private IWavePlayer filePlayer;
    private AudioFileReader fileReader;
    private EffectChain fileChain;

    private string status = "Load a .nam model to start.";
    private string performance = "";
    private double smoothedLoad;

    public NamDemoViewModel()
    {
        effects = [inputGain, ampSlot, cabinetSlot, outputGain];
        SetGain(inputGain, inputGainDb);
        SetGain(outputGain, outputGainDb);

        Drivers = new ObservableCollection<string>(AsioDevice.GetDriverNames());
        if (Drivers.Count > 0)
        {
            var remembered = DemoSettings.LastAsioDriver;
            selectedDriver = !string.IsNullOrEmpty(remembered) && Drivers.Contains(remembered) ? remembered : Drivers[0];
        }

        LoadModelCommand = new DelegateCommand(() => _ = LoadModelAsync());
        LoadIrCommand = new DelegateCommand(LoadIr);
        ClearIrCommand = new DelegateCommand(ClearIr) { IsEnabled = false };
        StartCommand = new DelegateCommand(Start) { IsEnabled = Drivers.Count > 0 };
        StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
        ChooseInputFileCommand = new DelegateCommand(ChooseInputFile);
        PlayFileCommand = new DelegateCommand(PlayFile) { IsEnabled = false };
        StopFileCommand = new DelegateCommand(StopFile) { IsEnabled = false };
        RenderCommand = new DelegateCommand(() => _ = RenderAsync()) { IsEnabled = false };

        timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += OnTick;
        timer.Start();
    }

    // ---- Amp model ----

    public string ModelPath
    {
        get => modelPath;
        private set { modelPath = value; OnPropertyChanged(nameof(ModelPath)); }
    }

    public string ModelInfo
    {
        get => modelInfo;
        private set { modelInfo = value; OnPropertyChanged(nameof(ModelInfo)); }
    }

    /// <summary>A network in the loaded file, as shown in the combo box.</summary>
    public sealed record NetworkOption(string Label, int Index);

    /// <summary>
    /// The networks in the loaded file (A2 files have A2-Lite and A2-Full). Each load
    /// publishes a new list rather than editing the bound one: clearing a bound
    /// collection makes the ComboBox drop its selection, after which it no longer
    /// shows the selected network even though the binding still holds it.
    /// </summary>
    public IReadOnlyList<NetworkOption> Networks
    {
        get => networks;
        private set { networks = value; OnPropertyChanged(nameof(Networks)); }
    }

    public NetworkOption SelectedNetwork
    {
        get => selectedNetwork;
        set
        {
            // While the list is being replaced the ComboBox pushes null (or a stale
            // item) back; only a pick from the current list changes the amp.
            if (value == null || ReferenceEquals(value, selectedNetwork) || !Contains(networks, value))
                return;
            selectedNetwork = value;
            OnPropertyChanged(nameof(SelectedNetwork));
            CreateAmp();
        }
    }

    private static bool Contains(IReadOnlyList<NetworkOption> options, NetworkOption option)
    {
        foreach (var o in options)
        {
            if (ReferenceEquals(o, option))
                return true;
        }
        return false;
    }

    public bool AmpEnabled
    {
        get => ampEnabled;
        set
        {
            ampEnabled = value;
            if (amp != null)
                amp.Bypass = !value;
            OnPropertyChanged(nameof(AmpEnabled));
        }
    }

    // ---- Cabinet ----

    public string IrPath
    {
        get => irPath;
        private set { irPath = value; OnPropertyChanged(nameof(IrPath)); }
    }

    public string IrInfo
    {
        get => irInfo;
        private set { irInfo = value; OnPropertyChanged(nameof(IrInfo)); }
    }

    public bool CabinetEnabled
    {
        get => cabinetEnabled;
        set
        {
            cabinetEnabled = value;
            if (cabinet != null)
                cabinet.Bypass = !value;
            OnPropertyChanged(nameof(CabinetEnabled));
        }
    }

    // ---- Levels ----

    public double InputGainDb
    {
        get => inputGainDb;
        set
        {
            inputGainDb = value;
            SetGain(inputGain, value);
            OnPropertyChanged(nameof(InputGainDb));
            OnPropertyChanged(nameof(InputGainText));
        }
    }

    public string InputGainText => $"{inputGainDb:+0.0;-0.0;0.0} dB";

    public double OutputGainDb
    {
        get => outputGainDb;
        set
        {
            outputGainDb = value;
            SetGain(outputGain, value);
            OnPropertyChanged(nameof(OutputGainDb));
            OnPropertyChanged(nameof(OutputGainText));
        }
    }

    public string OutputGainText => $"{outputGainDb:+0.0;-0.0;0.0} dB";

    // ---- Live input (ASIO) ----

    public ObservableCollection<string> Drivers { get; }

    public string SelectedDriver
    {
        get => selectedDriver;
        set
        {
            selectedDriver = value;
            if (!string.IsNullOrEmpty(value))
                DemoSettings.LastAsioDriver = value;
            OnPropertyChanged(nameof(SelectedDriver));
        }
    }

    /// <summary>The ASIO input the guitar is plugged into (1-based).</summary>
    public int InputChannel
    {
        get => inputChannel;
        set
        {
            inputChannel = value < 1 ? 1 : value;
            DemoSettings.LastInputChannelOffset = inputChannel;
            OnPropertyChanged(nameof(InputChannel));
        }
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

    // ---- DI file ----

    public string InputFilePath
    {
        get => inputFilePath;
        private set { inputFilePath = value; OnPropertyChanged(nameof(InputFilePath)); }
    }

    // ---- Status ----

    public string Status
    {
        get => status;
        private set { status = value; OnPropertyChanged(nameof(Status)); }
    }

    /// <summary>Amp CPU load and latency, refreshed while audio is running.</summary>
    public string Performance
    {
        get => performance;
        private set { performance = value; OnPropertyChanged(nameof(Performance)); }
    }

    public DelegateCommand LoadModelCommand { get; }
    public DelegateCommand LoadIrCommand { get; }
    public DelegateCommand ClearIrCommand { get; }
    public DelegateCommand StartCommand { get; }
    public DelegateCommand StopCommand { get; }
    public DelegateCommand ChooseInputFileCommand { get; }
    public DelegateCommand PlayFileCommand { get; }
    public DelegateCommand StopFileCommand { get; }
    public DelegateCommand RenderCommand { get; }

    private async Task LoadModelAsync()
    {
        var dialog = new OpenFileDialog { Filter = "NAM models (*.nam)|*.nam|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true)
            return;
        var path = dialog.FileName;
        Status = "Loading " + Path.GetFileName(path) + "…";
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var loaded = await Task.Run(() => NamModel.Load(path));
            var loadMs = stopwatch.ElapsedMilliseconds;

            model = loaded;
            ModelPath = path;
            var options = new NetworkOption[loaded.Submodels.Count];
            for (var i = 0; i < options.Length; i++)
                options[i] = new NetworkOption(Describe(loaded.Submodels[i]), i);
            Networks = options;
            ModelInfo = DescribeModel(loaded);
            // Default to the largest network, as NamEffect does. Set after the list so
            // the ComboBox finds the item in its new ItemsSource.
            selectedNetwork = options[^1];
            OnPropertyChanged(nameof(SelectedNetwork));
            CreateAmp();
            Status = $"Loaded {Path.GetFileName(path)} in {loadMs} ms.";
        }
        catch (Exception ex)
        {
            Status = "Could not load the model: " + ex.Message;
        }
        UpdateCommands();
    }

    /// <summary>Creates the amp for the selected network and swaps it into the running chain.</summary>
    private void CreateAmp()
    {
        if (model == null || selectedNetwork == null)
            return;
        var index = selectedNetwork.Index;
        // NamModel.GetSubmodel picks the first network whose MaxSize is above the size,
        // so the previous network's MaxSize selects this one.
        var size = index == 0 ? 0.0 : model.Submodels[index - 1].MaxSize;
        var newAmp = new NamEffect(model, size) { Bypass = !ampEnabled };
        ampSlot.Swap(newAmp);
        amp = newAmp;
        smoothedLoad = 0;
    }

    private void LoadIr()
    {
        var dialog = new OpenFileDialog { Filter = "Impulse responses (*.wav)|*.wav|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            var newCabinet = new CabinetEffect { MonoInput = true, Bypass = !cabinetEnabled };
            newCabinet.LoadImpulseResponse(dialog.FileName);
            cabinetSlot.Swap(newCabinet);
            cabinet = newCabinet;
            IrPath = dialog.FileName;
            UpdateIrInfo();
            ClearIrCommand.IsEnabled = true;
            Status = "Loaded cabinet IR " + Path.GetFileName(dialog.FileName) + ".";
        }
        catch (Exception ex)
        {
            Status = "Could not load the IR: " + ex.Message;
        }
    }

    private void ClearIr()
    {
        cabinetSlot.Swap(null);
        cabinet = null;
        IrPath = null;
        UpdateIrInfo();
        ClearIrCommand.IsEnabled = false;
    }

    private void UpdateIrInfo()
    {
        if (cabinet == null)
        {
            IrInfo = "No cabinet IR loaded: the amp's output is heard directly.";
            return;
        }
        // The prepared length is only known once the effect is configured for a stream.
        IrInfo = cabinet.ImpulseResponseLength > 0
            ? $"IR length after trimming: {cabinet.ImpulseResponseLength} samples. Zero latency."
            : "IR loaded. Zero latency.";
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(selectedDriver))
            return;
        StopFile();
        try
        {
            // Mono in from the chosen input; the engine copies it to both channels.
            engine.Start(selectedDriver, 1, inputChannel - 1);
            var format = WaveFormat.CreateIeeeFloatWaveFormat(engine.SampleRate, 2);
            foreach (var effect in effects)
                effect.Configure(format);
            engine.SetEffects(effects);
            Monitoring = false;
            UpdateIrInfo();
            Status = $"Running on '{selectedDriver}' at {engine.SampleRate} Hz, input {inputChannel}. " +
                     "Output is muted: enable monitoring to listen.";
            StartCommand.IsEnabled = false;
            StopCommand.IsEnabled = true;
            UpdateCommands();
        }
        catch (Exception ex)
        {
            Status = "Failed to start: " + ex.Message;
        }
    }

    private void Stop()
    {
        engine.Stop();
        engine.SetEffects([]);
        Monitoring = false;
        OutputLevel = 0;
        StartCommand.IsEnabled = Drivers.Count > 0;
        StopCommand.IsEnabled = false;
        UpdateCommands();
        Status = "Stopped.";
    }

    private void ChooseInputFile()
    {
        var dialog = new OpenFileDialog { Filter = "Audio files (*.wav;*.mp3;*.flac)|*.wav;*.mp3;*.flac|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true)
            return;
        InputFilePath = dialog.FileName;
        UpdateCommands();
        Status = "DI file selected. A dry, unprocessed guitar recording works best.";
    }

    /// <summary>The DI file as mono (the channels averaged), as NAM models take one input.</summary>
    private static ISampleProvider OpenMono(AudioFileReader reader) =>
        reader.WaveFormat.Channels == 1 ? reader : new StereoToMonoSampleProvider(reader);

    private void PlayFile()
    {
        StopFile();
        try
        {
            fileReader = new AudioFileReader(inputFilePath);
            fileChain = new EffectChain(OpenMono(fileReader)); // Add configures each effect for the file's format
            foreach (var effect in effects)
                fileChain.Add(effect);
            fileChain.Reset();
            var player = new WasapiPlayerBuilder().Build();
            filePlayer = player;
            player.Init(new MonoToStereoSampleProvider(fileChain));
            // Raised from the playback thread: tidy up on the UI thread, and only if this
            // is still the current player.
            player.PlaybackStopped += (_, _) => dispatcher.BeginInvoke(() =>
            {
                if (ReferenceEquals(filePlayer, player))
                {
                    StopFile();
                    Status = "Playback finished.";
                }
            });
            player.Play();
            UpdateIrInfo();
            Status = $"Playing {Path.GetFileName(inputFilePath)} at {fileReader.WaveFormat.SampleRate} Hz.";
        }
        catch (Exception ex)
        {
            Status = "Playback failed: " + ex.Message;
            StopFile();
        }
        UpdateCommands();
    }

    private void StopFile()
    {
        var player = filePlayer;
        filePlayer = null;
        player?.Dispose();
        fileChain = null;
        fileReader?.Dispose();
        fileReader = null;
        UpdateCommands();
    }

    private async Task RenderAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "WAV file (*.wav)|*.wav",
            FileName = Path.GetFileNameWithoutExtension(inputFilePath) + "_" + Path.GetFileNameWithoutExtension(modelPath ?? "dry") + ".wav",
        };
        if (dialog.ShowDialog() != true)
            return;
        RenderCommand.IsEnabled = false;
        PlayFileCommand.IsEnabled = false;
        StartCommand.IsEnabled = false;
        Status = "Rendering…";
        try
        {
            var input = inputFilePath;
            var output = dialog.FileName;
            var report = await Task.Run(() => Render(input, output));
            Status = $"Rendered {Path.GetFileName(output)}: {report}.";
        }
        catch (Exception ex)
        {
            Status = "Render failed: " + ex.Message;
        }
        StartCommand.IsEnabled = Drivers.Count > 0 && !engine.IsRunning;
        UpdateCommands();
    }

    private string Render(string inputPath, string outputPath)
    {
        using var reader = new AudioFileReader(inputPath);
        var chain = new EffectChain(OpenMono(reader));
        foreach (var effect in effects)
            chain.Add(effect);
        chain.Reset();
        // Drop the chain's latency from the start and feed silence at the end, so the
        // render lines up with the input.
        var latency = chain.LatencySamples;
        var sampleRate = reader.WaveFormat.SampleRate;

        var stopwatch = Stopwatch.StartNew();
        long frames = 0;
        using (var writer = new WaveFileWriter(outputPath, chain.WaveFormat))
        {
            var buffer = new float[4096];
            long skip = latency;
            int read;
            while ((read = chain.Read(buffer)) > 0)
            {
                frames += read;
                var start = (int)Math.Min(read, skip);
                skip -= start;
                writer.WriteSamples(buffer, start, read - start);
            }
            var tail = new float[latency];
            foreach (var effect in effects)
                effect.Process(tail);
            writer.WriteSamples(tail, 0, tail.Length);
        }
        var elapsed = stopwatch.Elapsed.TotalSeconds;
        var audioSeconds = frames / (double)sampleRate;
        return $"{audioSeconds:0.0} s of audio in {elapsed:0.00} s ({audioSeconds / Math.Max(elapsed, 1e-6):0}× real time)";
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (engine.IsRunning)
            OutputLevel = engine.OutputLevel;
        if (engine.ConsumeAutoMuted())
        {
            monitoring = false;
            OnPropertyChanged(nameof(Monitoring));
            Status = "Feedback detected: output auto-muted. Use headphones, then re-enable monitoring.";
        }

        var load = ampSlot.TakeLoad();
        if (load is { } l)
            smoothedLoad = smoothedLoad == 0 ? l : 0.8 * smoothedLoad + 0.2 * l;
        if (amp == null || (filePlayer == null && !engine.IsRunning))
        {
            Performance = "";
            return;
        }
        var text = new StringBuilder($"Amp CPU: {100 * smoothedLoad:0.0}% of one core");
        text.Append(amp.IsResampling
            ? $" · resampling to {amp.Submodel.SampleRate} Hz, latency {amp.LatencySamples} samples"
            : " · no added latency");
        if (cabinet != null)
            text.Append(" · cabinet IR ").Append(cabinet.ImpulseResponseLength).Append(" samples");
        Performance = text.ToString();
    }

    private void UpdateCommands()
    {
        var live = engine.IsRunning;
        var playing = filePlayer != null;
        var hasFile = !string.IsNullOrEmpty(inputFilePath);
        // The effects are shared, so only one audio path runs them at a time.
        PlayFileCommand.IsEnabled = hasFile && !live && !playing;
        StopFileCommand.IsEnabled = playing;
        RenderCommand.IsEnabled = hasFile && !live && !playing;
        StartCommand.IsEnabled = Drivers.Count > 0 && !live && !playing;
    }

    private static void SetGain(GainEffect gain, double db)
    {
        // Through the parameter, so that while the ASIO engine is running the change is
        // queued and applied on the audio thread.
        foreach (var parameter in gain.Parameters)
        {
            if (parameter.Name == "Gain")
                parameter.Value = (float)db;
        }
    }

    private static string Describe(NamSubmodel submodel) =>
        submodel.IsA2Lite ? "A2-Lite (3 channels)"
        : submodel.IsA2Full ? "A2-Full (8 channels)"
        : submodel.Architecture == "LSTM" ? $"LSTM (hidden size {submodel.Channels})"
        : $"WaveNet ({submodel.Channels} channels)";

    private static string DescribeModel(NamModel model)
    {
        var m = model.Metadata;
        var text = new StringBuilder();
        var title = string.Join(" ", new[] { m.GearMake, m.GearModel }).Trim();
        text.AppendLine(string.IsNullOrEmpty(title) ? (m.Name ?? "Unnamed capture") : title);
        if (!string.IsNullOrEmpty(m.Name) && !string.IsNullOrEmpty(title))
            text.AppendLine(m.Name);
        if (m.ModeledBy != null)
            text.AppendLine("Captured by " + m.ModeledBy);
        var details = new StringBuilder($"{model.Architecture}, file version {model.Version}");
        if (m.GearType != null)
            details.Append(", gear type ").Append(m.GearType);
        if (m.ToneType != null)
            details.Append(", tone ").Append(m.ToneType);
        text.AppendLine(details.ToString());
        var levels = new StringBuilder();
        if (model.Submodels[^1].SampleRate is { } rate)
            levels.Append($"Trained at {rate} Hz");
        if (m.Loudness is { } loudness)
            levels.Append($"{(levels.Length > 0 ? " · " : "")}loudness {loudness:0.0} dB");
        if (m.InputLevelDbu is { } dbu)
            levels.Append($"{(levels.Length > 0 ? " · " : "")}full-scale input = {dbu:0.0} dBu");
        if (levels.Length > 0)
            text.AppendLine(levels.ToString());
        if (m.GearType is "amp_cab" or "amp_pedal_cab")
            text.AppendLine("This capture includes the cabinet: you probably don't need an IR.");
        return text.ToString().TrimEnd();
    }

    public void Dispose()
    {
        timer.Stop();
        StopFile();
        engine.Dispose();
    }
}
