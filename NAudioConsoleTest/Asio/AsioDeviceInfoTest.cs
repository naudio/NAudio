using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioDeviceInfoTest
{
    public static void ListDrivers()
    {
        AnsiConsole.MarkupLine("[bold]Installed ASIO Drivers[/]\n");
        var drivers = AsioDevice.GetDriverNames();
        if (drivers.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ASIO drivers installed.[/]");
        }
        else
        {
            var table = new Table().AddColumn("#").AddColumn("Driver Name");
            for (int i = 0; i < drivers.Length; i++)
            {
                table.AddRow(i.ToString(), Markup.Escape(drivers[i]));
            }
            AnsiConsole.Write(table);
        }

        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    public static void ShowCapabilities()
    {
        AnsiConsole.MarkupLine("[bold]Show Driver Capabilities[/]\n");
        var driver = AsioDeviceSelector.SelectDriver();
        if (driver == null) return;

        using var device = AsioDevice.Open(driver);
        var caps = device.Capabilities;

        var info = new Table().Border(TableBorder.Rounded)
            .AddColumn("Property").AddColumn("Value");

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

        AnsiConsole.MarkupLine("\n[bold]Input channels[/]");
        var inputs = new Table().AddColumn("Idx").AddColumn("Name").AddColumn("Native Format");
        for (int i = 0; i < caps.NbInputChannels; i++)
        {
            var ch = caps.InputChannelInfos[i];
            inputs.AddRow(i.ToString(), Markup.Escape(ch.name ?? ""), ch.type.ToString());
        }
        if (caps.NbInputChannels > 0) AnsiConsole.Write(inputs);

        AnsiConsole.MarkupLine("\n[bold]Output channels[/]");
        var outputs = new Table().AddColumn("Idx").AddColumn("Name").AddColumn("Native Format");
        for (int i = 0; i < caps.NbOutputChannels; i++)
        {
            var ch = caps.OutputChannelInfos[i];
            outputs.AddRow(i.ToString(), Markup.Escape(ch.name ?? ""), ch.type.ToString());
        }
        if (caps.NbOutputChannels > 0) AnsiConsole.Write(outputs);

        AnsiConsole.MarkupLine("\n[bold]Sample rate support[/]");
        foreach (var rate in new[] { 44100, 48000, 88200, 96000, 176400, 192000 })
        {
            var supported = device.IsSampleRateSupported(rate);
            var tag = supported ? "[green]✓[/]" : "[red]✗[/]";
            AnsiConsole.MarkupLine($"  {tag} {rate} Hz");
        }

        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
