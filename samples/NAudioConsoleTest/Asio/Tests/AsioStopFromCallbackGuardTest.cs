using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Phase 0 F1 regression: calling <c>Stop()</c> on the ASIO callback thread would self-deadlock.
/// The new <see cref="AsioDevice"/> must throw <see cref="InvalidOperationException"/> loudly
/// from inside the audio callback instead.
/// </summary>
sealed class AsioStopFromCallbackGuardTest : IConsoleTest
{
    public string Id => "Asio.StopFromCallbackGuard";
    public string Description => "Regression: Stop() inside AudioCaptured must throw InvalidOperationException, not deadlock";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Stop from AudioCaptured callback (F1 guard)",
            Group: "Regression tests", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");
        if (device.Capabilities.NbInputChannels == 0)
            return TestResult.Fail("Driver has no input channels");

        device.InitRecording(new AsioRecordingOptions
        {
            InputChannels = [0],
            SampleRate = device.CurrentSampleRate,
        });

        var sawGuard = false;
        Exception? unexpectedException = null;
        var fired = new ManualResetEventSlim();

        device.AudioCaptured += (_, _) =>
        {
            if (fired.IsSet) return;
            fired.Set();
            try
            {
                device.Stop(); // must throw immediately
            }
            catch (InvalidOperationException)
            {
                sawGuard = true;
            }
            catch (Exception ex)
            {
                unexpectedException = ex;
            }
        };

        device.Start();
        fired.Wait(TimeSpan.FromSeconds(3));
        Thread.Sleep(100); // let one more callback run to confirm the guard didn't break dispatch
        device.Stop();

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["callbackFired"] = fired.IsSet ? "true" : "false",
            ["sawGuard"] = sawGuard ? "true" : "false",
        };

        if (!fired.IsSet)
            return TestResult.Fail("No callback fired — test inconclusive (is the driver producing audio?)", diagnostics);
        if (unexpectedException is not null)
        {
            diagnostics["unexpected"] = $"{unexpectedException.GetType().Name}: {unexpectedException.Message}";
            return TestResult.Fail(
                $"Stop() from callback threw {unexpectedException.GetType().Name}, expected InvalidOperationException",
                diagnostics);
        }
        if (!sawGuard)
            return TestResult.Fail("Stop() from callback did not throw — guard missing, potential deadlock hazard", diagnostics);
        return TestResult.Pass("Stop() from callback threw InvalidOperationException as expected", diagnostics);
    }
}
