using System.Reflection;
using NAudio.CoreAudioApi;
using NAudio.Dmo;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

static class ExclusiveFormatExplorer
{
    private static readonly int[] SampleRates = [44100, 48000, 88200, 96000, 176400, 192000];
    private static readonly int[] ChannelCounts = [1, 2, 4, 6, 8];

    /// <summary>
    /// Bit depth + encoding combinations to test.
    /// The WaveFormatExtensible constructor hardcodes 32-bit to IEEE float,
    /// so we need to handle 32-bit PCM separately.
    /// </summary>
    private static readonly (int bits, string encoding)[] BitDepthEncodings =
    [
        (16, "PCM"),
        (24, "PCM"),
        (32, "PCM"),
        (32, "Float"),
    ];

    private static readonly (int mask, string name)[] ChannelMasks =
    [
        (0, "(default)"),
        (0x0004, "1.0 Mono (FC)"),
        (0x0003, "2.0 Stereo (FL|FR)"),
        (0x000C, "1.1 (FC|LFE)"),
        (0x000B, "2.1 (FL|FR|LFE)"),
        (0x0033, "4.0 Quad (FL|FR|BL|BR)"),
        (0x0107, "4.0 Surround (FL|FR|FC|BC)"),
        (0x0607, "5.0 (FL|FR|FC|SL|SR)"),
        (0x003F, "5.1 Back (FL|FR|FC|LFE|BL|BR)"),
        (0x060F, "5.1 Surround (FL|FR|FC|LFE|SL|SR)"),
        (0x0637, "7.0 (FL|FR|FC|BL|BR|SL|SR)"),
        (0x00FF, "7.1 Wide (FL|FR|FC|LFE|BL|BR|FLC|FRC)"),
        (0x063F, "7.1 Surround (FL|FR|FC|LFE|BL|BR|SL|SR)"),
    ];

    public static void Run()
    {
        AnsiConsole.MarkupLine("[bold]Exclusive Mode Format Explorer[/]\n");

        var device = DeviceSelector.SelectRenderDevice();
        if (device == null) return;

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(device.FriendlyName)}[/]");

