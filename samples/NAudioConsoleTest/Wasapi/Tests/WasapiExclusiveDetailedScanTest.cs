using NAudio.CoreAudioApi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

internal sealed class WasapiExclusiveDetailedScanTest : IConsoleTest
{
    public string Id => "Wasapi.ExclusiveDetailedScan";
    public string Description => "Probe every (rate × bits × channels × channel-mask) combo for exclusive-mode support";
    public MenuPath? MenuLocation =>
        new("WASAPI", "Detailed scan exclusive formats (incl. channel masks)", Group: "Info", Order: 2);

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

        var supported = new List<string>();
        var tested = 0;

        void Scan()
        {
            foreach (var rate in WasapiExclusiveFormatHelper.SampleRates)
                foreach (var (bits, encoding) in WasapiExclusiveFormatHelper.BitDepthEncodings)
                    foreach (var ch in WasapiExclusiveFormatHelper.ChannelCounts)
                        foreach (var (mask, maskName) in WasapiExclusiveFormatHelper.GetMasksForChannelCount(ch))
                        {
                            if (ctx.Cancellation.IsCancellationRequested) return;
                            tested++;
                            var format = WasapiExclusiveFormatHelper.CreateFormat(rate, bits, ch, encoding, mask);
                            if (client.IsFormatSupported(AudioClientShareMode.Exclusive, format))
                                supported.Add($"{rate}Hz {bits}bit {encoding} {ch}ch mask={maskName}");
                        }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Testing formats...", _ => Scan());
        else
            Scan();

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped($"Cancelled after {tested} probes");

        AnsiConsole.MarkupLine($"[dim]Tested {tested} combinations[/]\n");
        if (supported.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No supported exclusive formats found.[/]");
        }
        else
        {
            var table = new Table().Border(TableBorder.Rounded).AddColumn("Supported Format").AddColumn("Channel Mask");
            foreach (var fmt in supported)
            {
                var parts = fmt.Split(" mask=");
                table.AddRow(parts[0], parts[1]);
            }
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]{supported.Count} supported format(s)[/]");
        }

        return TestResult.Pass(
            $"{supported.Count}/{tested} combos supported",
            new Dictionary<string, string>
            {
                ["device"] = device.FriendlyName,
                ["tested"] = tested.ToString(),
                ["supported"] = supported.Count.ToString(),
            });
    }
}
