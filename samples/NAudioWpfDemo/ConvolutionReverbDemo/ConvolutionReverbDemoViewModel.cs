using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.ConvolutionReverbDemo;

/// <summary>
/// Convolution-reverb test bench: play an input file live through the selected IR
/// (reporting DSP load), or render it offline through every IR in a folder (one at
/// a time or batched), with IR auto-resample to the input's rate and
/// peak-normalisation to -3 dBFS. Renders report Nx-real-time and added tail
/// duration, and land in a temp folder browsable from the panel.
/// </summary>
internal class ConvolutionReverbDemoViewModel : ViewModelBase, IDisposable
{
    private const float TargetPeakDbFs = -3f;
    private static readonly float TargetPeakLinear = MathF.Pow(10f, TargetPeakDbFs / 20f);

    private string inputFilePath;
    private string irFolderPath;
    private string selectedIr;
    private string selectedRender;
    private string status = "Pick an input file and a folder of impulse responses.";
    private bool busy;
    private double mix = 0.3;
    private bool sumToMono;
    private bool zeroLatency = true;

    // live playback; liveGeneration invalidates callbacks from a chain that has since been stopped
    private readonly DispatcherTimer loadTimer;
    private IWavePlayer player;
    private LiveReverbProvider live;
    private int liveGeneration;
    private bool liveStarting;
    private TimeSpan pendingStartAt;

    public ConvolutionReverbDemoViewModel()
    {
        OutputFolder = Path.Combine(Path.GetTempPath(), "NAudioWpfDemo", "ConvolutionReverb");
        Directory.CreateDirectory(OutputFolder);

        Irs = new ObservableCollection<string>();
        Renders = new ObservableCollection<string>();
        RefreshRenders();

        ChooseInputCommand = new DelegateCommand(ChooseInput);
        ChooseIrFolderCommand = new DelegateCommand(ChooseIrFolder);
        ProcessSelectedCommand = new DelegateCommand(() => _ = ProcessAsync(false));
        ProcessAllCommand = new DelegateCommand(() => _ = ProcessAsync(true));
        PlayLiveCommand = new DelegateCommand(() => _ = StartLiveAsync(TimeSpan.Zero));
        StopLiveCommand = new DelegateCommand(() => StopLive("Stopped."));
        PlayRenderCommand = new DelegateCommand(PlayRender);
        DeleteRenderCommand = new DelegateCommand(DeleteRender);
        OpenOutputFolderCommand = new DelegateCommand(() => ShellExecute(OutputFolder));

        loadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        loadTimer.Tick += (s, e) => UpdateLiveStatus();

        // pick up where the last session left off, if those files are still there
        if (File.Exists(DemoSettings.LastConvolutionInputFile))
            InputFilePath = DemoSettings.LastConvolutionInputFile;
        if (Directory.Exists(DemoSettings.LastConvolutionIrFolder))
            LoadIrFolder(DemoSettings.LastConvolutionIrFolder, DemoSettings.LastConvolutionIr);

        UpdateCommands();
    }

    /// <summary>Wet/dry mix, 0..1. Applies live and to renders.</summary>
    public double Mix
    {
        get => mix;
        set
        {
            mix = value;
            OnPropertyChanged(nameof(Mix));
            OnPropertyChanged(nameof(MixText));
            if (live != null) live.Effect.Mix = (float)value;
        }
    }

    public string MixText => $"{Mix:P0} wet";

    /// <summary>
    /// Feeds L+R to both convolvers, the usual way to drive a mono-in/stereo-out
    /// capture (e.g. of a hardware reverb). Applies live and to renders.
    /// </summary>
    public bool SumToMono
    {
        get => sumToMono;
        set
        {
            sumToMono = value;
            OnPropertyChanged(nameof(SumToMono));
            if (live != null) live.SumToMono = value;
        }
    }

    /// <summary>
    /// Applies the IR head directly so the wet signal has no latency. With it off, a
    /// partial mix blends the dry signal against a wet signal one partition late.
    /// </summary>
    public bool ZeroLatency
    {
        get => zeroLatency;
        set
        {
            zeroLatency = value;
            OnPropertyChanged(nameof(ZeroLatency));
            RestartLiveIfPlaying();
        }
    }

