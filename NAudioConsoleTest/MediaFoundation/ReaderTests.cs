using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation;

static class ReaderTests
{
    public static void ReadAudioFile()
    {
        AnsiConsole.MarkupLine("[bold]Read Audio File with MediaFoundationReader[/]\n");

        var filePath = AudioFileSelector.SelectAudioFile();
        if (filePath == null) return;

        MediaFoundationApi.Startup();

        using var reader = new MediaFoundationReader(filePath);

        AnsiConsole.MarkupLine($"[grey]File:[/]        {Markup.Escape(Path.GetFileName(filePath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Encoding:[/]    {reader.WaveFormat.Encoding}");
        AnsiConsole.MarkupLine($"[grey]Channels:[/]    {reader.WaveFormat.Channels}");
        AnsiConsole.MarkupLine($"[grey]Sample rate:[/] {reader.WaveFormat.SampleRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Bits/sample:[/] {reader.WaveFormat.BitsPerSample}");

        var duration = reader.TotalTime;
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {duration:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Length:[/]      {reader.Length:N0} bytes");

        // Read through the file to verify it decodes successfully
        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
        long totalBytesRead = 0;
        int reads = 0;

        AnsiConsole.MarkupLine("");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Reading entire file...", ctx =>
            {
                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytesRead += bytesRead;
                    reads++;
                }
            });

        AnsiConsole.MarkupLine($"[green]Successfully read {totalBytesRead:N0} bytes in {reads} reads[/]");

        // Test repositioning
        if (reader.Length > 0)
        {
            var midpoint = reader.Length / 2;
            reader.Position = midpoint;
            var bytesAfterSeek = reader.Read(buffer, 0, buffer.Length);
            AnsiConsole.MarkupLine($"[green]Seek to midpoint ({midpoint:N0}): read {bytesAfterSeek} bytes[/]");
        }

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }

    public static void ReadFromStream()
    {
        AnsiConsole.MarkupLine("[bold]Read from Stream with StreamMediaFoundationReader[/]\n");

        var filePath = AudioFileSelector.SelectAudioFile();
        if (filePath == null) return;

        MediaFoundationApi.Startup();

        using var fileStream = File.OpenRead(filePath);
        using var reader = new StreamMediaFoundationReader(fileStream);

        AnsiConsole.MarkupLine($"[grey]File:[/]        {Markup.Escape(Path.GetFileName(filePath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Channels:[/]    {reader.WaveFormat.Channels}");
        AnsiConsole.MarkupLine($"[grey]Sample rate:[/] {reader.WaveFormat.SampleRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Bits/sample:[/] {reader.WaveFormat.BitsPerSample}");

        var duration = reader.TotalTime;
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {duration:hh\\:mm\\:ss\\.fff}");

        // Read through the file
        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
        long totalBytesRead = 0;

        AnsiConsole.MarkupLine("");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Reading entire stream...", ctx =>
            {
                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytesRead += bytesRead;
                }
            });

        AnsiConsole.MarkupLine($"[green]Successfully read {totalBytesRead:N0} bytes from stream[/]");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
