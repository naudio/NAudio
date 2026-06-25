using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Encodes the input through MP3, AAC, and WMA to an in-memory <see cref="MemoryStream"/>, then
/// reads the result back via <see cref="StreamMediaFoundationReader"/>. Exercises the full
/// ComStream CCW round-trip — both the write leg
/// (<c>CreateSinkWriter(ComStream, Guid)</c> → <c>MFCreateMFByteStreamOnStream</c> with QI-for-IID
/// on the IStream CCW) and the read leg
/// (<c>CreateByteStream</c> + <c>CreateSourceReaderFromByteStream</c>).
/// </summary>
internal sealed class MediaFoundationRoundTripTest : IConsoleTest
{
    public string Id => "MediaFoundation.RoundTripMemoryStream";
    public string Description => "Round-trip encode/decode through MemoryStream (MP3 + AAC + WMA)";
    public MenuPath? MenuLocation =>
        new("Media Foundation", "Round-trip encode through MemoryStream (MP3 + AAC + WMA)",
            Group: "Encoding", Order: 4);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("bitrate", typeof(int), Required: false, Default: 96000, Help: "encoder bitrate in bps"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath))
            return TestResult.Fail($"Input not found: {inputPath}");

        var bitrate = ctx.Get<int>("bitrate");

        MediaFoundationApi.Startup();

        var formats = new (string Name, Action<IWaveProvider, Stream, int> Encode)[]
        {
            ("MP3", MediaFoundationEncoder.EncodeToMp3),
            ("AAC", MediaFoundationEncoder.EncodeToAac),
            ("WMA", MediaFoundationEncoder.EncodeToWma),
        };

        var diagnostics = new Dictionary<string, string> { ["bitrate"] = bitrate.ToString() };
        var failures = new List<string>();

        foreach (var (name, encode) in formats)
        {
            AnsiConsole.MarkupLine($"\n[bold yellow]-- {name} --[/]");
            try
            {
                // Fresh reader per format because IWaveProvider input gets consumed.
                using var source = new MediaFoundationReader(inputPath);
                using var encoded = new MemoryStream();
                encode(source, encoded, bitrate);
                AnsiConsole.MarkupLine($"  [green]Encoded:[/] {encoded.Length:N0} bytes (CCW write leg)");
                diagnostics[$"{name}_encodedBytes"] = encoded.Length.ToString();

                encoded.Position = 0;
                using var verifyReader = new StreamMediaFoundationReader(encoded);
                var buffer = new byte[verifyReader.WaveFormat.AverageBytesPerSecond];
                long total = 0;
                int bytesRead;
                while ((bytesRead = verifyReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += bytesRead;
                }
                AnsiConsole.MarkupLine($"  [green]Decoded:[/] {total:N0} bytes PCM via " +
                                       $"{Markup.Escape(verifyReader.WaveFormat.ToString())} (CCW read leg)");
                diagnostics[$"{name}_decodedBytes"] = total.ToString();
            }
            catch (Exception ex)
            {
                var failMsg = $"{ex.GetType().Name}: {ex.Message}";
                AnsiConsole.MarkupLine($"  [red]FAILED:[/] {Markup.Escape(failMsg)}");
                failures.Add($"{name}: {failMsg}");
                diagnostics[$"{name}_status"] = "FAIL";
            }
        }

        return failures.Count == 0
            ? TestResult.Pass("All three formats round-tripped through MemoryStream", diagnostics)
            : TestResult.Fail(string.Join("; ", failures), diagnostics);
    }
}
