using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Dsp.Tests;

/// <summary>
/// Writes a quiet sine WAV in a chosen format, so batch plans can generate their own fixtures
/// rather than depending on files that only exist on one machine. Each channel gets its own
/// frequency, which makes a channel swap or drop visible when the output is inspected. Formats
/// above 16 bits or 2 channels are written as WAVE_FORMAT_EXTENSIBLE, the shape that exercises
/// the extensible marshalling paths downstream.
/// </summary>
internal sealed class DspGenerateTestSignalTest : IConsoleTest
{
    public string Id => "Dsp.GenerateTestSignal";
    public string Description => "Write a quiet multi-frequency sine WAV (fixture for batch plans)";
    public MenuPath? MenuLocation => new("DSP", "Generate test signal WAV");

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("output", typeof(string), Required: true, Help: "output WAV path"),
        new("sampleRate", typeof(int), Required: false, Default: 44100, Help: "sample rate in Hz"),
        new("bitsPerSample", typeof(int), Required: false, Default: 16, Help: "16, 24 or 32 (PCM) bits per sample",
            Choices: ["16", "24", "32"]),
        new("float", typeof(bool), Required: false, Default: false, Help: "write 32-bit IEEE float instead of PCM"),
        new("channels", typeof(int), Required: false, Default: 2, Help: "channel count"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(3), Help: "signal length"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var outputPath = ctx.Get<string>("output");
        var sampleRate = ctx.Get<int>("sampleRate");
        var bits = ctx.Get<int>("bitsPerSample");
        var isFloat = ctx.Get<bool>("float");
        var channels = ctx.Get<int>("channels");
        var duration = ctx.Get<TimeSpan>("duration");
        if (channels < 1) return TestResult.Fail("channels must be at least 1");
        if (!isFloat && bits is not (16 or 24 or 32)) return TestResult.Fail($"Unsupported bitsPerSample {bits}");

        WaveFormat format = isFloat
            ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels)
            : bits > 16 || channels > 2
                ? new WaveFormatExtensible(sampleRate, bits, channels)
                : new WaveFormat(sampleRate, bits, channels);

        var parent = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

        var frames = (long)(duration.TotalSeconds * sampleRate);
        var frame = new float[channels];
        using (var writer = new WaveFileWriter(outputPath, format))
        {
            for (long n = 0; n < frames; n++)
            {
                for (var ch = 0; ch < channels; ch++)
                {
                    // 220Hz, 330Hz, 440Hz... at -12dBFS
                    frame[ch] = 0.25f * MathF.Sin(2 * MathF.PI * 110 * (ch + 2) * n / sampleRate);
                }
                writer.WriteSamples(frame, 0, channels);
            }
        }

        var info = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[grey]Wrote {info.Length:N0} bytes:[/] {Markup.Escape(format.ToString())}");
        return TestResult.Pass($"Wrote {duration.TotalSeconds:F1}s {format}", new Dictionary<string, string>
        {
            ["outputPath"] = info.FullName,
            ["outputBytes"] = info.Length.ToString(),
            ["format"] = format.ToString(),
        });
    }
}
