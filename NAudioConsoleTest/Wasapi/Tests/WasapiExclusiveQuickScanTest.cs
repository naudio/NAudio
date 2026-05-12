using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

sealed class WasapiExclusiveQuickScanTest : IConsoleTest
{
    public string Id => "Wasapi.ExclusiveQuickScan";
    public string Description => "Probe common (rate × bits × channels) combos for exclusive-mode support";
    public MenuPath? MenuLocation =>
        new("WASAPI", "Quick scan exclusive formats", Group: "Info", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceName = ctx.Get<string>("renderDevice");
        var device = WasapiDevices.ResolveRender(deviceName);
        if (device is null) return TestResult.Fail($"Render device not found: {deviceName}");

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(device.FriendlyName)}[/]");
        using var client = device.CreateAudioClient();

        var table = new Table().Border(TableBorder.Rounded).AddColumn("Format").AddColumn("Supported");
        var supportedCount = 0;
        foreach (var rate in WasapiExclusiveFormatHelper.SampleRates)
        foreach (var (bits, encoding) in WasapiExclusiveFormatHelper.BitDepthEncodings)
        foreach (var ch in WasapiExclusiveFormatHelper.ChannelCounts)
        {
            var format = WasapiExclusiveFormatHelper.CreateFormat(rate, bits, ch, encoding);
            if (client.IsFormatSupported(AudioClientShareMode.Exclusive, format))
            {
                table.AddRow($"{rate}Hz {bits}bit {encoding} {ch}ch", "[green]YES[/]");
                supportedCount++;
            }
        }

        if (supportedCount == 0)
            AnsiConsole.MarkupLine("[yellow]No formats supported with default channel masks. " +
                                   "Try 'Channel mask deep-dive' — your device may need a specific layout.[/]");
        else
            AnsiConsole.Write(table);

        return TestResult.Pass(
            $"{supportedCount} supported format(s)",
            new Dictionary<string, string>
            {
                ["device"] = device.FriendlyName,
                ["supportedCount"] = supportedCount.ToString(),
            });
    }
}
