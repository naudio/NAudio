using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioMenu
{
    public static void Show()
    {
        while (true)
        {
            var choice = Menu.Show("ASIO (AsioDevice — NAudio 3)",
                new Menu.Group("Info",
                    "List ASIO drivers",
                    "Show driver capabilities"),
                new Menu.Group("Playback",
                    "Play audio file",
                    "Play short test tone (quiet 440Hz, 2s)"),
                new Menu.Group("Recording",
                    "Record to WAV file",
                    "Show per-channel input levels"),
                new Menu.Group("Duplex",
                    "Duplex passthrough (input → gain → output)"),
                new Menu.Group("Lifecycle",
                    "Reinitialize round-trip"),
                new Menu.Group("Timing",
                    "Validate SamplePosition + SystemTimeNanoseconds"),
                new Menu.Group("Regression tests",
                    "Dispose from Stopped handler (F1)",
                    "Stop from AudioCaptured callback (F1 guard)",
                    "Legacy AsioOut duplex (AudioAvailable + WrittenToOutputBuffers)"),
                new Menu.Group("", "Back"));

            if (choice is null or "Back") return;

            try
            {
                switch (choice)
                {
                    case "List ASIO drivers":
                        AsioDeviceInfoTest.ListDrivers();
                        break;
                    case "Show driver capabilities":
                        AsioDeviceInfoTest.ShowCapabilities();
                        break;
                    case "Play audio file":
                        AsioPlayerTests.PlayAudioFile();
                        break;
                    case "Play short test tone (quiet 440Hz, 2s)":
                        AsioPlayerTests.PlayShortTestTone();
                        break;
                    case "Record to WAV file":
                        AsioRecorderTests.RecordToWav();
                        break;
                    case "Show per-channel input levels":
                        AsioRecorderTests.ShowChannelLevels();
                        break;
                    case "Duplex passthrough (input → gain → output)":
                        AsioDuplexTests.Passthrough();
                        break;
                    case "Reinitialize round-trip":
                        AsioLifecycleTests.ReinitializeRoundTrip();
                        break;
                    case "Validate SamplePosition + SystemTimeNanoseconds":
                        AsioTimingTests.ValidateSamplePosition();
                        break;
                    case "Dispose from Stopped handler (F1)":
                        AsioPlayerTests.DisposeFromStoppedHandler();
                        break;
                    case "Stop from AudioCaptured callback (F1 guard)":
                        AsioRecorderTests.StopFromCallbackGuard();
                        break;
                    case "Legacy AsioOut duplex (AudioAvailable + WrittenToOutputBuffers)":
                        AsioOutDuplexTests.LegacyAudioAvailablePassthrough();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Some ASIO drivers throw with empty Message strings; fall back to "(no message)" so the
                // user at least sees the exception type and isn't left staring at "Error: ".
                var message = string.IsNullOrWhiteSpace(ex.Message) ? "(no message)" : ex.Message;
                AnsiConsole.MarkupLine($"\n[red]Error: {Markup.Escape(message)}[/]");
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(ex.GetType().Name)}[/]");
                if (ex.InnerException != null)
                {
                    var innerMessage = string.IsNullOrWhiteSpace(ex.InnerException.Message) ? "(no message)" : ex.InnerException.Message;
                    AnsiConsole.MarkupLine($"[dim]Inner: {Markup.Escape(innerMessage)}[/]");
                }
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(intercept: true);
            }
        }
    }
}
