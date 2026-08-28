using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.WasapiRecorderDemo;

/// <summary>How the recorder gets its audio — each maps to a different builder entry point.</summary>
internal enum CaptureSource
{
    /// <summary>A specific capture endpoint (microphone, line in) via <c>WithDevice</c>.</summary>
    CaptureDevice,
    /// <summary>What a render endpoint is playing, via <c>WithLoopbackCapture</c>.</summary>
    Loopback,
    /// <summary>Follows the default capture device via <c>WithDefaultDeviceStreamRouting</c>.</summary>
    DefaultDeviceRouting,
    /// <summary>A single process's audio via <c>WithProcessLoopback</c>.</summary>
    ProcessLoopback
}

/// <summary>
/// Drives <see cref="WasapiRecorder"/> from the UI: builds one from the options on screen,
/// meters what arrives, and optionally writes it to a WAV file.
/// </summary>
/// <remarks>
/// Monitoring and recording are deliberately separate. Monitoring runs the recorder and meters
/// the incoming audio without touching the disk, which is the quickest way to check that a given
/// combination of options actually produces samples; recording adds the file writer on top and
/// can be armed while monitoring without restarting the stream.
/// </remarks>
internal class WasapiRecorderViewModel : ViewModelBase, IDisposable
{
    private const double MeterFloorDb = -60.0;

    private WasapiRecorder recorder;
    // Snapshot of recorder.WaveFormat, so the capture thread never has to touch the recorder
    // field — which the UI thread may null out from under it during teardown.
    private WaveFormat captureFormat;
    private WaveFileWriter writer;
    private readonly Lock writerLock = new();
    private readonly DispatcherTimer meterTimer = new();

    // Written by the capture thread, read and reset by the meter timer on the UI thread. Float
    // reads/writes are atomic, and a meter that occasionally misses one packet's peak doesn't
    // matter — this deliberately keeps a lock off the capture path.
    private float peakSinceLastMeterTick;

    private CaptureSource captureSource = CaptureSource.CaptureDevice;
    private MMDevice selectedDevice;
    private ProcessChoice selectedProcess;
    private int shareModeIndex;
    private int syncModeIndex;
    private int bufferMilliseconds = 100;
    private bool useLowLatency;
    private bool requireLowLatency;
    private bool useRawMode;
    private bool useCommunicationsMode;
    private bool useMmcss = true;
    private bool useCustomFormat;
    private int sampleRate = 48000;
    private int bitDepth = 32;
    private int channelCount = 2;
    private int sampleTypeIndex;

    private string status = "Idle";
    private string actualFormatText = "—";
    private string latencyText = "—";
    private string lowLatencyText;
    private double meterValue;
    private string peakText = "—";
    private float peakHold;
    private string currentFileName;
    private bool isRunning;
    private bool isWriting;
    private bool meterSupported = true;

    public RecordingsViewModel RecordingsViewModel { get; }

    public DelegateCommand MonitorCommand { get; }
    public DelegateCommand RecordCommand { get; }
    public DelegateCommand StopCommand { get; }
    public DelegateCommand ResetPeakCommand { get; }

    public WasapiRecorderViewModel()
    {
        using var enumerator = new MMDeviceEnumerator();
        CaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
        RenderDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArray();
        var defaultCapture = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        selectedDevice = CaptureDevices.FirstOrDefault(d => d.ID == defaultCapture.ID) ?? CaptureDevices.FirstOrDefault();

        Processes = ProcessChoice.Enumerate();
        selectedProcess = Processes.FirstOrDefault();

        MonitorCommand = new DelegateCommand(Monitor);
        RecordCommand = new DelegateCommand(Record);
        StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
        ResetPeakCommand = new DelegateCommand(ResetPeak);
        RecordingsViewModel = new RecordingsViewModel();

        meterTimer.Interval = TimeSpan.FromMilliseconds(50);
        meterTimer.Tick += (_, _) => UpdateMeter();
        ResetPeak();
    }

    public IReadOnlyList<MMDevice> CaptureDevices { get; }

    public IReadOnlyList<MMDevice> RenderDevices { get; }

    public IReadOnlyList<ProcessChoice> Processes { get; }

    /// <summary>The device list the picker shows — capture endpoints, or render endpoints for loopback.</summary>
    public IReadOnlyList<MMDevice> AvailableDevices =>
        captureSource == CaptureSource.Loopback ? RenderDevices : CaptureDevices;

