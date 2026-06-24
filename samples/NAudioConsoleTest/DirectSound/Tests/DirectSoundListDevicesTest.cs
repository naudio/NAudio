using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.DirectSound.Tests;

sealed class DirectSoundListDevicesTest : IConsoleTest
{
    public string Id => "DirectSound.ListDevices";
    public string Description => "List DirectSound playback devices";
    public MenuPath? MenuLocation => new("DirectSound", "List devices", Group: "Enumeration", Order: 0);
    public IReadOnlyList<TestParameter> Parameters => [];

    public TestResult Run(TestContext ctx)
    {
        var devices = DirectSoundOut.Devices.ToList();

        if (ctx.Interactive)
        {
            var table = new Table()
                .AddColumn("GUID")
                .AddColumn("Description")
                .AddColumn("Module");
            foreach (var d in devices)
            {
                table.AddRow(
                    Markup.Escape(d.Guid.ToString()),
                    Markup.Escape(d.Description ?? ""),
                    Markup.Escape(d.ModuleName ?? ""));
            }
            AnsiConsole.Write(table);
        }
        else
        {
            foreach (var d in devices)
                Console.WriteLine($"{d.Guid}\t{d.Description}\t{d.ModuleName}");
        }

        return TestResult.Pass($"{devices.Count} DirectSound device(s)",
            new Dictionary<string, string> { ["deviceCount"] = devices.Count.ToString() });
    }
}
