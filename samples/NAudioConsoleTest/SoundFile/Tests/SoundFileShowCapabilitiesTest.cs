using NAudio.SoundFile;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.SoundFile.Tests;

/// <summary>
/// Reports the loaded libsndfile version, every major format it can produce,
/// and a FLAC/Vorbis/Opus/MP3 support matrix. Doubles as the canonical
/// "is libsndfile installed and which codecs does this build have?" probe.
/// </summary>
internal sealed class SoundFileShowCapabilitiesTest : IConsoleTest
{
    public string Id => "SoundFile.ShowCapabilities";
    public string Description => "Show libsndfile version and supported formats/codecs";
    public MenuPath? MenuLocation =>
        new("Sound File (libsndfile)", "Show capabilities", Group: "Diagnostics", Order: 0);

    public IReadOnlyList<TestParameter> Parameters => [];

    public TestResult Run(TestContext ctx)
    {
        if (SoundFileTestHelper.ProbeLibrary() is { } skip)
        {
            return skip;
        }

        var version = SoundFileCapabilities.LibraryVersion;
        AnsiConsole.MarkupLine($"[grey]Library:[/] {Markup.Escape(version)}");
        AnsiConsole.WriteLine();

        var majors = SoundFileCapabilities.GetSupportedMajorFormats();
        AnsiConsole.MarkupLine($"[grey]Major formats ({majors.Count}):[/]");
        foreach (var f in majors)
        {
            AnsiConsole.MarkupLine($"  {Markup.Escape(f.ToString())}");
        }
        AnsiConsole.WriteLine();

        var codecs = new[]
        {
            SoundFileMajorFormat.Wav, SoundFileMajorFormat.Aiff, SoundFileMajorFormat.Flac,
            SoundFileMajorFormat.OggVorbis, SoundFileMajorFormat.Opus, SoundFileMajorFormat.Mp3,
        };
        var diagnostics = new Dictionary<string, string> { ["libraryVersion"] = version };
        AnsiConsole.MarkupLine("[grey]Codec support:[/]");
        foreach (var c in codecs)
        {
            bool ok = SoundFileCapabilities.IsFormatSupported(c);
            diagnostics[c.ToString()] = ok ? "yes" : "no";
            AnsiConsole.MarkupLine($"  {(ok ? "[green]✓[/]" : "[red]✗[/]")} {c}");
        }

        return TestResult.Pass($"{version}, {majors.Count} major formats", diagnostics);
    }
}
