using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation;

static class MediaFoundationMenu
{
    public static void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold blue]Media Foundation[/]").LeftJustified());
            AnsiConsole.MarkupLine("");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a test:")
                    .AddChoiceGroup("Reading", new[]
                    {
                        "Read audio file (MediaFoundationReader)",
                        "Read from stream (StreamMediaFoundationReader)",
                    })
                    .AddChoiceGroup("Encoding", new[]
                    {
                        "Encode to MP3",
                        "Encode to AAC",
                        "Encode to WMA",
                    })
                    .AddChoiceGroup("Resampling", new[]
                    {
                        "Resample audio file",
                    })
                    .AddChoiceGroup("Info", new[]
                    {
                        "Enumerate transforms",
                    })
                    .AddChoices("Back"));

            try
            {
                switch (choice)
                {
                    case "Read audio file (MediaFoundationReader)":
                        ReaderTests.ReadAudioFile();
                        break;
                    case "Read from stream (StreamMediaFoundationReader)":
                        ReaderTests.ReadFromStream();
                        break;
                    case "Encode to MP3":
                        EncoderTests.EncodeToMp3();
                        break;
                    case "Encode to AAC":
                        EncoderTests.EncodeToAac();
                        break;
                    case "Encode to WMA":
                        EncoderTests.EncodeToWma();
                        break;
                    case "Resample audio file":
                        ResamplerTests.ResampleFile();
                        break;
                    case "Enumerate transforms":
                        TransformEnumerationTests.EnumerateAll();
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
