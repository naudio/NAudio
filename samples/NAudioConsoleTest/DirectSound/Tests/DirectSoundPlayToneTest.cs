using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.DirectSound.Tests;

/// <summary>
/// Plays a short low-volume sine wave through <see cref="DirectSoundOut"/>. Audible signal so
/// the user can verify "yes, DirectSound playback still works end-to-end" — exercises
/// <c>DirectSoundCreate</c>, the primary + secondary buffer pair, the IDirectSoundNotify QI
/// cascade, the playback notification thread, and the Feed loop.
/// </summary>
sealed class DirectSoundPlayToneTest : IConsoleTest
{
    public string Id => "DirectSound.PlayTone";
    public string Description => "Play a sine tone through DirectSoundOut (low volume)";
    public MenuPath? MenuLocation => new("DirectSound", "Play tone (DirectSoundOut)", Group: "Playback", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("frequency", typeof(double), Required: false, Default: 440.0, Help: "tone frequency in Hz"),
        new("gain", typeof(double), Required: false, Default: 0.10, Help: "tone amplitude (0..1)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(2),
            Help: "playback duration"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var frequency = ctx.Get<double>("frequency");
        var gain = ctx.Get<double>("gain");
        var duration = ctx.Get<TimeSpan>("duration");

        var tone = new SignalGenerator(44100, 1)
        {
            Frequency = frequency,
            Gain = gain,
            Type = SignalGeneratorType.Sin,
        }.Take(duration).ToWaveProvider();

        using var dsoundOut = new DirectSoundOut(40);
        dsoundOut.Init(tone);

        AnsiConsole.MarkupLine(
            $"[green]Playing {frequency:F0} Hz tone for {duration.TotalSeconds:F1}s[/] " +
            $"[dim](gain={gain:F2}, {(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");
        dsoundOut.Play();

        while (dsoundOut.PlaybackState != PlaybackState.Stopped)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
            {
                dsoundOut.Stop();
                break;
            }
            if (ctx.Cancellation.WaitHandle.WaitOne(50))
            {
                dsoundOut.Stop();
                break;
            }
        }

        var cancelled = ctx.Cancellation.IsCancellationRequested;
        return cancelled
            ? TestResult.Skipped("Cancelled")
            : TestResult.Pass($"Played {duration.TotalSeconds:F1}s tone at {frequency:F0} Hz",
                new Dictionary<string, string>
                {
                    ["frequencyHz"] = frequency.ToString("F0"),
                    ["gain"] = gain.ToString("F3"),
                    ["durationMs"] = duration.TotalMilliseconds.ToString("F0"),
                });
    }
}
