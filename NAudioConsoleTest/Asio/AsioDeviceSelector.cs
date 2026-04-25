using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioDeviceSelector
{
    /// <summary>
    /// Prompts the user to pick an ASIO driver, returning null if none are installed or the user cancels.
    /// </summary>
    public static string? SelectDriver()
    {
        var drivers = AsioDevice.GetDriverNames();
        if (drivers.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No ASIO drivers installed on this system.[/]");
            AnsiConsole.MarkupLine("[dim]Try installing ASIO4ALL (https://asio4all.com/) to use WDM audio devices as ASIO.[/]");
            AnsiConsole.MarkupLine("[dim]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return null;
        }

        var choices = drivers.Concat(["Cancel"]).ToArray();
        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an ASIO driver:")
                .AddChoices(choices));
        return pick == "Cancel" ? null : pick;
    }

    /// <summary>
    /// Prompts the user to pick a subset of channels from the given channel count.
    /// Returns null on cancel; otherwise an int[] of zero-based physical channel indices.
    /// </summary>
    public static int[]? SelectChannels(string prompt, int availableChannels)
    {
        if (availableChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]Device reports 0 available channels in this direction.[/]");
            return null;
        }

        var options = Enumerable.Range(0, availableChannels)
            .Select(i => $"Channel {i}")
            .ToList();

        var picked = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title(prompt)
                .NotRequired()
                .InstructionsText("[grey](space to toggle, enter to confirm)[/]")
                .PageSize(Math.Min(15, availableChannels))
                .AddChoices(options));

        if (picked.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No channels selected.[/]");
            return null;
        }

        return picked.Select(p => int.Parse(p["Channel ".Length..])).ToArray();
    }
}
