using NAudio.SoundFile;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.SoundFile.Tests;

/// <summary>
/// Encodes a generated sine into an in-memory <see cref="MemoryStream"/> and
/// decodes it back from the same stream — exercising libsndfile's virtual
/// I/O (<c>sf_open_virtual</c>) on both the write and read legs.
/// </summary>
internal sealed class SoundFileStreamRoundTripTest : IConsoleTest
{
    public string Id => "SoundFile.StreamRoundTrip";
    public string Description => "Round-trip a generated tone through a MemoryStream (virtual I/O)";
    public MenuPath? MenuLocation =>
        new("Sound File (libsndfile)", "Round-trip through a MemoryStream", Group: "Conversion", Order: 2);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("format", typeof(string), Required: false, Default: "OggVorbis",
            Help: "container/codec to round-trip", Choices: SoundFileTestHelper.FormatChoices),
        new("seconds", typeof(int), Required: false, Default: 2, Help: "tone length in seconds"),
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
        // 48 kHz keeps every codec (incl. Opus) valid.
        var sine = new SineWaveSource(440f, 0.3f, 48000, 2);

        try
        {
            using var ms = new MemoryStream();
            using (var writer = new SoundFileWriter(ms, sine.WaveFormat, major,
                SoundFileTestHelper.OptionsFor(major, quality: 0.6)))
            {
                SoundFileTestHelper.PumpSine(sine, writer, seconds);
            }

            long encodedBytes = ms.Length;
            ms.Position = 0;
            using var reader = new SoundFileReader(ms);
            var (frames, rms) = SoundFileTestHelper.DrainAndMeasure(reader);

            long expected = (long)seconds * 48000;
            long tolerance = 48000 / 4;
            AnsiConsole.MarkupLine($"[grey]Format:[/] {major} — encoded {encodedBytes:N0} bytes in memory");
            AnsiConsole.MarkupLine($"[grey]Frames:[/] {frames:N0} (expected ~{expected:N0}), RMS {rms:0.0000}");

            var diag = new Dictionary<string, string>
            {
                ["format"] = major.ToString(),
                ["encodedBytes"] = encodedBytes.ToString(),
                ["frames"] = frames.ToString(),
                ["rms"] = rms.ToString("0.0000"),
            };

            if (encodedBytes == 0)
            {
                return TestResult.Fail("Nothing was written to the MemoryStream", diag);
            }
            if (rms <= 0.01)
            {
                return TestResult.Fail($"Decoded signal is silent (RMS {rms:0.0000})", diag);
            }
            if (Math.Abs(frames - expected) > tolerance)
            {
                return TestResult.Fail(
                    $"Frame count {frames:N0} off expected {expected:N0} by more than {tolerance:N0}", diag);
            }
            return TestResult.Pass($"{major} MemoryStream round-trip OK ({frames:N0} frames)", diag);
        }
        catch (ArgumentException ex)
        {
            return TestResult.Fail(ex.Message);
        }
        catch (SoundFileException ex)
        {
            return TestResult.Fail($"libsndfile error: {ex.Message}");
        }
    }
}
