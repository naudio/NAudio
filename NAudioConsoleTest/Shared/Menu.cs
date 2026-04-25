using Spectre.Console;

namespace NAudioConsoleTest.Shared;

/// <summary>
/// Minimal interactive menu renderer with keyboard navigation. Unlike Spectre.Console's
/// <c>SelectionPrompt</c>, this one returns <c>null</c> when the user presses Escape — letting
/// each submenu treat Escape as "go back" and the main menu treat it as "exit".
/// </summary>
static class Menu
{
    public sealed class Group
    {
        public string Title { get; }
        public string[] Items { get; }

        public Group(string title, params string[] items)
        {
            Title = title;
            Items = items;
        }
    }

    /// <summary>
    /// Renders a menu with a Rule header, grouped choices, and a hint line. Returns the selected
    /// item string or <c>null</c> if the user pressed Escape.
    /// </summary>
    public static string? Show(string title, params Group[] groups)
    {
        // Flatten into (text, isSelectable, groupTitleOrNull) entries.
        // Group titles are headers (not selectable); items are selectable.
        var entries = new List<(string Text, bool IsItem)>();
        foreach (var g in groups)
        {
            if (!string.IsNullOrEmpty(g.Title))
                entries.Add((g.Title, false));
            foreach (var item in g.Items)
                entries.Add((item, true));
        }

        int selected = entries.FindIndex(e => e.IsItem);
        if (selected < 0) return null;

        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                Render(title, entries, selected);
                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = FindPrev(entries, selected);
                        break;
                    case ConsoleKey.DownArrow:
                        selected = FindNext(entries, selected);
                        break;
                    case ConsoleKey.Home:
                        selected = FindFirst(entries);
                        break;
                    case ConsoleKey.End:
                        selected = FindLast(entries);
                        break;
                    case ConsoleKey.Enter:
                        return entries[selected].Text;
                    case ConsoleKey.Escape:
                        return null;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private static void Render(string title, List<(string Text, bool IsItem)> entries, int selected)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold blue]{Markup.Escape(title)}[/]").LeftJustified());
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]↑/↓ navigate  •  Enter select  •  Esc back[/]");
        AnsiConsole.MarkupLine("");

        for (int i = 0; i < entries.Count; i++)
        {
            var (text, isItem) = entries[i];
            if (!isItem)
            {
                AnsiConsole.MarkupLine($"  [yellow]{Markup.Escape(text)}[/]");
            }
            else if (i == selected)
            {
                AnsiConsole.MarkupLine($"  [black on cyan]▶ {Markup.Escape(text)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"    {Markup.Escape(text)}");
            }
        }
    }

    private static int FindPrev(List<(string Text, bool IsItem)> entries, int from)
    {
        for (int i = from - 1; i >= 0; i--)
            if (entries[i].IsItem) return i;
        for (int i = entries.Count - 1; i > from; i--)
            if (entries[i].IsItem) return i;
        return from;
    }

    private static int FindNext(List<(string Text, bool IsItem)> entries, int from)
    {
        for (int i = from + 1; i < entries.Count; i++)
            if (entries[i].IsItem) return i;
        for (int i = 0; i < from; i++)
            if (entries[i].IsItem) return i;
        return from;
    }

    private static int FindFirst(List<(string Text, bool IsItem)> entries)
        => entries.FindIndex(e => e.IsItem);

    private static int FindLast(List<(string Text, bool IsItem)> entries)
        => entries.FindLastIndex(e => e.IsItem);
}
