using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

static class WasapiMenu
{
    public static void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold blue]WASAPI[/]").LeftJustified());
            AnsiConsole.MarkupLine("");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a test:")
                    .AddChoiceGroup("Player", new[]
                    {
                        "Play audio file (shared mode)",
                        "Play audio file (exclusive mode)",
                        "Play audio file (low latency)",
                        "Play sine wave",
                    })
                    .AddChoiceGroup("Recorder", new[]
                    {
                        "Record and playback (15s)",
                        "Record to WAV file",
                    })
                    .AddChoiceGroup("Info", new[]
                    {
                        "List audio devices",
                        "Explore exclusive mode formats",
                    })
                    .AddChoices("Back"));

            try
            {
                switch (choice)
                {
                    case "Play audio file (shared mode)":
                        PlayerTests.PlayFileShared();
                        break;
                    case "Play audio file (exclusive mode)":
                        PlayerTests.PlayFileExclusive();
                        break;
                    case "Play audio file (low latency)":
                        PlayerTests.PlayFileLowLatency();
                        break;
                    case "Play sine wave":
                        PlayerTests.PlaySineWave();
                        break;
                    case "Record and playback (15s)":
                        RecorderTests.RecordAndPlayback();
                        break;
                    case "Record to WAV file":
                        RecorderTests.RecordToWavFile();
                        break;
                    case "List audio devices":
                        DeviceInfoTest.Run();
                        break;
                    case "Explore exclusive mode formats":
                        ExclusiveFormatExplorer.Run();
                        break;
                    case "Back":
                        return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"\n[red]Error: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(ex.GetType().Name)}[/]");
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(intercept: true);
            }
        }
    }
}
