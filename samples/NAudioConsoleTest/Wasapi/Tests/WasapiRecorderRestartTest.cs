using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Starts and stops one <see cref="WasapiRecorder"/> several times, checking each cycle delivers
/// audio and raises <see cref="WasapiRecorder.RecordingStopped"/> cleanly. Regression check for
/// #1479, where a second <c>StartRecording</c> re-initialized the audio client and failed.
/// Nothing is written to disk.
/// </summary>
internal sealed class WasapiRecorderRestartTest : IConsoleTest
{
    public string Id => "Wasapi.RecorderRestart";
    public string Description => "Start/stop the same WasapiRecorder several times (#1479 regression)";
    public MenuPath? MenuLocation => new("WASAPI", "Restart recorder", Group: "Recorder", Order: 2);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("captureDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "capture endpoint friendly name or 'default'",
            ChoiceProvider: WasapiDevices.CaptureDeviceNames),
        new("cycles", typeof(int), Required: false, Default: 3, Help: "number of start/stop cycles"),
        new("cycleDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(1),
            Help: "how long to record in each cycle"),
        new("eventSync", typeof(bool), Required: false, Default: true,
            Help: "event-driven capture (false = polling)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var captureName = ctx.Get<string>("captureDevice");
        var captureDevice = WasapiDevices.ResolveCapture(captureName);
        if (captureDevice is null) return TestResult.Fail($"Capture device not found: {captureName}");

        var cycles = ctx.Get<int>("cycles");
        if (cycles < 2) return TestResult.Fail("cycles must be at least 2 to exercise a restart");
        var cycleDuration = ctx.Get<TimeSpan>("cycleDuration");
        var eventSync = ctx.Get<bool>("eventSync");

        AnsiConsole.MarkupLine($"[grey]Device:[/] {Markup.Escape(captureDevice.FriendlyName)}");
        AnsiConsole.MarkupLine($"[grey]Cycles:[/] {cycles} x {cycleDuration.TotalSeconds:F1}s ({(eventSync ? "event" : "polling")} sync)");
        AnsiConsole.WriteLine();

        var builder = new WasapiRecorderBuilder()
            .WithDevice(captureDevice)
            .WithSharedMode();
        if (eventSync) builder.WithEventSync();
        using var recorder = builder.Build();

        long cycleBytes = 0;
        recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
            Interlocked.Add(ref cycleBytes, buffer.Length);

        using var stopped = new AutoResetEvent(false);
        Exception? stopException = null;
        recorder.RecordingStopped += (_, e) =>
        {
            stopException = e.Exception;
            stopped.Set();
        };

        var diagnostics = new Dictionary<string, string>
        {
            ["captureDevice"] = captureDevice.FriendlyName,
            ["format"] = recorder.WaveFormat.ToString(),
        };

        for (var cycle = 1; cycle <= cycles; cycle++)
        {
            Interlocked.Exchange(ref cycleBytes, 0);
            try
            {
                recorder.StartRecording();
            }
            catch (Exception ex)
            {
                diagnostics["failedCycle"] = cycle.ToString();
                return TestResult.Fail($"StartRecording threw on cycle {cycle}: {ex.GetType().Name}: {ex.Message}", diagnostics);
            }

            if (ctx.Cancellation.WaitHandle.WaitOne(cycleDuration))
            {
                recorder.StopRecording();
                stopped.WaitOne(TimeSpan.FromSeconds(5));
                return TestResult.Skipped($"Cancelled during cycle {cycle}", diagnostics);
            }

            var sw = Stopwatch.StartNew();
            recorder.StopRecording();
            if (!stopped.WaitOne(TimeSpan.FromSeconds(5)))
            {
                diagnostics["failedCycle"] = cycle.ToString();
                return TestResult.Fail($"RecordingStopped did not fire within 5s on cycle {cycle}", diagnostics);
            }
            var stopMs = sw.ElapsedMilliseconds;

            var bytes = Interlocked.Read(ref cycleBytes);
            diagnostics[$"cycle{cycle}Bytes"] = bytes.ToString();
            diagnostics[$"cycle{cycle}StopMs"] = stopMs.ToString();
            AnsiConsole.MarkupLine($"  cycle {cycle}: {bytes:N0} bytes, stopped in {stopMs}ms");

            if (stopException != null)
            {
                diagnostics["failedCycle"] = cycle.ToString();
                return TestResult.Fail($"RecordingStopped reported {stopException.GetType().Name} on cycle {cycle}: {stopException.Message}", diagnostics);
            }
            if (bytes == 0)
            {
                diagnostics["failedCycle"] = cycle.ToString();
                return TestResult.Fail($"No audio delivered on cycle {cycle}", diagnostics);
            }
            if (recorder.CaptureState != CaptureState.Stopped)
            {
                diagnostics["failedCycle"] = cycle.ToString();
                return TestResult.Fail($"CaptureState is {recorder.CaptureState} after cycle {cycle} stopped", diagnostics);
            }
        }

        return TestResult.Pass($"{cycles} start/stop cycles all delivered audio", diagnostics);
    }
}