        using var client = device.CreateAudioClient();
        var mix = client.MixFormat;
        var mixEncoding = mix is WaveFormatExtensible wfe
            ? (wfe.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT ? "Float" : "PCM")
            : (mix.Encoding == WaveFormatEncoding.IeeeFloat ? "Float" : "PCM");
        AnsiConsole.MarkupLine($"[grey]Mix format: {mix.SampleRate}Hz {mix.BitsPerSample}bit {mixEncoding} {mix.Channels}ch[/]\n");

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to explore?")
                .AddChoices(
                    "Quick scan (common formats)",
                    "Detailed scan (all combinations)",
                    "Channel mask deep-dive (specific channel count)",
                    "Find best format (GetSupportedExclusiveFormat)",
                    "Back"));

        switch (mode)
        {
            case "Quick scan (common formats)":
                QuickScan(device);
                break;
            case "Detailed scan (all combinations)":
                DetailedScan(device);
                break;
            case "Channel mask deep-dive (specific channel count)":
                ChannelMaskDeepDive(device);
                break;
            case "Find best format (GetSupportedExclusiveFormat)":
                FindBestFormat(device);
                break;
        }
    }

    private static void QuickScan(MMDevice device)
    {
        AnsiConsole.MarkupLine("\n[bold]Quick Scan — Common Formats[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Format")
            .AddColumn("Supported");

        using var client = device.CreateAudioClient();

        foreach (var rate in SampleRates)
        {
            foreach (var (bits, encoding) in BitDepthEncodings)
            {
                foreach (var ch in ChannelCounts)
                {
                    var format = CreateFormat(rate, bits, ch, encoding);
                    var supported = client.IsFormatSupported(AudioClientShareMode.Exclusive, format);
                    if (supported)
                    {
                        table.AddRow(
                            $"{rate}Hz {bits}bit {encoding} {ch}ch",
                            "[green]YES[/]");
                    }
                }
            }
        }

        if (table.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No formats supported with default channel masks.[/]");
            AnsiConsole.MarkupLine("[dim]Try 'Channel mask deep-dive' — your device may need a specific speaker layout.[/]");
        }
        else
        {
            AnsiConsole.Write(table);
        }

        WaitForKey();
    }

    private static void DetailedScan(MMDevice device)
    {
        AnsiConsole.MarkupLine("\n[bold]Detailed Scan — All Combinations Including Channel Masks[/]\n");

        using var client = device.CreateAudioClient();
        var supported = new List<string>();
        int tested = 0;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Testing formats...", ctx =>
            {
                foreach (var rate in SampleRates)
                {
                    ctx.Status($"Testing {rate}Hz...");
                    foreach (var (bits, encoding) in BitDepthEncodings)
                    {
                        foreach (var ch in ChannelCounts)
                        {
                            foreach (var (mask, maskName) in GetMasksForChannelCount(ch))
                            {
                                tested++;
                                var format = CreateFormat(rate, bits, ch, encoding, mask);
                                if (client.IsFormatSupported(AudioClientShareMode.Exclusive, format))
                                {
                                    supported.Add($"{rate}Hz {bits}bit {encoding} {ch}ch mask={maskName}");
                                }
                            }
                        }
                    }
                }
            });

        AnsiConsole.MarkupLine($"[dim]Tested {tested} combinations[/]\n");

        if (supported.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No supported exclusive formats found.[/]");
        }
        else
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Supported Format")
                .AddColumn("Channel Mask");

            foreach (var fmt in supported)
            {
                var parts = fmt.Split(" mask=");
                table.AddRow(parts[0], parts[1]);
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]{supported.Count} supported format(s)[/]");
        }

        WaitForKey();
    }

    private static void ChannelMaskDeepDive(MMDevice device)
    {
        var channelCount = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Select channel count to explore:")
                .AddChoices(1, 2, 3, 4, 5, 6, 7, 8));

        AnsiConsole.MarkupLine($"\n[bold]Channel Mask Deep-Dive — {channelCount} channels[/]\n");

        using var client = device.CreateAudioClient();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Sample Rate")
            .AddColumn("Bits")
            .AddColumn("Encoding")
            .AddColumn("Mask")
            .AddColumn("Layout")
            .AddColumn("Supported");

        var masks = GetMasksForChannelCount(channelCount);

        foreach (var rate in SampleRates)
        {
            foreach (var (bits, encoding) in BitDepthEncodings)
            {
                foreach (var (mask, name) in masks)
                {
                    var format = CreateFormat(rate, bits, channelCount, encoding, mask);
                    var supported = client.IsFormatSupported(AudioClientShareMode.Exclusive, format);
                    table.AddRow(
                        $"{rate}",
                        $"{bits}",
                        encoding,
                        $"0x{mask:X4}",
                        name,
                        supported ? "[green]YES[/]" : "[dim]no[/]");
                }
            }
        }

        AnsiConsole.Write(table);
        WaitForKey();
    }

    private static void FindBestFormat(MMDevice device)
    {
        AnsiConsole.MarkupLine("\n[bold]GetSupportedExclusiveFormat Test[/]\n");

        var rate = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Preferred sample rate:")
                .AddChoices(SampleRates));

        var bits = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Preferred bit depth:")
                .AddChoices(16, 24, 32));

        var channels = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Preferred channel count:")
                .AddChoices(1, 2, 3, 4, 5, 6, 7, 8));

        var preferredFormat = new WaveFormatExtensible(rate, bits, channels);

        AnsiConsole.MarkupLine($"\n[grey]Preferred: {rate}Hz {bits}bit {channels}ch[/]");
        AnsiConsole.MarkupLine($"[grey]Note: GetSupportedExclusiveFormat uses WaveFormatExtensible constructor[/]");
        AnsiConsole.MarkupLine($"[grey]  32-bit = IEEE Float, 16/24-bit = PCM[/]");
        AnsiConsole.MarkupLine("[dim]Searching...[/]\n");

        using var player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithExclusiveMode()
            .Build();

        var result = player.GetSupportedExclusiveFormat(preferredFormat);

        if (result != null)
        {
            var resultTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Property")
                .AddColumn("Value");

            resultTable.AddRow("Sample Rate", $"{result.SampleRate} Hz");
            resultTable.AddRow("Bit Depth", $"{result.BitsPerSample} bit");
            resultTable.AddRow("Channels", $"{result.Channels}");
            resultTable.AddRow("Block Align", $"{result.BlockAlign}");
            resultTable.AddRow("Encoding", $"{result.SubFormat}");
            resultTable.AddRow("Full Description", Markup.Escape(result.ToString()));

            var matchesPreferred = result.SampleRate == rate
                && result.BitsPerSample == bits
                && result.Channels == channels;

            AnsiConsole.MarkupLine(matchesPreferred
                ? "[green]Exact match found![/]"
                : "[yellow]Fallback format found (differs from preferred):[/]");
            AnsiConsole.Write(resultTable);
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No supported exclusive format found for this device.[/]");
        }

        WaitForKey();
    }

    /// <summary>
    /// Creates a WaveFormatExtensible with the specified encoding.
    /// The standard constructor hardcodes 32-bit to IEEE Float, so for 32-bit PCM
    /// we create it as float then patch the SubFormat via reflection.
    /// </summary>
    private static WaveFormatExtensible CreateFormat(int rate, int bits, int channels,
        string encoding, int channelMask = 0)
    {
        var format = new WaveFormatExtensible(rate, bits, channels, channelMask);

        if (bits == 32 && encoding == "PCM")
        {
            // The constructor set SubFormat to IEEE_FLOAT — override to PCM
            var subFormatField = typeof(WaveFormatExtensible)
                .GetField("subFormat", BindingFlags.NonPublic | BindingFlags.Instance)!;
            subFormatField.SetValue(format, AudioMediaSubtypes.MEDIASUBTYPE_PCM);
        }

        return format;
    }

    private static List<(int mask, string name)> GetMasksForChannelCount(int channelCount)
    {
        var masks = new List<(int, string)> { (0, "(default)") };
        foreach (var (mask, name) in ChannelMasks)
        {
            if (mask == 0) continue;
            // Count bits set in mask to match channel count
            if (BitCount(mask) == channelCount)
                masks.Add((mask, name));
        }
        return masks;
    }

    private static int BitCount(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    private static void WaitForKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
