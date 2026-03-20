using Spectre.Console;

namespace NAudioConsoleTest.Dmo;

static class DmoMenu
{
    public static void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold blue]DMO (DirectX Media Objects)[/]").LeftJustified());
            AnsiConsole.MarkupLine("");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a test:")
                    .AddChoices(
                        "Resample audio file (ResamplerDmoStream)",
                        "Decode MP3 (DmoMp3FrameDecompressor)",
                        "Apply echo effect (DmoEffectWaveProvider)",
                        "Back"));

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
                    case "Back":
                        return;
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
