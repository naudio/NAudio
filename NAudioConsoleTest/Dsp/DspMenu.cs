using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Dsp;

static class DspMenu
{
    public static void Show()
    {
        while (true)
        {
            var choice = Menu.Show("DSP",
                new Menu.Group("",
                    "Resample audio file (WdlResamplingSampleProvider)",
                    "Back"));

            if (choice is null or "Back") return;

            try
            {
                switch (choice)
                {
                    case "Resample audio file (WdlResamplingSampleProvider)":
                        WdlResamplerTests.ResampleFile();
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"\n[red]Error: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(ex.GetType().Name)}[/]");
                if (ex.InnerException != null)
                    AnsiConsole.MarkupLine($"[dim]Inner: {Markup.Escape(ex.InnerException.Message)}[/]");
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(intercept: true);
            }
        }
    }
}
