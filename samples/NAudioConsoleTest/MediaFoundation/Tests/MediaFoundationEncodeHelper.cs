using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Shared encode/decode plumbing for the <c>MediaFoundation.EncodeToXxx</c> family of tests.
/// Each format-specific test class is a thin wrapper that supplies its container extension
/// and the appropriate <see cref="MediaFoundationEncoder"/> entry point.
/// </summary>
static class MediaFoundationEncodeHelper
{
    public static IReadOnlyList<TestParameter> LossyParameters { get; } =
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("output", typeof(string), Required: false, Help: "output file path (auto if blank)"),
        new("bitrate", typeof(int), Required: false, Default: 192000, Help: "encoder bitrate in bps"),
    ];

    public static IReadOnlyList<TestParameter> LosslessParameters { get; } =
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("output", typeof(string), Required: false, Help: "output file path (auto if blank)"),
    ];

    public static TestResult RunLossy(TestContext ctx, string formatName, string extension,
        Action<IWaveProvider, string, int> encode)
    {
        if (!TryResolveInput(ctx, extension, out var inputPath, out var outputPath, out var fail))
            return fail!;

        var bitrate = ctx.Get<int>("bitrate");
        MediaFoundationApi.Startup();

        using var reader = new MediaFoundationReader(inputPath);
        PrintInputInfo(inputPath, reader, outputPath);
        AnsiConsole.MarkupLine($"[grey]Bitrate:[/]      {bitrate} bps");
        AnsiConsole.WriteLine();

        Encode(ctx.Interactive, formatName, () => encode(reader, outputPath, bitrate));

        return Finalise(outputPath, bitrate);
    }

    public static TestResult RunLossless(TestContext ctx, string formatName, string extension,
        Action<IWaveProvider, string> encode)
    {
        if (!TryResolveInput(ctx, extension, out var inputPath, out var outputPath, out var fail))
            return fail!;

        MediaFoundationApi.Startup();

        using var reader = new MediaFoundationReader(inputPath);
        PrintInputInfo(inputPath, reader, outputPath);
        AnsiConsole.MarkupLine("[dim]Lossless — no bitrate parameter[/]");
        AnsiConsole.WriteLine();

        Encode(ctx.Interactive, formatName, () => encode(reader, outputPath));

        return Finalise(outputPath, bitrate: null);
    }

    private static bool TryResolveInput(TestContext ctx, string extension,
        out string inputPath, out string outputPath, out TestResult? failure)
    {
        inputPath = ctx.Get<string>("input");
        outputPath = "";
        if (!File.Exists(inputPath))
        {
            failure = TestResult.Fail($"Input not found: {inputPath}");
            return false;
        }

        ctx.TryGet<string>("output", out var declared);
        outputPath = string.IsNullOrWhiteSpace(declared)
            ? Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(inputPath))!,
                Path.GetFileNameWithoutExtension(inputPath) + $"_encoded{extension}")
            : declared!;
        failure = null;
        return true;
    }

    private static void PrintInputInfo(string inputPath, MediaFoundationReader reader, string outputPath)
    {
        AnsiConsole.MarkupLine($"[grey]Input:[/]        {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Input format:[/] {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]     {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Output:[/]       {Markup.Escape(outputPath)}");
    }

    private static void Encode(bool interactive, string formatName, Action encode)
    {
        // Spectre's Status spinner needs a real terminal; fall back to a plain inline run
        // when invoked from CLI/CI.
        if (interactive)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start($"Encoding to {formatName}...", _ => encode());
        }
        else
        {
            encode();
        }
    }

    private static TestResult Finalise(string outputPath, int? bitrate)
    {
        var outputInfo = new FileInfo(outputPath);
        using var verifyReader = new MediaFoundationReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output size:[/]     {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        var diag = new Dictionary<string, string>
        {
            ["outputPath"] = outputPath,
            ["outputBytes"] = outputInfo.Length.ToString(),
            ["outputDurationMs"] = verifyReader.TotalTime.TotalMilliseconds.ToString("F0"),
        };
        if (bitrate is int br) diag["bitrate"] = br.ToString();

        return TestResult.Pass($"Encoded {outputInfo.Length:N0} bytes", diag);
    }
}
