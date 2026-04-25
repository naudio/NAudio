using NAudioConsoleTest.Asio;
using NAudioConsoleTest.Dmo;
using NAudioConsoleTest.MediaFoundation;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Wasapi;
using NAudioConsoleTest.WinMM;
using Spectre.Console;

namespace NAudioConsoleTest;

static class Program
{
    // ASIO drivers are COM objects that require the calling thread to be in an STA apartment.
    // Without [STAThread] the first AsioDevice.Open call fails on most native drivers.
    [STAThread]
    static void Main()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("NAudio").Color(Color.Blue));
            AnsiConsole.MarkupLine("[dim]Interactive Audio Test Harness[/]\n");

            var choice = Menu.Show("Main Menu",
                new Menu.Group("", "WASAPI", "ASIO", "WinMM", "Media Foundation", "DMO", "Exit"));

            // Escape at the top level exits the app.
            if (choice is null) return;

            switch (choice)
            {
                case "WASAPI":
                    WasapiMenu.Show();
                    break;
                case "ASIO":
                    AsioMenu.Show();
                    break;
                case "WinMM":
                    WinMmMenu.Show();
                    break;
                case "Media Foundation":
                    MediaFoundationMenu.Show();
                    break;
                case "DMO":
                    DmoMenu.Show();
                    break;
                case "Exit":
                    return;
            }
        }
    }
}
