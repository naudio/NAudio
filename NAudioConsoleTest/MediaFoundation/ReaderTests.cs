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

    /// <summary>
    /// Same shape as <see cref="ReadAudioFile"/> but with
    /// <c>RepositionInRead = false</c>, so <c>Position.set</c> calls
    /// <c>Reposition()</c> directly instead of deferring to the next Read. Exercises
    /// the code path that surfaced the pre-existing bug at MediaFoundationReader.cs:389
    /// (Reposition was using the stale <c>repositionTo</c> field instead of the
    /// <c>desiredPosition</c> parameter, so seeks silently went to position 0).
    /// </summary>
    public static void ReadAudioFileWithImmediateReposition()
    {
        AnsiConsole.MarkupLine("[bold]Read audio file with RepositionInRead=false[/]\n");
        AnsiConsole.MarkupLine("[dim]The default reader defers seeks to the next Read call. " +
                               "RepositionInRead=false makes Position.set call SetCurrentPosition " +
                               "immediately — a code path the default 'Read audio file' test " +
                               "does not exercise.[/]\n");

        var filePath = AudioFileSelector.SelectAudioFile();
        if (filePath == null) return;

        MediaFoundationApi.Startup();

        using var reader = new MediaFoundationReader(filePath,
            new MediaFoundationReader.MediaFoundationReaderSettings { RepositionInRead = false });

        AnsiConsole.MarkupLine($"[grey]File:[/]        {Markup.Escape(Path.GetFileName(filePath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Length:[/]      {reader.Length:N0} bytes");

        if (reader.Length == 0)
        {
            AnsiConsole.MarkupLine("\n[yellow]Stream has no length (live source?) — cannot seek; aborting.[/]");
            AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];

        var startBytes = reader.Read(buffer, 0, buffer.Length);
        AnsiConsole.MarkupLine($"\n[green]Read {startBytes:N0} bytes from start[/]");

        // Trigger immediate Reposition. With the pre-fix bug, nsPosition was
        // computed from the stale repositionTo field (-1) instead of the
        // desiredPosition parameter, so the seek silently went to position 0
        // — and the post-seek Read would return data from the start of the
        // file rather than the midpoint.
        var midpoint = reader.Length / 2;
        AnsiConsole.MarkupLine($"[grey]Setting Position = {midpoint:N0} (immediate seek)[/]");
        reader.Position = midpoint;

        var midpointBytes = reader.Read(buffer, 0, buffer.Length);
        AnsiConsole.MarkupLine($"[green]Read {midpointBytes:N0} bytes from midpoint[/]");
        AnsiConsole.MarkupLine($"[grey]Position after read:[/] {reader.Position:N0}");

        var expectedPosition = midpoint + midpointBytes;
        if (reader.Position == expectedPosition)
        {
            AnsiConsole.MarkupLine($"\n[green][[PASS]][/] Position advanced to {reader.Position:N0} " +
                                   $"as expected — seek went to midpoint, not start.");
        }
        else
        {
            AnsiConsole.MarkupLine($"\n[red][[FAIL]][/] Position mismatch — expected " +
                                   $"{expectedPosition:N0}, got {reader.Position:N0}. " +
                                   $"Seek did not land at requested position.");
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
