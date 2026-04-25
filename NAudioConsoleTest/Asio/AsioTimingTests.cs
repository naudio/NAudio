using System.Diagnostics;
using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioTimingTests
{
    public static void ValidateSamplePosition()
    {
        AnsiConsole.MarkupLine("[bold]Validate SamplePosition + SystemTimeNanoseconds[/]\n");
        AnsiConsole.MarkupLine("[grey]Records for ~10s and verifies that the driver-reported sample position[/]");
        AnsiConsole.MarkupLine("[grey]and system time advance correctly. No audio is written to disk.[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        if (device.Capabilities.NbInputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]This driver has no input channels — needed to drive the recording callback.[/]");
            PressAnyKey();
            return;
        }

        int sampleRate = device.CurrentSampleRate;
        device.InitRecording(new AsioRecordingOptions
        {
            InputChannels = [0],
            SampleRate = sampleRate
        });

        var samples = new List<Sample>(capacity: 8192);
        var lockObj = new object();

        device.AudioCaptured += (_, e) =>
        {
            // Snapshot under a lock so the main thread can read the list cleanly after Stop().
            lock (lockObj)
            {
                samples.Add(new Sample(
                    SamplePosition: e.SamplePosition,
                    SystemTimeNanoseconds: e.SystemTimeNanoseconds,
                    StopwatchTicks: Stopwatch.GetTimestamp(),
                    Frames: e.Frames));
            }
        };

        var stopwatchStart = Stopwatch.GetTimestamp();
        device.Start();
        AnsiConsole.MarkupLine("[green]Recording 10s...[/] [dim](ESC = stop early)[/]\n");

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            var remaining = deadline - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            Console.Write($"\r  Remaining: {remaining:ss\\.f}s    ");
            Thread.Sleep(100);
        }
        Console.WriteLine();
        device.Stop();

        AnsiConsole.WriteLine();
        Sample[] arr;
        lock (lockObj) arr = samples.ToArray();

        if (arr.Length < 4)
        {
            AnsiConsole.MarkupLine($"[red]Only {arr.Length} callbacks captured — driver isn't producing audio?[/]");
            PressAnyKey();
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Captured {arr.Length} callbacks at {sampleRate} Hz[/]\n");

        // ---------- First few and last few callbacks for visual sanity ----------
        var table = new Table().AddColumns("#", "SamplePosition", "Δsamples", "SystemTime (ns)", "Δns", "ns/sample");
        for (int i = 0; i < Math.Min(5, arr.Length); i++) AddRow(table, arr, i);
        if (arr.Length > 10) table.AddRow("...", "...", "...", "...", "...", "...");
        for (int i = Math.Max(5, arr.Length - 5); i < arr.Length; i++) AddRow(table, arr, i);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // ---------- Invariant 1: SamplePosition strictly increases by Frames each callback ----------
        int monotonicViolations = 0;
        int frameMismatches = 0;
        long zeroPositionCount = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i].SamplePosition == 0 && i > 0) zeroPositionCount++;
            if (i > 0)
            {
                long delta = arr[i].SamplePosition - arr[i - 1].SamplePosition;
                if (delta <= 0) monotonicViolations++;
                if (delta != arr[i].Frames) frameMismatches++;
            }
        }

        Verdict("SamplePosition strictly increasing",
            monotonicViolations == 0,
            $"{monotonicViolations} violation(s)");

        Verdict("Δsamples per callback equals Frames",
            frameMismatches == 0,
            $"{frameMismatches} callback(s) where Δ != Frames (some drivers batch — small numbers are OK)");

        Verdict("SamplePosition non-zero after first callback",
            zeroPositionCount == 0,
            $"{zeroPositionCount} callbacks reported position 0 (driver did not implement getSamplePosition?)");

        // ---------- Invariant 2: SystemTime strictly increases ----------
        int sysTimeViolations = 0;
        long zeroSysTimeCount = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i].SystemTimeNanoseconds == 0) zeroSysTimeCount++;
            if (i > 0 && arr[i].SystemTimeNanoseconds <= arr[i - 1].SystemTimeNanoseconds)
                sysTimeViolations++;
        }
        Verdict("SystemTimeNanoseconds strictly increasing",
            sysTimeViolations == 0,
            $"{sysTimeViolations} violation(s)");

        Verdict("SystemTimeNanoseconds non-zero",
            zeroSysTimeCount == 0,
            $"{zeroSysTimeCount} callbacks reported system time 0");

        // ---------- Invariant 3: audio clock vs host clock (THE killer test) ----------
        // Over the full recording, audio_seconds should match host_seconds within a few ms.
        // If byte order were wrong, audio_seconds would be off by 2^32 — drift would be galactic.
        var first = arr[0];
        var last = arr[^1];
        double audioSeconds = (last.SamplePosition - first.SamplePosition) / (double)sampleRate;
        double driverHostSeconds = (last.SystemTimeNanoseconds - first.SystemTimeNanoseconds) / 1e9;
        double stopwatchSeconds = (last.StopwatchTicks - first.StopwatchTicks) / (double)Stopwatch.Frequency;

        double driverDriftMs = (audioSeconds - driverHostSeconds) * 1000.0;
        double stopwatchDriftMs = (audioSeconds - stopwatchSeconds) * 1000.0;

        AnsiConsole.MarkupLine($"\n[bold]Drift over {audioSeconds:0.000} s of audio:[/]");
        AnsiConsole.MarkupLine($"  audio − driver_host: [{(Math.Abs(driverDriftMs) < 50 ? "green" : "red")}]{driverDriftMs:+0.000;-0.000} ms[/]");
        AnsiConsole.MarkupLine($"  audio − stopwatch:   [{(Math.Abs(stopwatchDriftMs) < 50 ? "green" : "red")}]{stopwatchDriftMs:+0.000;-0.000} ms[/]");
        AnsiConsole.MarkupLine($"  [dim](anything < 50 ms over 10 s is healthy; soundcards run on their own crystal so a small steady drift is expected)[/]");

        Verdict("Audio clock vs driver host clock within 50 ms",
            Math.Abs(driverDriftMs) < 50,
            $"drift = {driverDriftMs:0.0} ms — likely byte-order or unit issue");

        // Sanity check: total elapsed should be roughly what we asked for.
        double elapsedSinceStart = (last.StopwatchTicks - stopwatchStart) / (double)Stopwatch.Frequency;
        AnsiConsole.MarkupLine($"\n[grey]Elapsed since Start(): {elapsedSinceStart:0.00} s, captured {arr.Length} callbacks → ~{(arr.Length / elapsedSinceStart):0} cb/s[/]");

        PressAnyKey();
    }

    private static void AddRow(Table table, Sample[] arr, int i)
    {
        long deltaSamples = i == 0 ? 0 : arr[i].SamplePosition - arr[i - 1].SamplePosition;
        long deltaNs = i == 0 ? 0 : arr[i].SystemTimeNanoseconds - arr[i - 1].SystemTimeNanoseconds;
        string nsPerSample = (i == 0 || deltaSamples == 0) ? "—" : $"{deltaNs / (double)deltaSamples:0.0}";
        table.AddRow(
            i.ToString(),
            arr[i].SamplePosition.ToString("N0"),
            i == 0 ? "—" : deltaSamples.ToString("N0"),
            arr[i].SystemTimeNanoseconds.ToString("N0"),
            i == 0 ? "—" : deltaNs.ToString("N0"),
            nsPerSample);
    }

    private static void Verdict(string check, bool pass, string detail)
    {
        if (pass)
            AnsiConsole.MarkupLine($"  [green]PASS[/] {Markup.Escape(check)}");
        else
            AnsiConsole.MarkupLine($"  [red]FAIL[/] {Markup.Escape(check)} — [yellow]{Markup.Escape(detail)}[/]");
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    private readonly record struct Sample(long SamplePosition, long SystemTimeNanoseconds, long StopwatchTicks, int Frames);
}
