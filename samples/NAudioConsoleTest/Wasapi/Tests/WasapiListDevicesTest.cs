using NAudio.CoreAudioApi;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

internal sealed class WasapiListDevicesTest : IConsoleTest
{
    public string Id => "Wasapi.ListDevices";
    public string Description => "List active WASAPI render + capture endpoints with default-format probe";
    public MenuPath? MenuLocation => new("WASAPI", "List audio devices", Group: "Info", Order: 0);
    public IReadOnlyList<TestParameter> Parameters => [];

    public TestResult Run(TestContext ctx)
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToList();

        if (ctx.Interactive)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Name").AddColumn("Data Flow").AddColumn("State").AddColumn("Default Format").AddColumn("ID");
            foreach (var d in devices)
            {
                var format = "?";
                try
                {
                    using var client = d.CreateAudioClient();
                    var mix = client.MixFormat;
                    format = $"{mix.SampleRate}Hz {mix.BitsPerSample}bit {mix.Channels}ch";
                }
                catch { }
                table.AddRow(Markup.Escape(d.FriendlyName), d.DataFlow.ToString(), d.State.ToString(),
                    format, Markup.Escape(d.ID));
            }
            AnsiConsole.Write(table);
        }
        else
        {
            foreach (var d in devices)
                Console.WriteLine($"{d.DataFlow}\t{d.FriendlyName}\t{d.ID}");
        }

        var renderCount = devices.Count(d => d.DataFlow == DataFlow.Render);
        var captureCount = devices.Count(d => d.DataFlow == DataFlow.Capture);

        return TestResult.Pass(
            $"{devices.Count} active endpoints ({renderCount} render, {captureCount} capture)",
            new Dictionary<string, string>
            {
                ["totalEndpoints"] = devices.Count.ToString(),
                ["renderEndpoints"] = renderCount.ToString(),
                ["captureEndpoints"] = captureCount.ToString(),
            });
    }
}
