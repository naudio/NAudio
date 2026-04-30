using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.DirectSound;

static class DirectSoundMenu
{
    public static void Show()
    {
        while (true)
        {
            var choice = Menu.Show("DirectSound",
                new Menu.Group("Enumeration",
                    "List devices"),
                new Menu.Group("Playback",
                    "Play tone (DirectSoundOut)"),
                new Menu.Group("", "Back"));

            if (choice is null or "Back") return;

            try
            {
                switch (choice)
                {
                    case "List devices":
                        DirectSoundTests.ListDevices();
                        break;
                    case "Play tone (DirectSoundOut)":
                        DirectSoundTests.PlayTone();
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
