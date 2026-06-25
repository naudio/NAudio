using Spectre.Console;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Menu-side glue: looks up a test by id, prompts for any missing parameters, invokes it, and
/// renders the result with the standard "press any key to continue" tail. All <c>*Menu.cs</c>
/// files call this once per menu choice — no per-test switch arms are needed beyond mapping
/// the user-visible label to a test id (and eventually that mapping disappears too when the
/// menus are auto-built from <see cref="TestRegistry"/>).
/// </summary>
static class InteractiveTestLauncher
{
    public static void Launch(string id, IReadOnlyDictionary<string, object?>? seed = null)
    {
        if (!TestRegistry.TryGet(id, out var test))
        {
            AnsiConsole.MarkupLine($"\n[red]Test '{Markup.Escape(id)}' is not registered.[/]");
            PressAnyKey();
            return;
        }

        IReadOnlyDictionary<string, object?> values;
        try
        {
            values = ParameterPrompter.Prompt(test, seed);
        }
        catch (Exception ex)
        {
            // Prompt cancelled (e.g. Ctrl-C) or other input failure — surface it but don't crash the menu loop.
            AnsiConsole.MarkupLine($"\n[yellow]Cancelled: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        var result = ConsoleTestRunner.InvokeWithCancellation(test, values, interactive: true);
        ConsoleTestRunner.PrintResult(result);
        PressAnyKey();
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
