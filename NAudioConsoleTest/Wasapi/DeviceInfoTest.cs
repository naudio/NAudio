using NAudio.CoreAudioApi;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

static class DeviceInfoTest
{
    public static void Run()
    {
        using var enumerator = new MMDeviceEnumerator();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Data Flow")
            .AddColumn("State")
            .AddColumn("Default Format")
            .AddColumn("ID");

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
        {
            var format = "?";
            try
            {
                using var client = device.CreateAudioClient();
                var mix = client.MixFormat;
                format = $"{mix.SampleRate}Hz {mix.BitsPerSample}bit {mix.Channels}ch";
            }
            catch { }

            table.AddRow(
                Markup.Escape(device.FriendlyName),
                device.DataFlow.ToString(),
                device.State.ToString(),
                format,
                Markup.Escape(device.ID));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
