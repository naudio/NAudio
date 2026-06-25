using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Decodes an audio file via <see cref="StreamMediaFoundationReader"/>, which wraps the input
/// as an <c>IStream</c> CCW rather than letting MF open the file directly.
/// </summary>
internal sealed class MediaFoundationReadFromStreamTest : IConsoleTest
{
    public string Id => "MediaFoundation.ReadFromStream";
    public string Description => "Decode an audio file via StreamMediaFoundationReader (IStream CCW)";
    public MenuPath? MenuLocation =>
        new("Media Foundation", "Read from stream (StreamMediaFoundationReader)", Group: "Reading", Order: 2);

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

        using var fileStream = File.OpenRead(filePath);
        using var reader = new StreamMediaFoundationReader(fileStream);

        AnsiConsole.MarkupLine($"[grey]File:[/]        {Markup.Escape(Path.GetFileName(filePath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Channels:[/]    {reader.WaveFormat.Channels}");
        AnsiConsole.MarkupLine($"[grey]Sample rate:[/] {reader.WaveFormat.SampleRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Bits/sample:[/] {reader.WaveFormat.BitsPerSample}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.WriteLine();

        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
        long totalBytesRead = 0;

        void DoRead()
        {
            int bytesRead;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;
            }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Reading entire stream...", _ => DoRead());
        else
            DoRead();

        AnsiConsole.MarkupLine($"[green]Read {totalBytesRead:N0} bytes from stream[/]");

        return TestResult.Pass(
            $"Read {totalBytesRead:N0} bytes",
            new Dictionary<string, string> { ["totalBytes"] = totalBytesRead.ToString() });
    }
}