    public string OutputFolder { get; }
    public ObservableCollection<string> Irs { get; }
    public ObservableCollection<string> Renders { get; }

    public string InputFilePath
    {
        get => inputFilePath;
        private set { inputFilePath = value; OnPropertyChanged(nameof(InputFilePath)); UpdateCommands(); }
    }
    public string IrFolderPath
    {
        get => irFolderPath;
        private set { irFolderPath = value; OnPropertyChanged(nameof(IrFolderPath)); }
    }
    public string SelectedIr
    {
        get => selectedIr;
        set
        {
            selectedIr = value;
            OnPropertyChanged(nameof(SelectedIr));
            if (value != null)
                DemoSettings.LastConvolutionIr = value;
            UpdateCommands();
            RestartLiveIfPlaying();
        }
    }
    public string SelectedRender
    {
        get => selectedRender;
        set { selectedRender = value; OnPropertyChanged(nameof(SelectedRender)); UpdateCommands(); }
    }
    public string Status
    {
        get => status;
        private set { status = value; OnPropertyChanged(nameof(Status)); }
    }

    public DelegateCommand ChooseInputCommand { get; }
    public DelegateCommand ChooseIrFolderCommand { get; }
    public DelegateCommand ProcessSelectedCommand { get; }
    public DelegateCommand ProcessAllCommand { get; }
    public DelegateCommand PlayLiveCommand { get; }
    public DelegateCommand StopLiveCommand { get; }
    public DelegateCommand PlayRenderCommand { get; }
    public DelegateCommand DeleteRenderCommand { get; }
    public DelegateCommand OpenOutputFolderCommand { get; }

