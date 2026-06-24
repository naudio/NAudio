using System.Diagnostics;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Records for <c>duration</c> and validates the driver-reported <c>SamplePosition</c> and
/// <c>SystemTimeNanoseconds</c> advance correctly. The audio-vs-host-clock drift check would
/// be astronomical if byte order were wrong — that's the killer test for the timing path.
/// </summary>
sealed class AsioValidateSamplePositionTest : IConsoleTest
{
    public string Id => "Asio.ValidateSamplePosition";
    public string Description => "Validate ASIO SamplePosition + SystemTimeNanoseconds monotonicity and audio↔host drift";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Validate SamplePosition + SystemTimeNanoseconds",
            Group: "Timing", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10),
            Help: "recording duration — longer = tighter drift measurement"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");
        if (device.Capabilities.NbInputChannels == 0)
            return TestResult.Fail("Driver has no input channels — needed to drive the recording callback");

        var duration = ctx.Get<TimeSpan>("duration");
        var sampleRate = device.CurrentSampleRate;

        device.InitRecording(new AsioRecordingOptions
        {
            InputChannels = [0],
            SampleRate = sampleRate,
        });

        var samples = new List<Sample>(capacity: 8192);
        var lockObj = new object();
        device.AudioCaptured += (_, e) =>
        {
            lock (lockObj)
            {
                samples.Add(new Sample(
                    e.SamplePosition, e.SystemTimeNanoseconds, Stopwatch.GetTimestamp(),
                    e.Frames, e.Speed, e.TimeCode));
            }
        };

        var stopwatchStart = Stopwatch.GetTimestamp();
        device.Start();
        AnsiConsole.MarkupLine(
            $"[green]Recording {duration.TotalSeconds:F0}s[/] " +
            $"[dim]({(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]\n");
        var pollEnd = DateTime.UtcNow + duration;
        var cancelled = false;
        while (DateTime.UtcNow < pollEnd)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
        }
        device.Stop();

        Sample[] arr;
        lock (lockObj) arr = samples.ToArray();

        if (arr.Length < 4)
            return TestResult.Fail($"Only {arr.Length} callbacks captured — driver isn't producing audio");

        AnsiConsole.MarkupLine($"[bold]Captured {arr.Length} callbacks at {sampleRate} Hz[/]");

        // Invariants
        var monotonicViolations = 0;
        var frameMismatches = 0;
        var zeroPositionCount = 0L;
        var sysTimeViolations = 0;
        var zeroSysTimeCount = 0L;
        for (var i = 0; i < arr.Length; i++)
        {
            if (arr[i].SamplePosition == 0 && i > 0) zeroPositionCount++;
            if (arr[i].SystemTimeNanoseconds == 0) zeroSysTimeCount++;
            if (i > 0)
            {
                var delta = arr[i].SamplePosition - arr[i - 1].SamplePosition;
                if (delta <= 0) monotonicViolations++;
                if (delta != arr[i].Frames) frameMismatches++;
                if (arr[i].SystemTimeNanoseconds <= arr[i - 1].SystemTimeNanoseconds) sysTimeViolations++;
            }
        }

        var first = arr[0];
        var last = arr[^1];
        var audioSeconds = (last.SamplePosition - first.SamplePosition) / (double)sampleRate;
        var driverHostSeconds = (last.SystemTimeNanoseconds - first.SystemTimeNanoseconds) / 1e9;
        var stopwatchSeconds = (last.StopwatchTicks - first.StopwatchTicks) / (double)Stopwatch.Frequency;
        var driverDriftMs = (audioSeconds - driverHostSeconds) * 1000.0;
        var stopwatchDriftMs = (audioSeconds - stopwatchSeconds) * 1000.0;

        AnsiConsole.MarkupLine($"\n[bold]Drift over {audioSeconds:0.000} s of audio:[/]");
        AnsiConsole.MarkupLine($"  audio − driver_host: [{(Math.Abs(driverDriftMs) < 50 ? "green" : "red")}]{driverDriftMs:+0.000;-0.000} ms[/]");
        AnsiConsole.MarkupLine($"  audio − stopwatch:   [{(Math.Abs(stopwatchDriftMs) < 50 ? "green" : "red")}]{stopwatchDriftMs:+0.000;-0.000} ms[/]");
        AnsiConsole.MarkupLine("  [dim](< 50 ms over 10 s is healthy; soundcards run on their own crystal so some drift is expected)[/]");

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["callbacks"] = arr.Length.ToString(),
            ["sampleRate"] = sampleRate.ToString(),
            ["monotonicViolations"] = monotonicViolations.ToString(),
            ["frameMismatches"] = frameMismatches.ToString(),
            ["zeroPositionCount"] = zeroPositionCount.ToString(),
            ["sysTimeViolations"] = sysTimeViolations.ToString(),
            ["zeroSysTimeCount"] = zeroSysTimeCount.ToString(),
            ["audioSeconds"] = audioSeconds.ToString("F3"),
            ["driverDriftMs"] = driverDriftMs.ToString("F3"),
            ["stopwatchDriftMs"] = stopwatchDriftMs.ToString("F3"),
        };

        var problems = new List<string>();
        if (monotonicViolations > 0) problems.Add($"{monotonicViolations} SamplePosition non-monotonic");
        if (sysTimeViolations > 0) problems.Add($"{sysTimeViolations} SystemTime non-monotonic");
        if (Math.Abs(driverDriftMs) >= 50) problems.Add($"driver-clock drift {driverDriftMs:F1}ms > 50ms — likely byte-order issue");

        if (cancelled) return TestResult.Skipped("Cancelled", diagnostics);
        if (problems.Count > 0) return TestResult.Fail(string.Join("; ", problems), diagnostics);
        return TestResult.Pass(
            $"{arr.Length} callbacks over {audioSeconds:F2}s, driver drift {driverDriftMs:+0.0;-0.0;0} ms",
            diagnostics);
    }

    private readonly record struct Sample(
        long SamplePosition, long SystemTimeNanoseconds, long StopwatchTicks,
        int Frames, double Speed, AsioTimeCodeInfo? TimeCode);
}
