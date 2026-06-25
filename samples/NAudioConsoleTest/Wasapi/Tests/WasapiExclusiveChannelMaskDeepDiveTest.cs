using NAudio.CoreAudioApi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

internal sealed class WasapiExclusiveChannelMaskDeepDiveTest : IConsoleTest
{
    public string Id => "Wasapi.ExclusiveChannelMaskDeepDive";
    public string Description => "Show every (rate × bits × mask) probe result for a fixed channel count";
    public MenuPath? MenuLocation =>
        new("WASAPI", "Channel mask deep-dive (specific channel count)", Group: "Info", Order: 3);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("channels", typeof(int), Required: false, Default: 2,
            Help: "channel count to probe",
            Choices: ["1", "2", "3", "4", "5", "6", "7", "8"]),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceName = ctx.Get<string>("renderDevice");
        var device = WasapiDevices.ResolveRender(deviceName);
        if (device is null) return TestResult.Fail($"Render device not found: {deviceName}");

        var channelCount = ctx.Get<int>("channels");

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(device.FriendlyName)} — {channelCount} channels[/]\n");
        using var client = device.CreateAudioClient();

        var masks = WasapiExclusiveFormatHelper.GetMasksForChannelCount(channelCount);
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("Sample Rate").AddColumn("Bits").AddColumn("Encoding")
            .AddColumn("Mask").AddColumn("Layout").AddColumn("Supported");

        var supportedCount = 0;
        var totalProbed = 0;
        foreach (var rate in WasapiExclusiveFormatHelper.SampleRates)
            foreach (var (bits, encoding) in WasapiExclusiveFormatHelper.BitDepthEncodings)
                foreach (var (mask, name) in masks)
                {
                    totalProbed++;
                    var format = WasapiExclusiveFormatHelper.CreateFormat(rate, bits, channelCount, encoding, mask);
                    var supported = client.IsFormatSupported(AudioClientShareMode.Exclusive, format);
                    if (supported) supportedCount++;
                    table.AddRow($"{rate}", $"{bits}", encoding, $"0x{mask:X4}", name,
                        supported ? "[green]YES[/]" : "[dim]no[/]");
                }

        AnsiConsole.Write(table);

        return TestResult.Pass(
            $"{supportedCount}/{totalProbed} combos supported for {channelCount}ch",
            new Dictionary<string, string>
            {
                ["device"] = device.FriendlyName,
                ["channels"] = channelCount.ToString(),
                ["totalProbed"] = totalProbed.ToString(),
                ["supported"] = supportedCount.ToString(),
            });
    }
}
