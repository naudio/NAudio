using NAudio.CoreAudioApi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Stress-tests the <c>IAudioEndpointVolumeCallback</c> CCW dispatch path that Phase 2f
/// rebuilt on top of <c>[GeneratedComInterface]</c> / <c>[GeneratedComClass]</c> /
/// <c>ComWrappers</c>. A regression here looks like an access-violation on the WASAPI worker
/// thread the first time a callback fires after <c>MasterVolumeLevelScalar</c> is set — the
/// process exits with <c>0xC0000005</c> and no managed exception. A non-zero callback count
/// proves the chain dispatches end-to-end.
/// </summary>
sealed class WasapiVolumeCallbackStressTest : IConsoleTest
{
    public string Id => "Wasapi.VolumeCallbackStress";
    public string Description => "Stress-test IAudioEndpointVolumeCallback CCW dispatch";
    public MenuPath? MenuLocation =>
        new("WASAPI", "Stress endpoint volume callbacks", Group: "Callbacks", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("iterations", typeof(int), Required: false, Default: 10,
            Help: "cycles through the five-level sweep"),
        new("levelInterval", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMilliseconds(50),
            Help: "pause between each volume set"),
        new("settleTime", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMilliseconds(200),
            Help: "drain time after the last set, for in-flight callbacks to land"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var iterations = ctx.Get<int>("iterations");
        var levelInterval = ctx.Get<TimeSpan>("levelInterval");
        var settleTime = ctx.Get<TimeSpan>("settleTime");

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        AnsiConsole.MarkupLine($"Device: [yellow]{Markup.Escape(device.FriendlyName)}[/]");

        var endpointVolume = device.AudioEndpointVolume;
        var original = endpointVolume.MasterVolumeLevelScalar;
        AnsiConsole.MarkupLine($"Original master volume: [yellow]{original:F3}[/]");
        AnsiConsole.MarkupLine($"[dim]iterations={iterations}, levelInterval={levelInterval.TotalMilliseconds:F0}ms, " +
                               $"settleTime={settleTime.TotalMilliseconds:F0}ms[/]\n");

        var notifyCount = 0;
        void OnNotify(AudioVolumeNotificationData _) => Interlocked.Increment(ref notifyCount);
        endpointVolume.OnVolumeNotification += OnNotify;

        var totalSets = 0;
        var cancelled = false;
        try
        {
            float[] levels = [0.50f, 0.30f, 0.50f, 0.70f, 0.50f];

            for (var iter = 0; iter < iterations; iter++)
            {
                if (ctx.Cancellation.IsCancellationRequested) { cancelled = true; break; }
                foreach (var lvl in levels)
                {
                    if (ctx.Cancellation.IsCancellationRequested) { cancelled = true; break; }
                    endpointVolume.MasterVolumeLevelScalar = lvl;
                    totalSets++;
                    // WaitHandle.WaitOne returns true once cancellation fires, false on timeout —
                    // gives a cancellable sleep without spinning.
                    if (ctx.Cancellation.WaitHandle.WaitOne(levelInterval))
                    {
                        cancelled = true;
                        break;
                    }
                }
                if (cancelled) break;
            }

            // Drain pending callbacks (still cancellable).
            ctx.Cancellation.WaitHandle.WaitOne(settleTime);

            AnsiConsole.MarkupLine(
                $"{(cancelled ? "[yellow]Cancelled.[/]" : "[green]Done.[/]")} " +
                $"{totalSets} master-volume changes; {notifyCount} callbacks fired.");
        }
        finally
        {
            endpointVolume.OnVolumeNotification -= OnNotify;
            try { endpointVolume.MasterVolumeLevelScalar = original; } catch { /* best-effort restore */ }
            // Brief settle so the restore notification can land before we tear down the CCW.
            Thread.Sleep(200);
            endpointVolume.Dispose();
            device.Dispose();
        }

        var diagnostics = new Dictionary<string, string>
        {
            ["iterations"] = iterations.ToString(),
            ["volumeSets"] = totalSets.ToString(),
            ["callbacks"] = notifyCount.ToString(),
            ["cancelled"] = cancelled ? "true" : "false",
        };

        if (cancelled)
            return TestResult.Skipped($"Cancelled after {totalSets} sets, {notifyCount} callbacks");

        // notifyCount == 0 is the real regression signal — CCW registered but not dispatching.
        return notifyCount == 0
            ? TestResult.Fail(
                $"Zero callbacks fired across {totalSets} master-volume changes — CCW dispatch is broken",
                diagnostics)
            : TestResult.Pass(
                $"{notifyCount} callbacks fired across {totalSets} master-volume changes",
                diagnostics);
    }
}