    /// <summary>The source picker's entries — the enum's own names don't read well in a combo.</summary>
    public IReadOnlyList<CaptureSourceChoice> CaptureSources { get; } = new[]
    {
        new CaptureSourceChoice(CaptureSource.CaptureDevice, "Capture device"),
        new CaptureSourceChoice(CaptureSource.Loopback, "Loopback (render device)"),
        new CaptureSourceChoice(CaptureSource.DefaultDeviceRouting, "Default device (stream routing)"),
        new CaptureSourceChoice(CaptureSource.ProcessLoopback, "Process loopback"),
    };

    public CaptureSource CaptureSource
    {
        get => captureSource;
        set
        {
            if (captureSource == value) return;
            var wasLoopback = captureSource == CaptureSource.Loopback;
            captureSource = value;
            OnPropertyChanged(nameof(CaptureSource));
            OnPropertyChanged(nameof(ShowDevicePicker));
            OnPropertyChanged(nameof(ShowProcessPicker));
            if (wasLoopback != (value == CaptureSource.Loopback))
            {
                OnPropertyChanged(nameof(AvailableDevices));
                SelectedDevice = AvailableDevices.FirstOrDefault();
            }
        }
    }

    /// <summary>Stream routing picks the device itself, and process loopback has no endpoint at all.</summary>
    public bool ShowDevicePicker =>
        captureSource is CaptureSource.CaptureDevice or CaptureSource.Loopback;

    public bool ShowProcessPicker => captureSource == CaptureSource.ProcessLoopback;

    public MMDevice SelectedDevice
    {
        get => selectedDevice;
        set
        {
            if (selectedDevice == value) return;
            selectedDevice = value;
            OnPropertyChanged(nameof(SelectedDevice));
        }
    }

    public ProcessChoice SelectedProcess
    {
        get => selectedProcess;
        set
        {
            if (selectedProcess == value) return;
            selectedProcess = value;
            OnPropertyChanged(nameof(SelectedProcess));
        }
    }

    public int ShareModeIndex
    {
        get => shareModeIndex;
        set { shareModeIndex = value; OnPropertyChanged(nameof(ShareModeIndex)); }
    }

    public int SyncModeIndex
    {
        get => syncModeIndex;
        set { syncModeIndex = value; OnPropertyChanged(nameof(SyncModeIndex)); }
    }

    public int BufferMilliseconds
    {
        get => bufferMilliseconds;
        set { bufferMilliseconds = value; OnPropertyChanged(nameof(BufferMilliseconds)); }
    }

    public bool UseLowLatency
    {
        get => useLowLatency;
        set { useLowLatency = value; OnPropertyChanged(nameof(UseLowLatency)); }
    }

    /// <summary>When set, an unachievable low-latency request throws instead of silently falling back.</summary>
    public bool RequireLowLatency
    {
        get => requireLowLatency;
        set { requireLowLatency = value; OnPropertyChanged(nameof(RequireLowLatency)); }
    }

    public bool UseRawMode
    {
        get => useRawMode;
        set
        {
            useRawMode = value;
            // The builder rejects raw + communications: one asks to bypass the signal-processing
            // pipeline, the other asks for it. Keep the UI from offering the invalid pair.
            if (value) useCommunicationsMode = false;
            OnPropertyChanged(nameof(UseRawMode));
            OnPropertyChanged(nameof(UseCommunicationsMode));
        }
    }

    public bool UseCommunicationsMode
    {
        get => useCommunicationsMode;
        set
        {
            useCommunicationsMode = value;
            if (value) useRawMode = false;
            OnPropertyChanged(nameof(UseCommunicationsMode));
            OnPropertyChanged(nameof(UseRawMode));
        }
    }

    public bool UseMmcss
    {
        get => useMmcss;
        set { useMmcss = value; OnPropertyChanged(nameof(UseMmcss)); }
    }

    /// <summary>
    /// False means "don't call WithFormat" — capture at the device mix format, which is also the
    /// only way IAudioClient3 low latency can engage.
    /// </summary>
    public bool UseCustomFormat
    {
        get => useCustomFormat;
        set { useCustomFormat = value; OnPropertyChanged(nameof(UseCustomFormat)); }
    }

    public int SampleRate
    {
        get => sampleRate;
        set { sampleRate = value; OnPropertyChanged(nameof(SampleRate)); }
    }

