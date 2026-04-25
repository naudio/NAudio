using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Dmo;

static class DmoMenu
{
    public static void Show()
    {
        while (true)
        {
            var choice = Menu.Show("DMO (DirectX Media Objects)",
                new Menu.Group("",
                    "Resample audio file (ResamplerDmoStream)",
                    "Decode MP3 (DmoMp3FrameDecompressor)",
                    "Apply echo effect (DmoEffectWaveProvider)",
                    "Back"));

            if (choice is null or "Back") return;

            try
            {
                switch (choice)
                {
                    case "Resample audio file (ResamplerDmoStream)":
                        DmoResamplerTests.ResampleFile();
                        break;
                    case "Decode MP3 (DmoMp3FrameDecompressor)":
                        DmoMp3Tests.DecodeMp3();
                        break;
                    case "Apply echo effect (DmoEffectWaveProvider)":
                        DmoEffectTests.ApplyEcho();
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
