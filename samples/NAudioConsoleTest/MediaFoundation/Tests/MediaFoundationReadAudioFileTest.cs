using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Reads an audio file end-to-end via <see cref="MediaFoundationReader"/>, then seeks to the
/// midpoint and reads more to verify repositioning works.
/// </summary>
internal sealed class MediaFoundationReadAudioFileTest : IConsoleTest
{
    public string Id => "MediaFoundation.ReadAudioFile";
    public string Description => "Read an audio file via MediaFoundationReader (full decode + midpoint seek)";
    public MenuPath? MenuLocation =>
        new("Media Foundation", "Read audio file (MediaFoundationReader)", Group: "Reading", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path", IsFilePath: true, FileCategory: "audio"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var filePath = ctx.Get<string>("input");
        if (!File.Exists(filePath))
            return TestResult.Fail($"Input not found: {filePath}");

        MediaFoundationApi.Startup();
        using var reader = new MediaFoundationReader(filePath);

        AnsiConsole.MarkupLine($"[grey]File:[/]        {Markup.Escape(Path.GetFileName(filePath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Encoding:[/]    {reader.WaveFormat.Encoding}");
        AnsiConsole.MarkupLine($"[grey]Channels:[/]    {reader.WaveFormat.Channels}");
        AnsiConsole.MarkupLine($"[grey]Sample rate:[/] {reader.WaveFormat.SampleRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Bits/sample:[/] {reader.WaveFormat.BitsPerSample}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Length:[/]      {reader.Length:N0} bytes");
        AnsiConsole.WriteLine();

        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
        long totalBytesRead = 0;
        int reads = 0;

        void DoRead()
        {
            int bytesRead;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;
                reads++;
            }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Reading entire file...", _ => DoRead());
        else
            DoRead();

        AnsiConsole.MarkupLine($"[green]Read {totalBytesRead:N0} bytes in {reads} reads[/]");

        int bytesAfterSeek = 0;
        long midpoint = 0;
        if (reader.Length > 0)
        {
            midpoint = reader.Length / 2;
            reader.Position = midpoint;
            bytesAfterSeek = reader.Read(buffer, 0, buffer.Length);
            AnsiConsole.MarkupLine($"[green]Seek to midpoint ({midpoint:N0}): read {bytesAfterSeek} bytes[/]");
        }

        return TestResult.Pass(
            $"Read {totalBytesRead:N0} bytes",
            new Dictionary<string, string>
            {
                ["totalBytes"] = totalBytesRead.ToString(),
                ["reads"] = reads.ToString(),
                ["midpointBytes"] = bytesAfterSeek.ToString(),
                ["length"] = reader.Length.ToString(),
            });
    }
}
