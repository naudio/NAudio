using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Exercises per-process WASAPI loopback capture (ActivateAudioInterfaceAsync with
/// AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS). The first goal is simply to <em>successfully activate
/// and start capturing</em> — the test reports the exact HRESULT/exception at each stage so a
/// failure tells us precisely where the pipeline broke. With <c>playSine</c> enabled it plays a
/// tone in this same process so there is real audio to capture (WASAPI loopback delivers nothing
/// at all — not even silence — when the target is idle).
/// </summary>
internal sealed class WasapiProcessLoopbackTest : IConsoleTest
{
    public string Id => "Wasapi.ProcessLoopback";
    public string Description => "Capture audio rendered by a specific process (per-process WASAPI loopback)";
    public MenuPath? MenuLocation => new("WASAPI", "Process loopback capture", Group: "Recorder", Order: 3);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("processId", typeof(int), Required: false, Default: 0,
            Help: "target process id (0 = this test's own process)"),
        new("mode", typeof(ProcessLoopbackMode), Required: false,
            Default: ProcessLoopbackMode.IncludeTargetProcessTree,
            Help: "include or exclude the target process tree"),
        new("playSine", typeof(bool), Required: false, Default: true,
            Help: "play a sine wave in-process so there is audio to capture"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(8),
            Help: "capture duration"),
        new("output", typeof(string), Required: false,
            Help: "optional WAV output path (auto-named on Desktop if blank)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var processId = ctx.Get<int>("processId");
        if (processId == 0) processId = Environment.ProcessId;
        var mode = ctx.Get<ProcessLoopbackMode>("mode");
        var playSine = ctx.Get<bool>("playSine");
        var duration = ctx.Get<TimeSpan>("duration");
        ctx.TryGet<string>("output", out var filePath);

        AnsiConsole.MarkupLine($"[grey]Target PID:[/] {processId} [dim]({mode})[/]");
        AnsiConsole.MarkupLine($"[grey]Play sine:[/] {playSine}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]  {duration.TotalSeconds:F0}s");
        AnsiConsole.WriteLine();

        var diagnostics = new Dictionary<string, string>
        {
            ["targetProcessId"] = processId.ToString(),
            ["mode"] = mode.ToString(),
        };

        // --- Stage 1: activate the process-loopback audio client (async) -------------------------
        // Run the activation on the thread pool: Main is [STAThread], and although the completion
        // handler is agile (so the callback won't deadlock an STA), keeping the whole async flow
        // off the UI thread is the robust pattern.
        WasapiRecorder recorder;
        try
        {
            recorder = Task.Run(() => new WasapiRecorderBuilder()
                    .WithProcessLoopback((uint)processId, mode)
                    .BuildAsync())
                .GetAwaiter().GetResult();
            AnsiConsole.MarkupLine("[green]✓[/] Activated process-loopback audio client");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Activation failed: {Markup.Escape(Describe(ex))}");
            return TestResult.Fail($"Activation failed: {Describe(ex)}", diagnostics);
        }

        diagnostics["captureFormat"] = recorder.WaveFormat.ToString();
        AnsiConsole.MarkupLine($"[grey]Format:[/]    {recorder.WaveFormat}");

        long byteCount = 0;
        long packetCount = 0;
        long silentPackets = 0;
        Exception? stoppedException = null;
        WaveFileWriter? writer = null;

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        }
        else
        {
            filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"NAudio_ProcessLoopback_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        }

        try
        {
            using (recorder)
            {
                writer = new WaveFileWriter(filePath, recorder.WaveFormat);

                recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
                {
                    Interlocked.Increment(ref packetCount);
                    if ((flags & AudioClientBufferFlags.Silent) != 0)
                        Interlocked.Increment(ref silentPackets);

                    if (!buffer.IsEmpty)
                    {
                        var arr = buffer.ToArray();
                        // ReSharper disable once AccessToDisposedClosure
                        writer.Write(arr, 0, arr.Length);
                        Interlocked.Add(ref byteCount, arr.Length);
                    }
                };
                recorder.RecordingStopped += (_, e) => stoppedException = e.Exception;

                // --- Stage 2: start capturing (Initialize + Start) -------------------------------
                try
                {
                    recorder.StartRecording();
                    AnsiConsole.MarkupLine("[green]✓[/] Started capturing");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] StartRecording failed: {Markup.Escape(Describe(ex))}");
                    return TestResult.Fail($"StartRecording failed: {Describe(ex)}", diagnostics);
                }

                // --- Optional: produce audio in this process so loopback has something to capture --
                using var sinePlayer = playSine ? StartSinePlayer() : null;

                AnsiConsole.MarkupLine(
                    $"\n[bold red]Capturing for {duration.TotalSeconds:F0}s...[/] " +
                    $"[dim]({(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");

                var start = DateTime.UtcNow;
                while (DateTime.UtcNow - start < duration)
                {
                    if (stoppedException != null) break;
                    if (ctx.Interactive && Console.KeyAvailable
                        && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
                    if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
                }

                recorder.StopRecording();
                sinePlayer?.Stop();
            }
        }
        finally
        {
            writer?.Dispose();
        }

        diagnostics["packets"] = packetCount.ToString();
        diagnostics["silentPackets"] = silentPackets.ToString();
        diagnostics["pcmBytes"] = byteCount.ToString();
        diagnostics["outputPath"] = filePath;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]Packets:[/]   {packetCount} ([dim]{silentPackets} silent[/])");
        AnsiConsole.MarkupLine($"[grey]Captured:[/]  {byteCount / 1024}KB");
        AnsiConsole.MarkupLine($"[grey]Output:[/]    {Markup.Escape(filePath)}");

        if (stoppedException != null)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Capture thread error: {Markup.Escape(Describe(stoppedException))}");
            return TestResult.Fail($"Capture thread error: {Describe(stoppedException)}", diagnostics);
        }

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped("Cancelled — file kept", diagnostics);

        // Success here means activation + start worked. Zero captured bytes is acceptable (the
        // target may have rendered no audio), but we call it out so the operator knows.
        return byteCount > 0
            ? TestResult.Pass($"Captured {byteCount / 1024}KB across {packetCount} packets", diagnostics)
            : TestResult.Pass("Activated and captured successfully (no audio data — target was idle?)", diagnostics);
    }

    private static IWavePlayer? StartSinePlayer()
    {
        var device = WasapiDevices.ResolveRender(WasapiDevices.DefaultMarker);
        if (device is null)
        {
            AnsiConsole.MarkupLine("[yellow]![/] No default render device — skipping sine playback");
            return null;
        }

        WasapiVolumeSafety.CapAt(device);
        var player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .Build();
        player.Init(new SineWaveSource(440f, 0.25f));
        player.Play();
        AnsiConsole.MarkupLine($"[green]✓[/] Playing 440Hz sine on [grey]{Markup.Escape(device.FriendlyName)}[/]");
        return player;
    }

    private static string Describe(Exception? ex)
    {
        if (ex is null) return "(none)";
        var hr = ex is COMException or ExternalException ? $" (HRESULT 0x{ex.HResult:X8})" : "";
        return $"{ex.GetType().Name}: {ex.Message}{hr}";
    }
}
