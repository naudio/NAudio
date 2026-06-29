using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Records from a capture endpoint into memory for <c>recordDuration</c>, then plays the
/// captured audio back through the render endpoint. End-to-end smoke test for
/// <see cref="WasapiRecorder"/> + <see cref="WasapiPlayer"/>.
/// </summary>
internal sealed class WasapiRecordAndPlaybackTest : IConsoleTest
{
    public string Id => "Wasapi.RecordAndPlayback";
    public string Description => "Record from capture endpoint to memory, then play back through render endpoint";
    public MenuPath? MenuLocation => new("WASAPI", "Record and playback", Group: "Recorder", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("captureDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "capture endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.CaptureDeviceNames),
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("recordDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(15),
            Help: "recording duration"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var captureName = ctx.Get<string>("captureDevice");
        var renderName = ctx.Get<string>("renderDevice");
        var recordDuration = ctx.Get<TimeSpan>("recordDuration");

        var captureDevice = WasapiDevices.ResolveCapture(captureName);
        if (captureDevice is null) return TestResult.Fail($"Capture device not found: {captureName}");
        var renderDevice = WasapiDevices.ResolveRender(renderName);
        if (renderDevice is null) return TestResult.Fail($"Render device not found: {renderName}");

        AnsiConsole.MarkupLine($"[grey]Capture:[/] {Markup.Escape(captureDevice.FriendlyName)}");
        AnsiConsole.MarkupLine($"[grey]Render:[/]  {Markup.Escape(renderDevice.FriendlyName)}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {recordDuration.TotalSeconds:F0}s");
        AnsiConsole.WriteLine();

        // ---- record ----
        using var recorder = new WasapiRecorderBuilder()
            .WithDevice(captureDevice)
            .WithSharedMode()
            .WithEventSync()
            .Build();

        var captured = new MemoryStream();
        var waveFormat = recorder.WaveFormat;
        recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) == 0)
            {
                var arr = buffer.ToArray();
                captured.Write(arr, 0, arr.Length);
            }
        };

        AnsiConsole.MarkupLine(
            $"[bold red]Recording for {recordDuration.TotalSeconds:F0}s...[/] " +
            $"[dim]({(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");
        recorder.StartRecording();
        var recordStart = DateTime.UtcNow;
        while (DateTime.UtcNow - recordStart < recordDuration)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
        }
        recorder.StopRecording();

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped("Cancelled during recording, no playback");

        var capturedBytes = captured.ToArray();
        AnsiConsole.MarkupLine($"\n[grey]Captured {capturedBytes.Length / 1024}KB ({waveFormat})[/]");
        if (capturedBytes.Length == 0)
            return TestResult.Fail("No audio data captured (silent device?)");

        // ---- playback ----
        WasapiVolumeSafety.CapAt(renderDevice);

        AnsiConsole.MarkupLine($"\n[bold green]Playing back through {Markup.Escape(renderDevice.FriendlyName)}[/]");

        using var player = new WasapiPlayerBuilder()
            .WithDevice(renderDevice)
            .WithSharedMode()
            .WithEventSync()
            .Build();

        var rawStream = new RawSourceWaveStream(new MemoryStream(capturedBytes), waveFormat);
        player.Init(rawStream);

        var playStart = DateTime.UtcNow;
        player.Play();
        var maxPlay = recordDuration + TimeSpan.FromSeconds(2); // small grace
        while (player.PlaybackState != PlaybackState.Stopped)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
            if (DateTime.UtcNow - playStart >= maxPlay) break;
        }
        player.Stop();

        return TestResult.Pass(
            $"Recorded {capturedBytes.Length / 1024}KB, played back through {renderDevice.FriendlyName}",
            new Dictionary<string, string>
            {
                ["captureDevice"] = captureDevice.FriendlyName,
                ["renderDevice"] = renderDevice.FriendlyName,
                ["capturedBytes"] = capturedBytes.Length.ToString(),
                ["waveFormat"] = waveFormat.ToString(),
            });
    }
}