    public int BitDepth
    {
        get => bitDepth;
        set { bitDepth = value; OnPropertyChanged(nameof(BitDepth)); }
    }

    public int ChannelCount
    {
        get => channelCount;
        set { channelCount = value; OnPropertyChanged(nameof(ChannelCount)); }
    }

    public int SampleTypeIndex
    {
        get => sampleTypeIndex;
        set
        {
            sampleTypeIndex = value;
            BitDepth = value == 0 ? 32 : 16;
            OnPropertyChanged(nameof(SampleTypeIndex));
            OnPropertyChanged(nameof(IsBitDepthConfigurable));
        }
    }

    public bool IsBitDepthConfigurable => sampleTypeIndex == 1;

    public string Status
    {
        get => status;
        private set { status = value; OnPropertyChanged(nameof(Status)); }
    }

    /// <summary>The format the recorder actually opened — not necessarily the one requested.</summary>
    public string ActualFormatText
    {
        get => actualFormatText;
        private set { actualFormatText = value; OnPropertyChanged(nameof(ActualFormatText)); }
    }

    public string LatencyText
    {
        get => latencyText;
        private set { latencyText = value; OnPropertyChanged(nameof(LatencyText)); }
    }

    /// <summary>Why low latency didn't happen, when it was asked for and refused. Null otherwise.</summary>
    public string LowLatencyText
    {
        get => lowLatencyText;
        private set
        {
            lowLatencyText = value;
            OnPropertyChanged(nameof(LowLatencyText));
            OnPropertyChanged(nameof(HasLowLatencyText));
        }
    }

    public bool HasLowLatencyText => !string.IsNullOrEmpty(lowLatencyText);

    /// <summary>Meter bar position, 0..1, mapped from dBFS over a -60..0 dB range.</summary>
    public double MeterValue
    {
        get => meterValue;
        private set { meterValue = value; OnPropertyChanged(nameof(MeterValue)); }
    }

    public string PeakText
    {
        get => peakText;
        private set { peakText = value; OnPropertyChanged(nameof(PeakText)); }
    }

