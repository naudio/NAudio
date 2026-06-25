using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

internal sealed class WasapiFindBestExclusiveFormatTest : IConsoleTest
{
    public string Id => "Wasapi.FindBestExclusiveFormat";
    public string Description => "Ask GetSupportedExclusiveFormat for the best match to a preferred format";
    public MenuPath? MenuLocation =>
        new("WASAPI", "Find best exclusive format (GetSupportedExclusiveFormat)", Group: "Info", Order: 4);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("rate", typeof(int), Required: false, Default: 48000, Help: "preferred sample rate",
            Choices: ["44100", "48000", "88200", "96000", "176400", "192000"]),
        new("bits", typeof(int), Required: false, Default: 16, Help: "preferred bit depth",
            Choices: ["16", "24", "32"]),
        new("channels", typeof(int), Required: false, Default: 2, Help: "preferred channel count",
            Choices: ["1", "2", "3", "4", "5", "6", "7", "8"]),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceName = ctx.Get<string>("renderDevice");
        var device = WasapiDevices.ResolveRender(deviceName);
        if (device is null) return TestResult.Fail($"Render device not found: {deviceName}");

        var rate = ctx.Get<int>("rate");
        var bits = ctx.Get<int>("bits");
        var channels = ctx.Get<int>("channels");

        var preferred = new WaveFormatExtensible(rate, bits, channels);

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(device.FriendlyName)}[/]");
        AnsiConsole.MarkupLine($"[grey]Preferred: {rate}Hz {bits}bit {channels}ch[/]");
        AnsiConsole.MarkupLine("[grey]Note: WaveFormatExtensible ctor pins 32-bit to IEEE float.[/]\n");

        using var player = new WasapiPlayerBuilder().WithDevice(device).WithExclusiveMode().Build();
        var result = player.GetSupportedExclusiveFormat(preferred);

        if (result is null)
            return TestResult.Fail("GetSupportedExclusiveFormat returned null — no supported format",
                new Dictionary<string, string>
                {
                    ["device"] = device.FriendlyName,
                    ["preferredRate"] = rate.ToString(),
                    ["preferredBits"] = bits.ToString(),
                    ["preferredChannels"] = channels.ToString(),
                });

        var resultTable = new Table().Border(TableBorder.Rounded)
            .AddColumn("Property").AddColumn("Value");
        resultTable.AddRow("Sample Rate", $"{result.SampleRate} Hz");
        resultTable.AddRow("Bit Depth", $"{result.BitsPerSample} bit");
        resultTable.AddRow("Channels", $"{result.Channels}");
        resultTable.AddRow("Block Align", $"{result.BlockAlign}");
        resultTable.AddRow("Encoding", $"{result.SubFormat}");
        resultTable.AddRow("Full Description", Markup.Escape(result.ToString()));

        var exact = result.SampleRate == rate && result.BitsPerSample == bits && result.Channels == channels;
        AnsiConsole.MarkupLine(exact
            ? "[green]Exact match found![/]"
            : "[yellow]Fallback format found (differs from preferred):[/]");
        AnsiConsole.Write(resultTable);

        return TestResult.Pass(
            exact ? "Exact match" : $"Fallback: {result.SampleRate}Hz {result.BitsPerSample}bit {result.Channels}ch",
            new Dictionary<string, string>
            {
                ["device"] = device.FriendlyName,
                ["exactMatch"] = exact ? "true" : "false",
                ["resultRate"] = result.SampleRate.ToString(),
                ["resultBits"] = result.BitsPerSample.ToString(),
                ["resultChannels"] = result.Channels.ToString(),
            });
    }
}
