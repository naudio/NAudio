using Spectre.Console;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Builds a submenu for a category by reading <see cref="TestRegistry"/> rather than hand-rolling
/// a switch statement per category. Once every test in a category has a <see cref="MenuPath"/>,
/// the legacy <c>*Menu.cs</c> file for that category collapses to a single
/// <c>MenuRenderer.Show("Category Name")</c> call.
/// </summary>
/// <remarks>
/// Group ordering: tests are sorted by <c>MenuPath.Order</c> then label. The first time a group
/// name is encountered (in that sorted scan) sets the group's display position — so giving the
/// earliest test in group A a lower <c>Order</c> than any test in group B places A above B.
/// Tests with a null <c>Group</c> render flat (no group header) and appear above all named groups.
/// </remarks>
internal static class MenuRenderer
{
    public static void Show(string category)
    {
        while (true)
        {
            var tests = TestRegistry.All
                .Where(t => t.MenuLocation?.Category == category)
                .OrderBy(t => t.MenuLocation!.Order)
                .ThenBy(t => t.MenuLocation!.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tests.Count == 0)
            {
                AnsiConsole.MarkupLine($"\n[yellow]No tests registered for category '{Markup.Escape(category)}'.[/]");
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(intercept: true);
                return;
            }

            var byLabel = new Dictionary<string, IConsoleTest>(StringComparer.Ordinal);
            foreach (var t in tests)
            {
                if (!byLabel.TryAdd(t.MenuLocation!.Label, t))
                {
                    AnsiConsole.MarkupLine(
                        $"\n[red]Duplicate menu label in '{Markup.Escape(category)}': " +
                        $"{Markup.Escape(t.MenuLocation.Label)} " +
                        $"(used by {Markup.Escape(t.Id)} and {Markup.Escape(byLabel[t.MenuLocation.Label].Id)})[/]");
                    AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                    Console.ReadKey(intercept: true);
                    return;
                }
            }

            var menuGroups = BuildGroups(tests);
            menuGroups.Add(new Menu.Group("", "Back"));

            var choice = Menu.Show(category, menuGroups.ToArray());
            if (choice is null or "Back") return;

            if (byLabel.TryGetValue(choice, out var picked))
            {
                InteractiveTestLauncher.Launch(picked.Id);
            }
        }
    }

    private static List<Menu.Group> BuildGroups(List<IConsoleTest> sortedTests)
    {
        // Track the position of each named group by the first test that introduces it.
        var groupOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        var seen = 0;
        foreach (var t in sortedTests)
        {
            var g = t.MenuLocation!.Group;
            if (g is not null && !groupOrder.ContainsKey(g))
                groupOrder[g] = seen++;
        }

        var ungrouped = sortedTests
            .Where(t => t.MenuLocation!.Group is null)
            .Select(t => t.MenuLocation!.Label)
            .ToArray();

        var named = sortedTests
            .Where(t => t.MenuLocation!.Group is not null)
            .GroupBy(t => t.MenuLocation!.Group!, StringComparer.Ordinal)
            .OrderBy(g => groupOrder[g.Key])
            .ToList();

        var result = new List<Menu.Group>();
        if (ungrouped.Length > 0)
            result.Add(new Menu.Group("", ungrouped));
        foreach (var g in named)
            result.Add(new Menu.Group(g.Key, g.Select(t => t.MenuLocation!.Label).ToArray()));
        return result;
    }
}
