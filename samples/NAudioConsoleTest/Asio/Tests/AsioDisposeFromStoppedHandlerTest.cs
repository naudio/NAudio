using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Plays a 1s silent stream, then <c>Dispose()</c>s the device from inside the
/// <c>Stopped</c> handler. Phase 0 F1 bug: would self-deadlock or crash. Must complete cleanly.
/// </summary>
sealed class AsioDisposeFromStoppedHandlerTest : IConsoleTest
{
    public string Id => "Asio.DisposeFromStoppedHandler";
    public string Description => "Regression: Dispose() from inside the Stopped handler must complete cleanly";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Dispose from Stopped handler (F1)",
            Group: "Regression tests", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");

        try
        {
            var sampleRate = device.CurrentSampleRate;
            var silent = new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)).ToSampleProvider();
            var limited = new OffsetSampleProvider(silent) { Take = TimeSpan.FromSeconds(1) };
            device.InitPlayback(new AsioPlaybackOptions
            {
                Source = limited.ToWaveProvider(),
                OutputChannels = [0],
                AutoStopOnEndOfStream = true,
            });
        }
        catch (Exception ex)
        {
            device.Dispose();
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        var completed = new ManualResetEventSlim();
        Exception? handlerException = null;

        device.Stopped += (_, _) =>
        {
            try
            {
                device.Dispose();
                AnsiConsole.MarkupLine("[green]Stopped handler disposed device cleanly.[/]");
            }
            catch (Exception ex)
            {
                handlerException = ex;
            }
            completed.Set();
        };

        device.Start();
        var fired = completed.Wait(TimeSpan.FromSeconds(5));

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["stoppedHandlerFired"] = fired ? "true" : "false",
        };

        if (!fired)
            return TestResult.Fail("TIMED OUT — Stopped handler never fired (possible deadlock)", diagnostics);
        if (handlerException is not null)
        {
            diagnostics["handlerException"] = $"{handlerException.GetType().Name}: {handlerException.Message}";
            return TestResult.Fail($"Stopped handler threw: {handlerException.Message}", diagnostics);
        }
        return TestResult.Pass("Dispose() from Stopped handler completed cleanly", diagnostics);
    }
}
