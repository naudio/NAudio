using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Plays an audio file through <see cref="WasapiPlayer"/>. <c>mode</c> picks between
/// shared, exclusive, and low-latency (shared + IAudioClient3) — the three previously
/// separate menu entries collapse to one parameterised test.
/// </summary>
internal sealed class WasapiPlayFileTest : IConsoleTest
{
    public string Id => "Wasapi.PlayFile";
    public string Description => "Play an audio file via WasapiPlayer (shared / exclusive / low-latency)";
    public MenuPath? MenuLocation => new("WASAPI", "Play audio file", Group: "Player", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "audio file path", IsFilePath: true, FileCategory: "audio"),
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name, 'default', or auto stream routing",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("mode", typeof(string), Required: false, Default: "Shared",
            Help: "playback mode",
            Choices: ["Shared", "Exclusive", "LowLatency"]),
        new("maxDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMinutes(5),
            Help: "playback cap (interactive mode uses ESC instead)", CliOnly: true),
    ];

    public TestResult Run(TestContext ctx)
    {
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath)) return TestResult.Fail($"Input not found: {inputPath}");

        var deviceName = ctx.Get<string>("renderDevice");
        var useRouting = WasapiDevices.IsRoutingMarker(deviceName);

        MMDevice? device = null;
        if (!useRouting)
        {
            device = WasapiDevices.ResolveRender(deviceName);
            if (device is null) return TestResult.Fail($"Render device not found: {deviceName}");
            WasapiVolumeSafety.CapAt(device);
        }

        var mode = ctx.Get<string>("mode");
        var maxDuration = ctx.Get<TimeSpan>("maxDuration");

        // Automatic stream routing is standard shared mode only, so override any other requested mode.
        if (useRouting && !mode.Equals("Shared", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine($"[yellow]Stream routing is shared-mode only — ignoring '{mode}' mode.[/]");
            mode = "Shared";
        }

        WasapiPlayer player;
        if (useRouting)
        {
            player = new WasapiPlayerBuilder()
                .WithDefaultDeviceStreamRouting()
                .WithEventSync()
                .WithMmcssThreadPriority("Pro Audio")
                .BuildAsync().GetAwaiter().GetResult();
        }
        else
        {
            var builder = new WasapiPlayerBuilder()
                .WithDevice(device!)
                .WithEventSync()
                .WithMmcssThreadPriority("Pro Audio");
            builder = mode.ToLowerInvariant() switch
            {
                "exclusive" => builder.WithExclusiveMode(),
                "lowlatency" => builder.WithSharedMode().WithLowLatency(),
                _ => builder.WithSharedMode(),
            };
            player = builder.Build();
        }

        using var _player = player;
        using var reader = new MediaFoundationReader(inputPath);

        // Validate the chosen options up front (non-destructive) before opening the stream.
        var capability = player.GetPlaybackCapability(reader.WaveFormat);
        if (!capability.Supported)
            return TestResult.Fail($"Format not supported: {capability.Reason}");

        try { player.Init(reader); }
        catch (Exception ex) { return TestResult.Fail($"Init failed: {ex.Message}"); }

        var deviceDisplay = device?.FriendlyName ?? "default device (auto stream routing)";
        AnsiConsole.MarkupLine($"[bold green]Playing[/] [grey]via {Markup.Escape(deviceDisplay)}[/]");
        AnsiConsole.MarkupLine($"[grey]File:[/]   {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Mode:[/]   {mode}");
        if (mode.Equals("LowLatency", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine(player.LowLatencyActive
                ? $"[green]Low latency:[/] active ({player.LatencyMilliseconds} ms)"
                : $"[yellow]Low latency:[/] not available — fell back to standard shared mode ({Markup.Escape(player.LowLatencyUnavailableReason ?? "unknown reason")})");
        }
        AnsiConsole.MarkupLine($"[grey]Source:[/] {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Format:[/] {player.OutputWaveFormat}");
        if (capability.Conversions.Count > 0)
            AnsiConsole.MarkupLine($"[grey]Convert:[/] {Markup.Escape(string.Join(", ", capability.Conversions))}");
        AnsiConsole.MarkupLine($"[grey]Length:[/] {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine(ctx.Interactive
            ? "[dim]SPACE pause/resume, ESC stop[/]\n"
            : $"[dim]cap {maxDuration.TotalSeconds:F0}s, Ctrl+C to stop early[/]\n");

        var start = DateTime.UtcNow;
        player.Play();

        while (player.PlaybackState != PlaybackState.Stopped)
        {
            if (ctx.Interactive && Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape) break;
                if (key.Key == ConsoleKey.Spacebar)
                {
                    if (player.PlaybackState == PlaybackState.Playing)
                    {
                        player.Pause();
                        AnsiConsole.MarkupLine("[yellow]Paused[/]");
                    }
                    else if (player.PlaybackState == PlaybackState.Paused)
                    {
                        player.Play();
                        AnsiConsole.MarkupLine("[green]Resumed[/]");
                    }
                }
            }
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
            if (!ctx.Interactive && DateTime.UtcNow - start >= maxDuration) break;
        }
        player.Stop();

        var elapsed = DateTime.UtcNow - start;
        var cancelled = ctx.Cancellation.IsCancellationRequested;

        var diagnostics = new Dictionary<string, string>
        {
            ["device"] = deviceDisplay,
            ["mode"] = mode,
            ["lowLatencyActive"] = player.LowLatencyActive.ToString(),
            ["latencyMs"] = player.LatencyMilliseconds.ToString(),
            ["elapsedMs"] = elapsed.TotalMilliseconds.ToString("F0"),
            ["fileDurationMs"] = reader.TotalTime.TotalMilliseconds.ToString("F0"),
        };

        return cancelled
            ? TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s")
            : TestResult.Pass($"Played {elapsed.TotalSeconds:F1}s in {mode} mode", diagnostics);
    }
}
