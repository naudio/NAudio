using Spectre.Console;

namespace NAudioConsoleTest.WinMM;

static class WinMmMenu
{
    public static void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold blue]WinMM (Windows Multimedia)[/]").LeftJustified());
            AnsiConsole.MarkupLine("");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a test:")
                    .AddChoiceGroup("Playback & Recording",
                        "Play audio file (WaveOut)",
                        "Record audio (WaveIn)")
                    .AddChoiceGroup("ACM Compression",
                        "Decode MP3 (AcmMp3FrameDecompressor)",
                        "Convert format (WaveFormatConversionProvider)",
                        "Convert format stream (WaveFormatConversionStream)")
                    .AddChoices("Back"));

            try
            {
                switch (choice)
                {
                    case "Play audio file (WaveOut)":
                        PlaybackTests.PlayFile();
                        break;
                    case "Record audio (WaveIn)":
                        RecordingTests.RecordToFile();
                        break;
                    case "Decode MP3 (AcmMp3FrameDecompressor)":
                        AcmTests.DecodeMp3();
                        break;
                    case "Convert format (WaveFormatConversionProvider)":
                        AcmTests.ConvertWithProvider();
                        break;
                    case "Convert format stream (WaveFormatConversionStream)":
                        AcmTests.ConvertWithStream();
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
