using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Shared driver enumeration and channel-list parsing for ASIO tests. Drivers are identified
/// by their installed friendly name; channel lists are comma-separated integers like <c>"0,1"</c>.
/// </summary>
static class AsioDrivers
{
    public static IReadOnlyList<string> DriverNames() => AsioDevice.GetDriverNames();

    /// <summary>
    /// Opens an ASIO device by friendly name, returning null when the name doesn't match any
    /// installed driver (so the caller can return <c>TestResult.Fail</c> with a clear message
    /// instead of letting <c>AsioDevice.Open</c> throw).
    /// </summary>
    public static AsioDevice? TryOpen(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var drivers = AsioDevice.GetDriverNames();
        if (!drivers.Any(d => string.Equals(d, name, StringComparison.OrdinalIgnoreCase)))
            return null;
        return AsioDevice.Open(name);
    }

    /// <summary>
    /// Parses a comma-separated channel list (e.g. <c>"0,1"</c>) into a zero-based int array.
    /// Rejects empty lists, non-numeric entries, duplicates, and out-of-range indices.
    /// </summary>
    public static bool TryParseChannels(string raw, int maxExclusive, out int[] channels, out string error)
    {
        channels = [];
        error = "";
        if (string.IsNullOrWhiteSpace(raw)) { error = "channel list is empty"; return false; }
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) { error = "channel list is empty"; return false; }

        var list = new int[parts.Length];
        var seen = new HashSet<int>();
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var idx))
            {
                error = $"invalid channel index '{parts[i]}'";
                return false;
            }
            if (idx < 0 || idx >= maxExclusive)
            {
                error = $"channel {idx} out of range (driver has 0..{maxExclusive - 1})";
                return false;
            }
            if (!seen.Add(idx))
            {
                error = $"duplicate channel index {idx}";
                return false;
            }
            list[i] = idx;
        }
        channels = list;
        return true;
    }

    /// <summary>
    /// Interactive multi-select for ASIO input channels. Reads the chosen driver from the
    /// already-prompted values, probes its input count, and presents a Spectre multi-select.
    /// Returns the same comma-separated string format the CLI accepts (e.g. <c>"0,1"</c>) so
    /// test bodies can keep parsing through <see cref="TryParseChannels"/>.
    /// </summary>
    public static object? PickInputChannels(IReadOnlyDictionary<string, object?> prior)
        => PickChannels(prior, input: true);

    /// <inheritdoc cref="PickInputChannels"/>
    public static object? PickOutputChannels(IReadOnlyDictionary<string, object?> prior)
        => PickChannels(prior, input: false);

    private static object? PickChannels(IReadOnlyDictionary<string, object?> prior, bool input)
    {
        var driverName = prior.TryGetValue("driver", out var d) ? d as string : null;
        if (string.IsNullOrWhiteSpace(driverName))
        {
            // Driver wasn't selected (cancelled or missing) — fall back to a text prompt so the
            // user can still hand-enter or the test can fail downstream with a clear message.
            return AnsiConsole.Prompt(new TextPrompt<string>(
                $"{(input ? "input" : "output")}Channels (comma-separated, e.g. 0,1):").AllowEmpty());
        }

        int available;
        using (var probe = TryOpen(driverName))
        {
            if (probe is null)
            {
                AnsiConsole.MarkupLine($"[red]Could not open ASIO driver '{Markup.Escape(driverName)}'.[/]");
                return "";
            }
            available = input ? probe.Capabilities.NbInputChannels : probe.Capabilities.NbOutputChannels;
        }

        if (available == 0)
        {
            AnsiConsole.MarkupLine($"[red]Driver has no {(input ? "input" : "output")} channels.[/]");
            return "";
        }

        var options = Enumerable.Range(0, available).Select(i => $"Channel {i}").ToList();
        var picked = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"Select {(input ? "input" : "output")} channels:")
                .NotRequired()
                .InstructionsText("[grey](space to toggle, enter to confirm)[/]")
                .PageSize(Math.Min(15, available))
                .AddChoices(options));

        if (picked.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No channels selected.[/]");
            return "";
        }

        return string.Join(",", picked.Select(p => int.Parse(p["Channel ".Length..])));
    }
}
