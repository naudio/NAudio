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
        var banner = new Rows(
            new FigletText("NAudio").Color(Color.Blue),
            new Markup("[dim]Interactive Audio Test Harness[/]"));

        while (true)
        {
            var choice = Menu.Show("Main Menu", banner,
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
