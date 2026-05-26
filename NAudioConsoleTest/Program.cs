using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest;

static class Program
{
    // ASIO drivers are COM objects that require the calling thread to be in an STA apartment.
    // Without [STAThread] the first AsioDevice.Open call fails on most native drivers.
    [STAThread]
    static int Main(string[] args)
    {
        TestRegistration.RegisterAll();

        if (CliDispatcher.TryHandle(args, out var cliExitCode))
            return cliExitCode;

        var banner = new Rows(
            new FigletText("NAudio").Color(Color.Blue),
            new Markup("[dim]Interactive Audio Test Harness[/]"));

        while (true)
        {
            var choice = Menu.Show("Main Menu", banner,
                new Menu.Group("", "WASAPI", "ASIO", "WinMM", "DirectSound", "Media Foundation", "Sound File", "DMO", "DSP", "Exit"));

            // Escape at the top level exits the app.
            if (choice is null) return 0;

            switch (choice)
            {
                case "WASAPI":
                    MenuRenderer.Show("WASAPI");
                    break;
                case "ASIO":
                    MenuRenderer.Show("ASIO (AsioDevice — NAudio 3)");
                    break;
                case "WinMM":
                    MenuRenderer.Show("WinMM (Windows Multimedia)");
                    break;
                case "DirectSound":
                    MenuRenderer.Show("DirectSound");
                    break;
                case "Media Foundation":
                    MenuRenderer.Show("Media Foundation");
                    break;
                case "Sound File":
                    MenuRenderer.Show("Sound File (libsndfile)");
                    break;
                case "DMO":
                    MenuRenderer.Show("DMO (DirectX Media Objects)");
                    break;
                case "DSP":
                    MenuRenderer.Show("DSP");
                    break;
                case "Exit":
                    return 0;
            }
        }
    }
}
