using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Same shape as <see cref="MediaFoundationReadAudioFileTest"/> but with
/// <c>RepositionInRead = false</c>, so <c>Position.set</c> calls <c>Reposition()</c> immediately
/// instead of deferring to the next Read. Exercises the code path that surfaced the pre-existing
/// bug at <c>MediaFoundationReader.cs:389</c> (Reposition was using the stale
/// <c>repositionTo</c> field instead of the <c>desiredPosition</c> parameter, so seeks
/// silently went to position 0).
/// </summary>
internal sealed class MediaFoundationReadImmediateRepositionTest : IConsoleTest
{
    public string Id => "MediaFoundation.ReadImmediateReposition";
    public string Description => "Read with RepositionInRead=false — seek-to-midpoint regression check";
    public MenuPath? MenuLocation =>
        new("Media Foundation",
            "Read audio file with immediate reposition (RepositionInRead=false)",
            Group: "Reading", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var filePath = ctx.Get<string>("input");
        if (!File.Exists(filePath))
            return TestResult.Fail($"Input not found: {filePath}");

        MediaFoundationApi.Startup();

        using var reader = new MediaFoundationReader(filePath,
            new MediaFoundationReader.MediaFoundationReaderSettings { RepositionInRead = false });

        AnsiConsole.MarkupLine($"[grey]File:[/]     {Markup.Escape(Path.GetFileName(filePath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]   {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Length:[/]   {reader.Length:N0} bytes");

        if (reader.Length == 0)
        {
            return TestResult.Skipped("Stream has no length (live source?) — cannot seek");
        }

        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];

        var startBytes = reader.Read(buffer, 0, buffer.Length);
        AnsiConsole.MarkupLine($"\n[green]Read {startBytes:N0} bytes from start[/]");

        // Trigger immediate Reposition. With the pre-fix bug, the post-seek Read would return
        // data from the start of the file rather than the midpoint.
        var midpoint = reader.Length / 2;
        AnsiConsole.MarkupLine($"[grey]Setting Position = {midpoint:N0} (immediate seek)[/]");
        reader.Position = midpoint;

        var midpointBytes = reader.Read(buffer, 0, buffer.Length);
        AnsiConsole.MarkupLine($"[green]Read {midpointBytes:N0} bytes from midpoint[/]");
        AnsiConsole.MarkupLine($"[grey]Position after read:[/] {reader.Position:N0}");

        var expectedPosition = midpoint + midpointBytes;
        var diagnostics = new Dictionary<string, string>
        {
            ["midpoint"] = midpoint.ToString(),
            ["midpointBytes"] = midpointBytes.ToString(),
            ["expectedPosition"] = expectedPosition.ToString(),
            ["actualPosition"] = reader.Position.ToString(),
        };

        return reader.Position == expectedPosition
            ? TestResult.Pass($"Position advanced to {reader.Position:N0} as expected — seek landed at midpoint",
                diagnostics)
            : TestResult.Fail(
                $"Position mismatch — expected {expectedPosition:N0}, got {reader.Position:N0}. " +
                "Seek did not land at requested position.",
                diagnostics);
    }
}
