using NAudio.SoundFile;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.SoundFile.Tests;

/// <summary>
/// Decodes an input file with <see cref="SoundFileReader"/> and re-encodes it
/// to a chosen format with <see cref="SoundFileWriter"/>, then re-opens the
/// output to verify it. The cross-platform analogue of the
/// <c>MediaFoundation.EncodeToXxx</c> / resample file-in-file-out tests.
/// </summary>
sealed class SoundFileTranscodeTest : IConsoleTest
{
    public string Id => "SoundFile.Transcode";
    public string Description => "Transcode an audio file to another format via libsndfile";
    public MenuPath? MenuLocation =>
        new("Sound File (libsndfile)", "Transcode audio file", Group: "Conversion", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("output", typeof(string), Required: false, Help: "output path (auto if blank)"),
        new("format", typeof(string), Required: false, Default: "Flac",
            Help: "output container/codec", Choices: SoundFileTestHelper.FormatChoices),
        new("quality", typeof(double), Required: false,
            Help: "0..1 — VBR quality (Ogg/Opus/MP3) or FLAC compression level"),
    ];

    public TestResult Run(TestContext ctx)
    {
        if (SoundFileTestHelper.ProbeLibrary() is { } skip)
        {
            return skip;
        }

        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath))
        {
            return TestResult.Fail($"Input not found: {inputPath}");
        }

        var major = SoundFileTestHelper.ToMajor(ctx.Get<string>("format"));
        if (SoundFileTestHelper.RequireCodec(major) is { } codecSkip)
        {
            return codecSkip;
        }

        double? quality = ctx.TryGet<double>("quality", out var q) ? q : null;
        ctx.TryGet<string>("output", out var declared);
        var outputPath = string.IsNullOrWhiteSpace(declared)
            ? Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(inputPath))!,
                Path.GetFileNameWithoutExtension(inputPath) + "_transcoded" + SoundFileTestHelper.ExtensionFor(major))
            : declared!;

        var options = SoundFileTestHelper.OptionsFor(major, quality);

        try
        {
            using var reader = new SoundFileReader(inputPath);
            AnsiConsole.MarkupLine($"[grey]Input:[/]    {Markup.Escape(Path.GetFileName(inputPath))}");
            AnsiConsole.MarkupLine($"[grey]Format:[/]   {reader.WaveFormat}");
            AnsiConsole.MarkupLine($"[grey]Duration:[/] {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
            AnsiConsole.MarkupLine($"[grey]Target:[/]   {major}{(quality is { } qq ? $" @ {qq:0.0#}" : "")}");
            AnsiConsole.MarkupLine($"[grey]Output:[/]   {Markup.Escape(outputPath)}");
            AnsiConsole.WriteLine();

            void DoEncode() => SoundFileWriter.CreateSoundFile(outputPath, reader, major, options);
            if (ctx.Interactive)
            {
                AnsiConsole.Status().Spinner(Spinner.Known.Dots)
                    .Start($"Transcoding to {major}...", _ => DoEncode());
            }
            else
            {
                DoEncode();
            }
        }
        catch (ArgumentException ex)
        {
            // Unsupported format/rate combination (e.g. Opus at 44.1 kHz),
            // or libsndfile rejected the format — surface the clear message.
            return TestResult.Fail(ex.Message);
        }
        catch (SoundFileException ex)
        {
            return TestResult.Fail($"libsndfile error: {ex.Message}");
        }

        var info = new FileInfo(outputPath);
        using var verify = new SoundFileReader(outputPath);
        AnsiConsole.MarkupLine($"[green]Transcoded[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]     {info.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verify.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verify.TotalTime:hh\\:mm\\:ss\\.fff}");

        return TestResult.Pass(
            $"Transcoded to {major}, {info.Length:N0} bytes",
            new Dictionary<string, string>
            {
                ["outputPath"] = outputPath,
                ["outputBytes"] = info.Length.ToString(),
                ["format"] = major.ToString(),
                ["outputDurationMs"] = verify.TotalTime.TotalMilliseconds.ToString("F0"),
            });
    }
}
