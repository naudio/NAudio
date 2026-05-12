using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Records ~1s, <c>Stop()</c>s, <c>Reinitialize()</c>s, <c>Start()</c>s again, records ~1s more.
/// Verifies the same <see cref="AsioDevice"/> instance can recover the way
/// <c>DriverResetRequest</c> expects (Phase 0 F6 regression).
/// </summary>
sealed class AsioReinitializeRoundTripTest : IConsoleTest
{
    public string Id => "Asio.ReinitializeRoundTrip";
    public string Description => "Verify AsioDevice.Reinitialize() restores recording on the same instance";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Reinitialize round-trip", Group: "Lifecycle", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("passDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(1),
            Help: "duration of each recording pass"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");
        if (device.Capabilities.NbInputChannels == 0)
            return TestResult.Fail("Driver has no input channels");

        var passDuration = ctx.Get<TimeSpan>("passDuration");

        device.InitRecording(new AsioRecordingOptions
        {
            InputChannels = [0],
            SampleRate = device.CurrentSampleRate,
        });

        var pass1Callbacks = 0;
        var pass2Callbacks = 0;
        var currentPass = 1;
        device.AudioCaptured += (_, _) =>
        {
            if (currentPass == 1) Interlocked.Increment(ref pass1Callbacks);
            else Interlocked.Increment(ref pass2Callbacks);
        };

        // Pass 1
        device.Start();
        Thread.Sleep(passDuration);
        device.Stop();
        AnsiConsole.MarkupLine($"  Pass 1: {pass1Callbacks} callbacks fired before Stop().");

        try
        {
            device.Reinitialize();
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Reinitialize threw: {ex.Message}");
        }

        // Pass 2 — same options, same AsioDevice instance
        currentPass = 2;
        device.Start();
        Thread.Sleep(passDuration);
        device.Stop();
        AnsiConsole.MarkupLine($"  Pass 2: {pass2Callbacks} callbacks fired after Reinitialize+Start.");

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["pass1Callbacks"] = pass1Callbacks.ToString(),
            ["pass2Callbacks"] = pass2Callbacks.ToString(),
            ["passDurationMs"] = passDuration.TotalMilliseconds.ToString("F0"),
        };

        if (pass1Callbacks == 0)
            return TestResult.Fail("Pass 1 produced no callbacks — driver not producing audio?", diagnostics);
        if (pass2Callbacks == 0)
            return TestResult.Fail("Pass 2 produced no callbacks after Reinitialize — recovery broken", diagnostics);

        return TestResult.Pass(
            $"Both passes produced audio ({pass1Callbacks} + {pass2Callbacks} callbacks)",
            diagnostics);
    }
}