    private void ChooseInput()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*",
            InitialDirectory = Path.GetDirectoryName(InputFilePath) ?? ""
        };
        if (dialog.ShowDialog() == true)
        {
            StopLive("Stopped.");
            InputFilePath = dialog.FileName;
            DemoSettings.LastConvolutionInputFile = dialog.FileName;
        }
    }

    private void ChooseIrFolder()
    {
        var dialog = new OpenFolderDialog { InitialDirectory = IrFolderPath ?? "" };
        if (dialog.ShowDialog() != true)
            return;
        StopLive("Stopped.");
        DemoSettings.LastConvolutionIrFolder = dialog.FolderName;
        LoadIrFolder(dialog.FolderName, null);
    }

    private void LoadIrFolder(string folder, string irToSelect)
    {
        IrFolderPath = folder;
        Irs.Clear();
        foreach (var f in Directory.EnumerateFiles(folder, "*.wav").OrderBy(f => f))
            Irs.Add(Path.GetFileName(f));
        SelectedIr = Irs.Contains(irToSelect) ? irToSelect : Irs.FirstOrDefault();
        UpdateCommands();
    }

    private async Task ProcessAsync(bool all)
    {
        if (busy || InputFilePath == null)
            return;
        busy = true;
        UpdateCommands();
        try
        {
            var targets = all ? Irs.ToList() : new List<string> { SelectedIr };
            foreach (var irName in targets)
            {
                if (irName == null)
                    continue;
                Status = $"Processing {irName}…";
                try
                {
                    var report = await Task.Run(() => RenderOne(InputFilePath, Path.Combine(IrFolderPath, irName)));
                    Status = $"{irName}: {report}";
                    RefreshRenders();
                }
                catch (Exception ex)
                {
                    Status = $"Failed on {irName}: {ex.Message}";
                }
            }
        }
        finally
        {
            busy = false;
            UpdateCommands();
        }
    }

    private string RenderOne(string inputPath, string irPath)
    {
        using var reader = new AudioFileReader(inputPath);
        var inputRate = reader.WaveFormat.SampleRate;
        var inputChannels = reader.WaveFormat.Channels;

        var ir = LoadAndPrepareIr(irPath, inputRate);
        var irChannels = ir.Length;
        var irLength = ir[0].Length;
        var outputChannels = Math.Max(inputChannels, irChannels);
        if (outputChannels > 2)
            throw new NotSupportedException("Only mono and stereo inputs/IRs are supported by this demo.");

        ISampleProvider source = reader;
        if (inputChannels == 1 && outputChannels == 2)
            source = new MonoToStereoSampleProvider(reader);

        var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(inputRate, outputChannels);
        var mono = SumToMono;
        var effect = CreateEffect(outputFormat, ir);

        var latency = effect.LatencySamples;
        var outName = $"{Path.GetFileNameWithoutExtension(inputPath)}_" +
                      $"{Path.GetFileNameWithoutExtension(irPath)}_" +
                      $"mix{Mix * 100:0}{(mono ? "_mono" : "")}{(effect.ZeroLatency ? "" : "_latency")}_" +
                      $"{DateTime.Now:HHmmss}.wav";
        var outPath = Path.Combine(OutputFolder, outName);

        var stopwatch = Stopwatch.StartNew();
        long inputFrames = 0;
        using (var writer = new WaveFileWriter(outPath, outputFormat))
        {
            const int blockFrames = 4096;
            var buffer = new float[blockFrames * outputChannels];
            long framesProcessed = 0;

            while (true)
            {
                var read = source.Read(buffer);
                if (read == 0)
                    break;
                inputFrames += read / outputChannels;
                if (mono)
                    SumStereoToMono(buffer.AsSpan(0, read), outputChannels);
                effect.Process(buffer.AsSpan(0, read));
                WriteSkippingLatency(writer, buffer, read, outputChannels, ref framesProcessed, latency);
            }

            // Flush the tail: feed (irLength + latency) silent frames so the full
            // tail emerges after the initial latency-skip window.
            var tailFramesNeeded = irLength + latency;
            long tailFramesFed = 0;
            while (tailFramesFed < tailFramesNeeded)
            {
                var framesThisRound = (int)Math.Min(blockFrames, tailFramesNeeded - tailFramesFed);
                var samples = framesThisRound * outputChannels;
                Array.Clear(buffer, 0, samples);
                effect.Process(buffer.AsSpan(0, samples));
                WriteSkippingLatency(writer, buffer, samples, outputChannels, ref framesProcessed, latency);
                tailFramesFed += framesThisRound;
            }
        }
        stopwatch.Stop();

        var inputSeconds = inputFrames / (double)inputRate;
        var tailSeconds = irLength / (double)inputRate;
        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var nx = elapsedSeconds > 0 ? inputSeconds / elapsedSeconds : double.PositiveInfinity;
        return $"{nx:0.#}× real-time, +{tailSeconds:0.##} s tail";
    }

    private ConvolutionReverbEffect CreateEffect(WaveFormat format, float[][] ir)
    {
        var effect = new ConvolutionReverbEffect { Mix = (float)Mix, ZeroLatency = ZeroLatency };
        effect.Configure(format);
        effect.SetImpulseResponse(ir);
        return effect;
    }

    /// <summary>Replaces each stereo frame with the mean of its two channels.</summary>
    private static void SumStereoToMono(Span<float> buffer, int channels)
    {
        if (channels != 2)
            return;
        for (var i = 0; i + 1 < buffer.Length; i += 2)
        {
            var m = 0.5f * (buffer[i] + buffer[i + 1]);
            buffer[i] = m;
            buffer[i + 1] = m;
        }
    }

    private void RestartLiveIfPlaying()
    {
        if (live != null)
            _ = StartLiveAsync(live.Reader.CurrentTime);
        else if (liveStarting)
            _ = StartLiveAsync(pendingStartAt);
    }

    private async Task StartLiveAsync(TimeSpan startAt)
    {
        if (InputFilePath == null || SelectedIr == null)
            return;
        StopLive(null);
        var generation = liveGeneration;
        var inputPath = InputFilePath;
        var irName = SelectedIr;
        var irPath = Path.Combine(IrFolderPath, irName);
        liveStarting = true;
        pendingStartAt = startAt;
        UpdateCommands();
        Status = $"Loading {irName}…";

        LiveReverbProvider chain;
        try
        {
            chain = await Task.Run(() => BuildLiveChain(inputPath, irPath, irName, startAt));
        }
        catch (Exception ex)
        {
            if (generation == liveGeneration)
            {
                liveStarting = false;
                Status = $"Could not play {irName}: {ex.Message}";
                UpdateCommands();
            }
            return;
        }
        if (generation != liveGeneration)
        {
            // stopped or restarted while the IR was loading
            chain.Reader.Dispose();
            return;
        }

        liveStarting = false;
        live = chain;
        try
        {
            player = SamplerPlayback.Create(chain, null);
        }
        catch (Exception ex)
        {
            StopLive($"Could not open audio output: {ex.Message}");
            return;
        }
        player.PlaybackStopped += (s, e) => Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (generation == liveGeneration)
                StopLive(e.Exception != null ? $"Playback error: {e.Exception.Message}" : "Finished.");
        }));
        player.Play();
        loadTimer.Start();
        UpdateLiveStatus();
        UpdateCommands();
    }

    private LiveReverbProvider BuildLiveChain(string inputPath, string irPath, string irName, TimeSpan startAt)
    {
        var reader = new AudioFileReader(inputPath);
        try
        {
            var ir = LoadAndPrepareIr(irPath, reader.WaveFormat.SampleRate);
            var outputChannels = Math.Max(reader.WaveFormat.Channels, ir.Length);
            if (outputChannels > 2)
                throw new NotSupportedException("Only mono and stereo inputs/IRs are supported by this demo.");
            if (startAt > TimeSpan.Zero && startAt < reader.TotalTime)
                reader.CurrentTime = startAt;

            ISampleProvider source = reader;
            if (reader.WaveFormat.Channels == 1 && outputChannels == 2)
                source = new MonoToStereoSampleProvider(reader);
            var effect = CreateEffect(WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, outputChannels), ir);
            return new LiveReverbProvider(reader, source, effect, irName, ir[0].Length) { SumToMono = SumToMono };
        }
        catch
        {
            reader.Dispose();
            throw;
        }
    }

    private void StopLive(string message)
    {
        liveGeneration++;
        liveStarting = false;
        loadTimer.Stop();
        player?.Stop();
        player?.Dispose();
        player = null;
        live?.Reader.Dispose();
        live = null;
        if (message != null)
            Status = message;
        UpdateCommands();
    }

    private void UpdateLiveStatus()
    {
        if (live == null)
            return;
        var (average, peak) = live.TakeLoad();
        var position = live.InTail ? "reverb tail" : $@"{live.Reader.CurrentTime:m\:ss} / {live.Reader.TotalTime:m\:ss}";
        Status = $"Live: {live.IrName} ({position}). DSP load {average:P0} average, {peak:P0} peak per buffer" +
                 (peak >= 1 ? " - can't keep up, expect glitches" : "");
    }

    public void Dispose() => StopLive(null);

    /// <summary>
    /// Live playback chain: optional mono sum, the reverb, timing of the reverb against
    /// the audio it produced (DSP load), and silence after the source ends so the tail
    /// rings out instead of being cut off.
    /// </summary>
    private sealed class LiveReverbProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly int channels;
        private readonly int sampleRate;
        private long tailSamplesRemaining;
        private bool sourceEnded;
        private double loadSum;
        private int loadCount;
        private double loadPeak;
        private readonly object loadLock = new();

        public LiveReverbProvider(AudioFileReader reader, ISampleProvider source, ConvolutionReverbEffect effect,
            string irName, int irFrames)
        {
            Reader = reader;
            this.source = source;
            Effect = effect;
            IrName = irName;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
            channels = WaveFormat.Channels;
            sampleRate = WaveFormat.SampleRate;
            tailSamplesRemaining = (long)irFrames * channels;
        }

        public AudioFileReader Reader { get; }
        public ConvolutionReverbEffect Effect { get; }
        public string IrName { get; }
        public volatile bool SumToMono;
        public bool InTail => sourceEnded;
        public WaveFormat WaveFormat { get; }

        public int Read(Span<float> buffer)
        {
            var read = sourceEnded ? 0 : source.Read(buffer);
            if (read < buffer.Length)
            {
                sourceEnded = true;
                var pad = (int)Math.Min(buffer.Length - read, tailSamplesRemaining);
                pad -= pad % channels;
                buffer.Slice(read, pad).Clear();
                read += pad;
                tailSamplesRemaining -= pad;
            }
            if (read == 0)
                return 0;

            var block = buffer.Slice(0, read);
            var start = Stopwatch.GetTimestamp();
            if (SumToMono)
                SumStereoToMono(block, channels);
            Effect.Process(block);
            var load = Stopwatch.GetElapsedTime(start).TotalSeconds / ((double)read / channels / sampleRate);
            lock (loadLock)
            {
                loadSum += load;
                loadCount++;
                loadPeak = Math.Max(loadPeak, load);
            }
            return read;
        }

        /// <summary>Average and peak load since the last call: reverb time over the audio time it produced.</summary>
        public (double Average, double Peak) TakeLoad()
        {
            lock (loadLock)
            {
                var result = (loadCount > 0 ? loadSum / loadCount : 0, loadPeak);
                loadSum = 0;
                loadCount = 0;
                loadPeak = 0;
                return result;
            }
        }
    }

    private static void WriteSkippingLatency(WaveFileWriter writer, float[] buffer, int samples,
        int channels, ref long framesProcessed, int latencyFrames)
    {
        var framesInBuffer = samples / channels;
        var skipFrames = (int)Math.Min(framesInBuffer, Math.Max(0L, latencyFrames - framesProcessed));
        framesProcessed += framesInBuffer;
        if (skipFrames < framesInBuffer)
            writer.WriteSamples(buffer, skipFrames * channels, (framesInBuffer - skipFrames) * channels);
    }

    private static float[][] LoadAndPrepareIr(string path, int targetSampleRate)
    {
        using var reader = new AudioFileReader(path);
        ISampleProvider provider = reader;
        if (reader.WaveFormat.SampleRate != targetSampleRate)
            provider = new WdlResamplingSampleProvider(provider, targetSampleRate);

        var channels = provider.WaveFormat.Channels;
        var pool = new List<float>(1 << 16);
        var temp = new float[4096];
        int read;
        while ((read = provider.Read(temp)) > 0)
            for (var i = 0; i < read; i++)
                pool.Add(temp[i]);

        var totalFrames = pool.Count / channels;
        var perChannel = new float[channels][];
        for (var ch = 0; ch < channels; ch++)
        {
            var arr = new float[totalFrames];
            for (var i = 0; i < totalFrames; i++)
                arr[i] = pool[i * channels + ch];
            perChannel[ch] = arr;
        }

        // Peak-normalise the IR to -3 dBFS across all channels (preserves stereo balance).
        var peak = 0f;
        foreach (var arr in perChannel)
            foreach (var s in arr)
                if (MathF.Abs(s) > peak)
                    peak = MathF.Abs(s);
        if (peak > 1e-9f)
        {
            var scale = TargetPeakLinear / peak;
            foreach (var arr in perChannel)
                for (var i = 0; i < arr.Length; i++)
                    arr[i] *= scale;
        }
        return perChannel;
    }

    private void RefreshRenders()
    {
        Renders.Clear();
        foreach (var f in Directory.EnumerateFiles(OutputFolder, "*.wav").OrderBy(f => f))
            Renders.Add(Path.GetFileName(f));
    }

    private void PlayRender()
    {
        if (SelectedRender != null)
            ShellExecute(Path.Combine(OutputFolder, SelectedRender));
    }

    private void DeleteRender()
    {
        if (SelectedRender == null)
            return;
        try
        {
            File.Delete(Path.Combine(OutputFolder, SelectedRender));
            Renders.Remove(SelectedRender);
            SelectedRender = Renders.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not delete render: " + ex.Message);
        }
    }

    private static void ShellExecute(string target)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo(target) { UseShellExecute = true }
        };
        p.Start();
    }

    private void UpdateCommands()
    {
        var playing = live != null || liveStarting;
        ProcessSelectedCommand.IsEnabled = !busy && !playing && InputFilePath != null && SelectedIr != null;
        ProcessAllCommand.IsEnabled = !busy && !playing && InputFilePath != null && Irs.Count > 0;
        PlayLiveCommand.IsEnabled = !busy && !playing && InputFilePath != null && SelectedIr != null;
        StopLiveCommand.IsEnabled = playing;
        PlayRenderCommand.IsEnabled = SelectedRender != null;
        DeleteRenderCommand.IsEnabled = SelectedRender != null;
    }
}
