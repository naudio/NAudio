using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Routes ASIO input channels through a fixed gain straight to output channels (one-to-one).
/// Reports per-channel peak amplitude at the end.
/// </summary>
sealed class AsioDuplexPassthroughTest : IConsoleTest
{
    public string Id => "Asio.DuplexPassthrough";
    public string Description => "Run duplex passthrough (input → gain → output) for a fixed duration";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Duplex passthrough (input → gain → output)",
            Group: "Duplex", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("inputChannels", typeof(string), Required: true,
            Help: "comma-separated input channel indices",
            InteractivePrompter: AsioDrivers.PickInputChannels),
        new("outputChannels", typeof(string), Required: true,
            Help: "comma-separated output channel indices (same count as inputs)",
            InteractivePrompter: AsioDrivers.PickOutputChannels),
        new("gain", typeof(float), Required: false, Default: 0.5f, Help: "passthrough gain (0..1)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10)),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");

        if (device.Capabilities.NbInputChannels == 0 || device.Capabilities.NbOutputChannels == 0)
            return TestResult.Fail("Driver must have both inputs and outputs for duplex mode");

        if (!AsioDrivers.TryParseChannels(ctx.Get<string>("inputChannels"),
            device.Capabilities.NbInputChannels, out var inputs, out var err))
            return TestResult.Fail($"--inputChannels: {err}");
        if (!AsioDrivers.TryParseChannels(ctx.Get<string>("outputChannels"),
            device.Capabilities.NbOutputChannels, out var outputs, out err))
            return TestResult.Fail($"--outputChannels: {err}");
        if (outputs.Length != inputs.Length)
            return TestResult.Fail($"Need exactly {inputs.Length} output channel(s), got {outputs.Length}");

        var gain = ctx.Get<float>("gain");
        var duration = ctx.Get<TimeSpan>("duration");

        AnsiConsole.MarkupLine($"[yellow]⚠ Routes input straight to outputs at {gain:P0} gain. " +
                               "Watch for feedback if mic and speaker share a room.[/]\n");

        var peaks = new float[inputs.Length];        // running max — survives across callbacks
        var currentPeaks = new float[inputs.Length]; // per-callback — fed to the live meter
        try
        {
            device.InitDuplex(new AsioDuplexOptions
            {
                InputChannels = inputs,
                OutputChannels = outputs,
                SampleRate = device.CurrentSampleRate,
                Processor = (in AsioProcessBuffers b) =>
                {
                    var n = Math.Min(b.InputChannelCount, b.OutputChannelCount);
                    for (var ch = 0; ch < n; ch++)
                    {
                        var inSpan = b.GetInput(ch);
                        var outSpan = b.GetOutput(ch);
                        var peak = 0f;
                        for (var i = 0; i < b.Frames; i++)
                        {
                            var s = inSpan[i] * gain;
                            outSpan[i] = s;
                            var a = s < 0 ? -s : s;
                            if (a > peak) peak = a;
                        }
                        currentPeaks[ch] = peak;
                        if (peak > peaks[ch]) peaks[ch] = peak;
                    }
                },
            });
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        var completed = new ManualResetEventSlim();
        Exception? stopException = null;
        device.Stopped += (_, e) => { stopException = e.Exception; completed.Set(); };

        AnsiConsole.MarkupLine(
            $"[green]Duplex passthrough running[/] " +
            $"[dim]({duration.TotalSeconds:F0}s, {(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");
        AnsiConsole.MarkupLine($"[grey]Buffer:[/] {device.FramesPerBuffer} frames, " +
                               $"latency in/out: {device.InputLatencySamples}/{device.OutputLatencySamples} frames " +
                               $"@ {device.CurrentSampleRate} Hz\n");

        var start = DateTime.UtcNow;
        device.Start();
        var cancelled = false;

        if (ctx.Interactive)
        {
            using var meter = new LiveMeterRenderer(inputs.Length);
            while (!completed.IsSet && DateTime.UtcNow - start < duration)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
                if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
                for (var i = 0; i < inputs.Length; i++)
                    meter.Update(i, $"in {inputs[i]} → out {outputs[i]}", currentPeaks[i]);
            }
        }
        else
        {
            while (!completed.IsSet && DateTime.UtcNow - start < duration)
            {
                if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
            }
        }
        if (!completed.IsSet)
        {
            try { device.Stop(); } catch { }
            completed.Wait(TimeSpan.FromSeconds(2));
        }

        var summary = new Table().AddColumn("In → Out").AddColumn("Peak").AddColumn("Peak dBFS");
        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["gain"] = gain.ToString("F3"),
            ["durationMs"] = duration.TotalMilliseconds.ToString("F0"),
        };
        for (var i = 0; i < inputs.Length; i++)
        {
            var p = peaks[i];
            var db = p > 1e-6f ? 20f * MathF.Log10(p) : float.NegativeInfinity;
            summary.AddRow($"{inputs[i]} → {outputs[i]}", p.ToString("F4"),
                float.IsNegativeInfinity(db) ? "-∞" : $"{db:F1}");
            diagnostics[$"peak_{inputs[i]}to{outputs[i]}"] = p.ToString("F4");
        }
        AnsiConsole.Write(summary);

        if (stopException is not null)
            return TestResult.Fail($"Stopped with error: {stopException.Message}", diagnostics);
        return cancelled
            ? TestResult.Skipped("Cancelled")
            : TestResult.Pass($"Passthrough ran cleanly across {inputs.Length} channel pair(s)", diagnostics);
    }
}
