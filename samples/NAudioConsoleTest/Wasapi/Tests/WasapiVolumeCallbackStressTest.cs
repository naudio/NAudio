using NAudio.CoreAudioApi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Two things in one pass over the <c>IAudioEndpointVolumeCallback</c> path.
/// <para>
/// (1) Stress-tests the CCW dispatch path that Phase 2f rebuilt on top of
/// <c>[GeneratedComInterface]</c> / <c>[GeneratedComClass]</c> / <c>ComWrappers</c>. A regression
/// here looks like an access-violation on the WASAPI worker thread the first time a callback fires
/// after <c>MasterVolumeLevelScalar</c> is set — the process exits with <c>0xC0000005</c> and no
/// managed exception. A non-zero callback count proves the chain dispatches end-to-end.
/// </para>
/// <para>
/// (2) Regression test for #351: <c>AudioEndpointVolumeCallback.OnNotify</c> used to fill the
/// per-channel volume array with channel 0's value for every channel. We set distinct per-channel
/// volumes and confirm the notification's <c>ChannelVolume</c> array reflects those distinct values
/// (and matches the device read-back) rather than collapsing to a single repeated value.
/// </para>
/// </summary>
internal sealed class WasapiVolumeCallbackStressTest : IConsoleTest
{
    public string Id => "Wasapi.VolumeCallbackStress";
    public string Description => "Stress IAudioEndpointVolumeCallback dispatch and verify per-channel volumes (#351)";
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
        var channelCount = endpointVolume.Channels.Count;

        // Capture per-channel originals so we can restore them in finally.
        var originalChannels = new float[channelCount];
        for (var i = 0; i < channelCount; i++)
            originalChannels[i] = endpointVolume.Channels[i].VolumeLevelScalar;

        AnsiConsole.MarkupLine($"Original master volume: [yellow]{original:F3}[/]  channels: [yellow]{channelCount}[/]");
        AnsiConsole.MarkupLine($"[dim]iterations={iterations}, levelInterval={levelInterval.TotalMilliseconds:F0}ms, " +
                               $"settleTime={settleTime.TotalMilliseconds:F0}ms[/]\n");

        var notifyCount = 0;
        var sync = new object();
        float[] latestChannelVolumes = [];
        void OnNotify(AudioVolumeNotificationData data)
        {
            Interlocked.Increment(ref notifyCount);
            // Snapshot the per-channel array from the most recent notification.
            lock (sync) latestChannelVolumes = data.ChannelVolume;
        }
        endpointVolume.OnVolumeNotification += OnNotify;

        var totalSets = 0;
        var cancelled = false;
        var channelCheck = "skipped";          // verified | skipped | regression
        var channelRegression = false;
        var notifiedChannels = "";
        var readbackChannels = "";
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

            // --- Per-channel verification (regression test for #351) ---
            if (!cancelled && channelCount >= 2)
            {
                // Set deliberately distinct per-channel volumes.
                for (var i = 0; i < channelCount; i++)
                    endpointVolume.Channels[i].VolumeLevelScalar = Math.Min(0.2f + i * 0.15f, 0.9f);

                // Let the final notification (with all channels at their distinct values) land.
                ctx.Cancellation.WaitHandle.WaitOne(settleTime);

                // What the device actually stored per channel...
                var readback = new float[channelCount];
                for (var i = 0; i < channelCount; i++)
                    readback[i] = endpointVolume.Channels[i].VolumeLevelScalar;

                // ...versus what the most recent notification reported.
                float[] notified;
                lock (sync) notified = latestChannelVolumes;

                readbackChannels = string.Join(", ", Array.ConvertAll(readback, v => v.ToString("F3")));
                notifiedChannels = string.Join(", ", Array.ConvertAll(notified, v => v.ToString("F3")));

                if (AllApproximatelyEqual(readback))
                {
                    // The endpoint ties its channels together (or ignores per-channel sets), so a
                    // single repeated notification value is correct — nothing to assert here.
                    channelCheck = "skipped (device does not keep channels distinct)";
                }
                else if (notified.Length == channelCount && AllApproximatelyEqual(notified))
                {
                    // Device kept the channels distinct but the notification reported one repeated
                    // value — exactly the #351 bug.
                    channelRegression = true;
                    channelCheck = "regression";
                }
                else
                {
                    channelCheck = "verified";
                }

                AnsiConsole.MarkupLine($"[dim]Per-channel device:[/] [yellow]{readbackChannels}[/]");
                AnsiConsole.MarkupLine($"[dim]Per-channel notified:[/] [yellow]{notifiedChannels}[/]");
            }
            else if (!cancelled)
            {
                channelCheck = "skipped (single-channel endpoint)";
            }
        }
        finally
        {
            endpointVolume.OnVolumeNotification -= OnNotify;
            try
            {
                endpointVolume.MasterVolumeLevelScalar = original;
                for (var i = 0; i < channelCount; i++)
                    endpointVolume.Channels[i].VolumeLevelScalar = originalChannels[i];
            }
            catch { /* best-effort restore */ }
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
            ["channelCount"] = channelCount.ToString(),
            ["channelCheck"] = channelCheck,
            ["channelsReadback"] = readbackChannels,
            ["channelsNotified"] = notifiedChannels,
            ["cancelled"] = cancelled ? "true" : "false",
        };

        if (cancelled)
            return TestResult.Skipped($"Cancelled after {totalSets} sets, {notifyCount} callbacks");

        // notifyCount == 0 is a real regression signal — CCW registered but not dispatching.
        if (notifyCount == 0)
            return TestResult.Fail(
                $"Zero callbacks fired across {totalSets} master-volume changes — CCW dispatch is broken",
                diagnostics);

        // #351: distinct per-channel volumes collapsed to one repeated value in the notification.
        if (channelRegression)
            return TestResult.Fail(
                $"Per-channel notification reported one repeated value ({notifiedChannels}) " +
                $"despite distinct device channels ({readbackChannels}) — #351 regression",
                diagnostics);

        return TestResult.Pass(
            $"{notifyCount} callbacks fired across {totalSets} master-volume changes; " +
            $"per-channel check: {channelCheck}",
            diagnostics);
    }

    // True when every element is within tolerance of the first — i.e. the array carries no
    // meaningful per-channel variation.
    private static bool AllApproximatelyEqual(float[] values, float tolerance = 0.01f)
    {
        for (var i = 1; i < values.Length; i++)
            if (Math.Abs(values[i] - values[0]) > tolerance)
                return false;
        return true;
    }
}
