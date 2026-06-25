using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Enumerates installed ASIO drivers. Zero parameters; safe to run unattended.
/// </summary>
internal sealed class AsioListDriversTest : IConsoleTest
{
    public string Id => "Asio.ListDrivers";
    public string Description => "List installed ASIO drivers";
    public MenuPath? MenuLocation => new("ASIO (AsioDevice — NAudio 3)", "List ASIO drivers", Group: "Info", Order: 0);
    public IReadOnlyList<TestParameter> Parameters => [];

    public TestResult Run(TestContext context)
    {
        var drivers = AsioDevice.GetDriverNames();

        if (context.Interactive)
        {
            AnsiConsole.MarkupLine("[bold]Installed ASIO Drivers[/]\n");
            if (drivers.Length == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No ASIO drivers installed.[/]");
            }
            else
            {
                var table = new Table().AddColumn("#").AddColumn("Driver Name");
                for (int i = 0; i < drivers.Length; i++)
                    table.AddRow(i.ToString(), Markup.Escape(drivers[i]));
                AnsiConsole.Write(table);
            }
        }
        else
        {
            // Non-interactive: plain, scriptable output.
            if (drivers.Length == 0)
            {
                Console.WriteLine("No ASIO drivers installed.");
            }
            else
            {
                for (int i = 0; i < drivers.Length; i++)
                    Console.WriteLine($"{i}\t{drivers[i]}");
            }
        }

        var diagnostics = new Dictionary<string, string>
        {
            ["driverCount"] = drivers.Length.ToString(),
        };
        return TestResult.Pass($"{drivers.Length} ASIO driver(s) found", diagnostics);
    }
}