    /// <summary>
    /// Whether the capture format is one the meter can decode. Says so on screen rather than
    /// leaving a meter that just sits at zero looking broken.
    /// </summary>
    public bool MeterSupported
    {
        get => meterSupported;
        private set { meterSupported = value; OnPropertyChanged(nameof(MeterSupported)); }
    }

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            isRunning = value;
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(CanEditOptions));
        }
    }

    /// <summary>True once the file writer is attached — i.e. monitoring has been armed to disk.</summary>
    public bool IsWriting
    {
        get => isWriting;
        private set { isWriting = value; OnPropertyChanged(nameof(IsWriting)); }
    }

    /// <summary>Options are baked into the recorder at build time, so lock them while it runs.</summary>
    public bool CanEditOptions => !isRunning;

    private void Monitor() => Start(writeToFile: false);

    private void Record()
    {
        // Already writing: pressing Record again would abandon the open file mid-stream.
        if (isWriting) return;
        if (isRunning)
        {
            // Already monitoring — just arm the writer, no need to restart the stream.
            StartWriter();
            return;
        }
        Start(writeToFile: true);
    }

    private async void Start(bool writeToFile)
    {
        try
        {
            recorder = await BuildRecorderAsync();
            // A WASAPI mix format arrives as WaveFormatExtensible, whose Encoding is Extensible
            // rather than IeeeFloat or Pcm. Reduce it to the equivalent standard format so the
            // meter can switch on the encoding — otherwise 32-bit float samples get read as
            // 32-bit integers and the level sits at a near-constant ~-5 dBFS whatever goes in.
            captureFormat = recorder.WaveFormat is WaveFormatExtensible extensible
                ? extensible.ToStandardWaveFormat()
                : recorder.WaveFormat;
            MeterSupported = captureFormat.Encoding is WaveFormatEncoding.IeeeFloat or WaveFormatEncoding.Pcm;
            recorder.DataAvailable += OnDataAvailable;
            recorder.RecordingStopped += OnRecordingStopped;
            recorder.StartRecording();

            IsRunning = true;
            ResetPeak();
            meterTimer.Start();

            ActualFormatText = recorder.WaveFormat.ToString();
            LatencyText = $"{recorder.LatencyMilliseconds} ms" +
                          (recorder.LowLatencyActive ? " (IAudioClient3 low latency active)" : "");
            LowLatencyText = recorder.LowLatencyUnavailableReason is { } reason
                ? $"Low latency unavailable: {reason}"
                : null;

            if (writeToFile) StartWriter();
            Status = writeToFile ? "Recording" : "Monitoring";

            MonitorCommand.IsEnabled = false;
            StopCommand.IsEnabled = true;
        }
        catch (Exception e)
        {
            // A rejected option combination (exclusive mode the device won't grant, required low
            // latency, raw mode on a device without IAudioClient2) surfaces here — surfacing it is
            // the point of the demo, so report it rather than swallowing it.
            Status = "Failed: " + e.Message;
            MessageBox.Show(e.Message, "Could not start the recorder");
            CleanUpRecorder();
            IsRunning = false;
            MonitorCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
        }
    }

    private Task<WasapiRecorder> BuildRecorderAsync()
    {
        var builder = new WasapiRecorderBuilder()
            .WithBufferLength(BufferMilliseconds);

        builder = ShareModeIndex == 0 ? builder.WithSharedMode() : builder.WithExclusiveMode();
        builder = SyncModeIndex == 0 ? builder.WithEventSync() : builder.WithPollingSync();

        switch (CaptureSource)
        {
            case CaptureSource.CaptureDevice:
                builder.WithDevice(SelectedDevice);
                break;
            case CaptureSource.Loopback:
                builder.WithDevice(SelectedDevice).WithLoopbackCapture();
                break;
            case CaptureSource.DefaultDeviceRouting:
                builder.WithDefaultDeviceStreamRouting();
                break;
            case CaptureSource.ProcessLoopback:
                if (SelectedProcess == null)
                    throw new InvalidOperationException("Select a process to capture first.");
                builder.WithProcessLoopback((uint)SelectedProcess.Id);
                break;
        }

        if (UseCustomFormat) builder.WithFormat(BuildFormat());
        if (UseLowLatency) builder.WithLowLatency(RequireLowLatency);
        if (UseRawMode) builder.WithRawMode();
        if (UseCommunicationsMode) builder.WithCommunicationsMode();
        if (UseMmcss) builder.WithMmcssThreadPriority();

        // BuildAsync covers every case — process loopback and stream routing genuinely need it,
        // and it just wraps Build() for the rest.
        return builder.BuildAsync();
    }

    private WaveFormat BuildFormat() =>
        SampleTypeIndex == 0
            ? WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, ChannelCount)
            : new WaveFormat(SampleRate, BitDepth, ChannelCount);

    private void StartWriter()
    {
        currentFileName = $"NAudioDemo {DateTime.Now:yyyy-MM-dd HH-mm-ss}.wav";
        lock (writerLock)
        {
            writer = new WaveFileWriter(
                Path.Combine(RecordingsViewModel.OutputFolder, currentFileName), recorder.WaveFormat);
        }
        IsWriting = true;
        RecordCommand.IsEnabled = false;
        Status = "Recording";
    }

    private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags,
        long devicePosition, long qpcPosition)
    {
        // Runs on the capture thread. The span is only valid for this call, so anything kept has
        // to be reduced or copied here — the peak is reduced, the file write consumes it directly.
        var peak = PeakOf(buffer, captureFormat);
        if (peak > peakSinceLastMeterTick) peakSinceLastMeterTick = peak;

        lock (writerLock)
        {
            writer?.Write(buffer);
        }
    }

    /// <summary>
    /// Largest absolute sample in the buffer, as a 0..1 magnitude. Returns 0 for any encoding it
    /// doesn't understand rather than guessing from the bit depth alone — misreading float data
    /// as integer produces a plausible-looking but meaningless level.
    /// </summary>
    /// <param name="format">
    /// Must be a standard PCM or IEEE float format. Reduce a <see cref="WaveFormatExtensible"/>
    /// with <see cref="WaveFormatExtensible.ToStandardWaveFormat"/> before calling.
    /// </param>
    internal static float PeakOf(ReadOnlySpan<byte> buffer, WaveFormat format)
    {
        float peak = 0f;
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            foreach (var s in MemoryMarshal.Cast<byte, float>(buffer))
            {
                var a = Math.Abs(s);
                if (a > peak) peak = a;
            }
        }
        else if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
        {
            foreach (var s in MemoryMarshal.Cast<byte, short>(buffer))
            {
                var a = Math.Abs(s / 32768f);
                if (a > peak) peak = a;
            }
        }
        else if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 24)
        {
            for (int i = 0; i + 2 < buffer.Length; i += 3)
            {
                int sample = (buffer[i + 2] << 16) | (buffer[i + 1] << 8) | buffer[i];
                if ((sample & 0x800000) != 0) sample |= unchecked((int)0xFF000000); // sign-extend
                var a = Math.Abs(sample / 8388608f);
                if (a > peak) peak = a;
            }
        }
        else if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 32)
        {
            foreach (var s in MemoryMarshal.Cast<byte, int>(buffer))
            {
                var a = Math.Abs(s / 2147483648f);
                if (a > peak) peak = a;
            }
        }
        return peak > 1f ? 1f : peak;
    }

    private void UpdateMeter()
    {
        if (!MeterSupported)
        {
            PeakText = $"n/a ({captureFormat.Encoding})";
            return;
        }

        var peak = peakSinceLastMeterTick;
        peakSinceLastMeterTick = 0f;
        if (peak > peakHold) peakHold = peak;

        MeterValue = NormalizeDb(peak);
        PeakText = $"{FormatDb(peak)} (max {FormatDb(peakHold)})";
    }

    private static double NormalizeDb(float magnitude)
    {
        if (magnitude <= 0f) return 0.0;
        var db = 20.0 * Math.Log10(magnitude);
        if (db <= MeterFloorDb) return 0.0;
        return (db - MeterFloorDb) / -MeterFloorDb;
    }

    private static string FormatDb(float magnitude) =>
        magnitude <= 0f ? "-∞ dBFS" : $"{20.0 * Math.Log10(magnitude):F1} dBFS";

    private void ResetPeak()
    {
        peakHold = 0f;
        peakSinceLastMeterTick = 0f;
        MeterValue = 0;
        PeakText = $"{FormatDb(0)} (max {FormatDb(0)})";
    }

    private void Stop()
    {
        recorder?.StopRecording();
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        // WasapiRecorder posts this to the synchronization context captured when it was built —
        // the UI thread here — so it's safe to touch view-model state directly.
        meterTimer.Stop();
        CloseWriter();
        CleanUpRecorder();

        IsRunning = false;
        MeterValue = 0;
        Status = e.Exception == null ? "Stopped" : "Stopped with error: " + e.Exception.Message;
        MonitorCommand.IsEnabled = true;
        StopCommand.IsEnabled = false;
    }

    private void CloseWriter()
    {
        string finished = null;
        lock (writerLock)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
                finished = currentFileName;
            }
        }
        IsWriting = false;
        RecordCommand.IsEnabled = true;
        if (finished != null)
        {
            RecordingsViewModel.Recordings.Add(finished);
            RecordingsViewModel.SelectedRecording = finished;
        }
    }

    private void CleanUpRecorder()
    {
        if (recorder == null) return;
        recorder.DataAvailable -= OnDataAvailable;
        recorder.RecordingStopped -= OnRecordingStopped;
        recorder.Dispose();
        recorder = null;
    }

    public void Dispose()
    {
        meterTimer.Stop();
        // Dispose joins the capture thread, so the posted RecordingStopped may never get to run
        // its cleanup — close the file here too rather than leave a truncated WAV behind when the
        // user switches away from the panel mid-recording.
        recorder?.StopRecording();
        CleanUpRecorder();
        CloseWriter();
    }
}

/// <summary>A <see cref="CaptureSource"/> paired with the label the picker shows for it.</summary>
internal record CaptureSourceChoice(CaptureSource Value, string Name);

/// <summary>A pickable process for per-process loopback capture.</summary>
internal class ProcessChoice
{
    public int Id { get; init; }

    public string Name { get; init; }

    public override string ToString() => $"{Name} ({Id})";

    /// <summary>
    /// Processes worth offering: those with a main window, as a rough proxy for "something the
    /// user could plausibly be playing audio from".
    /// </summary>
    public static IReadOnlyList<ProcessChoice> Enumerate()
    {
        try
        {
            return Process.GetProcesses()
                .Where(p => p.MainWindowHandle != IntPtr.Zero)
                .Select(p => new ProcessChoice { Id = p.Id, Name = p.ProcessName })
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<ProcessChoice>();
        }
    }
}
