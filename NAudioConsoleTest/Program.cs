using NAudioConsoleTest.Dmo;
using NAudioConsoleTest.MediaFoundation;
using NAudioConsoleTest.Wasapi;
using Spectre.Console;

AnsiConsole.Write(new FigletText("NAudio").Color(Color.Blue));
AnsiConsole.MarkupLine("[dim]Interactive Audio Test Harness[/]\n");

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold]Main Menu[/]")
            .AddChoices("WASAPI", "Media Foundation", "DMO", "Exit"));

    switch (choice)
    {
        case "WASAPI":
            WasapiMenu.Show();
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
