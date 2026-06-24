using NAudio.SoundFile;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.SoundFile.Tests;

/// <summary>
/// Self-contained correctness check needing no input file: generates a sine,
/// encodes it to a temp file in the chosen format, decodes it back, and
/// asserts the frame count is about right and the signal is non-silent.
/// Good for batch/CI-style runs.
/// </summary>
internal sealed class SoundFileRoundTripTest : IConsoleTest
{
    public string Id => "SoundFile.RoundTrip";
    public string Description => "Encode a generated sine then decode it back (no input file needed)";
    public MenuPath? MenuLocation =>
        new("Sound File (libsndfile)", "Round-trip a generated tone", Group: "Conversion", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("format", typeof(string), Required: false, Default: "Flac",
            Help: "container/codec to round-trip", Choices: SoundFileTestHelper.FormatChoices),
        new("seconds", typeof(int), Required: false, Default: 2, Help: "tone length in seconds"),
        new("sampleRate", typeof(int), Required: false, Default: 48000,
            Help: "sample rate (Opus requires 8000/12000/16000/24000/48000)",
            Choices: ["16000", "24000", "44100", "48000"]),
    ];

    public TestResult Run(TestContext ctx)
    {
        if (SoundFileTestHelper.ProbeLibrary() is { } skip)
        {
            return skip;
        }

        var major = SoundFileTestHelper.ToMajor(ctx.Get<string>("format"));
        if (SoundFileTestHelper.RequireCodec(major) is { } codecSkip)
        {
            return codecSkip;
        }

        var seconds = ctx.Get<int>("seconds");
        var sampleRate = ctx.Get<int>("sampleRate");
        var sine = new SineWaveSource(440f, 0.3f, sampleRate, 2);
        var tempPath = Path.Combine(Path.GetTempPath(),
            "naudio-sf-rt-" + Guid.NewGuid().ToString("N") + SoundFileTestHelper.ExtensionFor(major));

        try
        {
            using (var writer = new SoundFileWriter(tempPath, sine.WaveFormat, major,
                SoundFileTestHelper.OptionsFor(major, quality: 0.6)))
            {
                SoundFileTestHelper.PumpSine(sine, writer, seconds);
            }

            long bytes = new FileInfo(tempPath).Length;
            using var reader = new SoundFileReader(tempPath);
            var (frames, rms) = SoundFileTestHelper.DrainAndMeasure(reader);

            long expected = (long)seconds * sampleRate;
            // Lossy codecs add encoder/decoder delay + padding; allow ~0.25 s.
            long tolerance = sampleRate / 4;
            bool framesOk = Math.Abs(frames - expected) <= tolerance;
            bool signalOk = rms > 0.01;

            AnsiConsole.MarkupLine($"[grey]Format:[/]   {major} ({bytes:N0} bytes)");
            AnsiConsole.MarkupLine($"[grey]Frames:[/]   {frames:N0} (expected ~{expected:N0})");
            AnsiConsole.MarkupLine($"[grey]RMS:[/]      {rms:0.0000}");

            var diag = new Dictionary<string, string>
            {
                ["format"] = major.ToString(),
                ["encodedBytes"] = bytes.ToString(),
                ["frames"] = frames.ToString(),
                ["expectedFrames"] = expected.ToString(),
                ["rms"] = rms.ToString("0.0000"),
            };

            if (!signalOk)
            {
                return TestResult.Fail($"Decoded signal is silent (RMS {rms:0.0000})", diag);
            }
            if (!framesOk)
            {
                return TestResult.Fail(
                    $"Frame count {frames:N0} off expected {expected:N0} by more than {tolerance:N0}", diag);
            }
            return TestResult.Pass($"{major} round-trip OK ({frames:N0} frames, RMS {rms:0.000})", diag);
        }
        catch (ArgumentException ex)
        {
            return TestResult.Fail(ex.Message);
        }
        catch (SoundFileException ex)
        {
            return TestResult.Fail($"libsndfile error: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
        }
    }
}
