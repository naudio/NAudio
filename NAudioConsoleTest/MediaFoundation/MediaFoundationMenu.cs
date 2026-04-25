using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation;

static class MediaFoundationMenu
{
    public static void Show()
    {
        while (true)
        {
            var choice = Menu.Show("Media Foundation",
                new Menu.Group("Reading",
                    "Read audio file (MediaFoundationReader)",
                    "Read from stream (StreamMediaFoundationReader)"),
                new Menu.Group("Encoding",
                    "Encode to MP3",
                    "Encode to AAC",
                    "Encode to WMA"),
                new Menu.Group("Resampling",
                    "Resample audio file"),
                new Menu.Group("Info",
                    "Enumerate transforms"),
                new Menu.Group("", "Back"));

            if (choice is null or "Back") return;

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
