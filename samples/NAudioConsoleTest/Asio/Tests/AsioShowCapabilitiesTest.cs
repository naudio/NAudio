using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

sealed class AsioShowCapabilitiesTest : IConsoleTest
{
    public string Id => "Asio.ShowCapabilities";
    public string Description => "Print an ASIO driver's channel counts, latencies, supported rates, and clock sources";
    public MenuPath? MenuLocation => new("ASIO (AsioDevice — NAudio 3)", "Show driver capabilities", Group: "Info", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");

        var caps = device.Capabilities;

        var info = new Table().Border(TableBorder.Rounded).AddColumn("Property").AddColumn("Value");
        info.AddRow("Driver name", Markup.Escape(caps.DriverName));
        info.AddRow("Current sample rate", $"{device.CurrentSampleRate} Hz");
        info.AddRow("Input channels", caps.NbInputChannels.ToString());
        info.AddRow("Output channels", caps.NbOutputChannels.ToString());
        info.AddRow("Input latency", $"{caps.InputLatency} frames");
        info.AddRow("Output latency", $"{caps.OutputLatency} frames");
        info.AddRow("Buffer size range", $"{caps.BufferMinSize} – {caps.BufferMaxSize} frames");
        info.AddRow("Buffer preferred", $"{caps.BufferPreferredSize} frames");
        info.AddRow("Buffer granularity", caps.BufferGranularity.ToString());
        AnsiConsole.Write(info);

        if (caps.NbInputChannels > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Input channels[/]");
            var inputs = new Table().AddColumn("Idx").AddColumn("Name").AddColumn("Native Format");
            for (var i = 0; i < caps.NbInputChannels; i++)
            {
                var ch = caps.InputChannelInfos[i];
                inputs.AddRow(i.ToString(), Markup.Escape(ch.name ?? ""), ch.type.ToString());
            }
            AnsiConsole.Write(inputs);
        }
        if (caps.NbOutputChannels > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Output channels[/]");
            var outputs = new Table().AddColumn("Idx").AddColumn("Name").AddColumn("Native Format");
            for (var i = 0; i < caps.NbOutputChannels; i++)
            {
                var ch = caps.OutputChannelInfos[i];
                outputs.AddRow(i.ToString(), Markup.Escape(ch.name ?? ""), ch.type.ToString());
            }
            AnsiConsole.Write(outputs);
        }

        AnsiConsole.MarkupLine("\n[bold]Sample rate support[/]");
        var supportedRates = new List<int>();
        foreach (var rate in new[] { 44100, 48000, 88200, 96000, 176400, 192000 })
        {
            var supported = device.IsSampleRateSupported(rate);
            if (supported) supportedRates.Add(rate);
            AnsiConsole.MarkupLine($"  {(supported ? "[green]✓[/]" : "[red]✗[/]")} {rate} Hz");
        }

        var clockSourceCount = 0;
        AnsiConsole.MarkupLine("\n[bold]Clock sources[/]");
        try
        {
            var clocks = device.GetClockSources();
            clockSourceCount = clocks.Length;
            if (clocks.Length == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Driver reported no clock sources.[/]");
            }
            else
            {
                var clockTable = new Table()
                    .AddColumn("Idx").AddColumn("Name").AddColumn("Channel").AddColumn("Group").AddColumn("Current");
                foreach (var c in clocks)
                {
                    clockTable.AddRow(
                        c.Index.ToString(), Markup.Escape(c.Name ?? ""),
                        c.AssociatedChannel.ToString(), c.AssociatedGroup.ToString(),
                        c.IsCurrentSource != 0 ? "[green]✓[/]" : "");
                }
                AnsiConsole.Write(clockTable);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Driver does not support clock-source enumeration: {Markup.Escape(ex.Message)}[/]");
        }

        return TestResult.Pass(
            $"{caps.NbInputChannels}in/{caps.NbOutputChannels}out @ {device.CurrentSampleRate}Hz",
            new Dictionary<string, string>
            {
                ["driver"] = caps.DriverName,
                ["currentSampleRate"] = device.CurrentSampleRate.ToString(),
                ["inputChannels"] = caps.NbInputChannels.ToString(),
                ["outputChannels"] = caps.NbOutputChannels.ToString(),
                ["inputLatency"] = caps.InputLatency.ToString(),
                ["outputLatency"] = caps.OutputLatency.ToString(),
                ["bufferPreferred"] = caps.BufferPreferredSize.ToString(),
                ["supportedRates"] = string.Join(",", supportedRates),
                ["clockSources"] = clockSourceCount.ToString(),
            });
    }
}
