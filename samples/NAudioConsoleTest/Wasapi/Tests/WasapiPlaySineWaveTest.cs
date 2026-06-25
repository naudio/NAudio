using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

sealed class WasapiPlaySineWaveTest : IConsoleTest
{
    public string Id => "Wasapi.PlaySineWave";
    public string Description => "Play a generated sine wave through WasapiPlayer (shared mode)";
    public MenuPath? MenuLocation => new("WASAPI", "Play sine wave", Group: "Player", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("frequency", typeof(float), Required: false, Default: 440f, Help: "tone frequency in Hz"),
        new("amplitude", typeof(float), Required: false, Default: 0.25f, Help: "tone amplitude (0..1)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(5),
            Help: "playback duration"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceName = ctx.Get<string>("renderDevice");
        var device = WasapiDevices.ResolveRender(deviceName);
        if (device is null) return TestResult.Fail($"Render device not found: {deviceName}");

        var frequency = ctx.Get<float>("frequency");
        var amplitude = ctx.Get<float>("amplitude");
        var duration = ctx.Get<TimeSpan>("duration");

        WasapiVolumeSafety.CapAt(device);

        using var player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .WithMmcssThreadPriority("Pro Audio")
            .Build();

        var sineSource = new SineWaveSource(frequency, amplitude);
        player.Init(sineSource);

        AnsiConsole.MarkupLine(
            $"[bold green]Playing[/] {frequency}Hz sine for {duration.TotalSeconds:F1}s " +
            $"[dim](amplitude={amplitude:F2}, {(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");
        AnsiConsole.MarkupLine($"[grey]Device:[/] {Markup.Escape(device.FriendlyName)}");
        AnsiConsole.MarkupLine($"[grey]Format:[/] {player.OutputWaveFormat}\n");

        var start = DateTime.UtcNow;
        player.Play();

        while (player.PlaybackState != PlaybackState.Stopped)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
            if (DateTime.UtcNow - start >= duration) break;
        }
        player.Stop();

        var elapsed = DateTime.UtcNow - start;
        var cancelled = ctx.Cancellation.IsCancellationRequested;

        var diagnostics = new Dictionary<string, string>
        {
            ["device"] = device.FriendlyName,
            ["frequencyHz"] = frequency.ToString("F0"),
            ["amplitude"] = amplitude.ToString("F3"),
            ["elapsedMs"] = elapsed.TotalMilliseconds.ToString("F0"),
        };

        return cancelled
            ? TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s")
            : TestResult.Pass($"Played {elapsed.TotalSeconds:F1}s of {frequency:F0}Hz sine", diagnostics);
    }
}
