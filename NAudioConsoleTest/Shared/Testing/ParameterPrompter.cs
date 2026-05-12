using System.Globalization;
using Spectre.Console;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Interactive-mode counterpart to <see cref="CliDispatcher"/>'s argv parser: walks a test's
/// declared parameters and prompts the user for any value not already supplied. The resulting
/// dictionary is the same shape both code paths produce, so the test body never has to know
/// which one filled it in.
/// </summary>
static class ParameterPrompter
{
    public static IReadOnlyDictionary<string, object?> Prompt(IConsoleTest test,
        IReadOnlyDictionary<string, object?>? seed = null)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (seed is not null)
            foreach (var kv in seed) dict[kv.Key] = kv.Value;

        foreach (var p in test.Parameters)
        {
            if (dict.ContainsKey(p.Name)) continue;
            if (p.CliOnly)
            {
                // CliOnly params (e.g. maxDuration caps) get the default in interactive mode —
                // the user controls "stop now" via ESC instead.
                dict[p.Name] = p.Default;
                continue;
            }
            // Custom interactive picker (e.g. ASIO channel multi-select) wins over the default
            // prompt. Receives the dict so it can read previously-prompted values like driver.
            dict[p.Name] = p.InteractivePrompter is not null
                ? p.InteractivePrompter(dict)
                : PromptOne(p);
        }
        return dict;
    }

    private static object? PromptOne(TestParameter p)
    {
        var label = p.Help is null ? p.Name : $"{p.Name} ([dim]{p.Help}[/])";

        var effectiveChoices = p.GetEffectiveChoices();
        if (effectiveChoices is { Count: > 0 } choices)
        {
            var sel = new SelectionPrompt<string>().Title(label).AddChoices(choices);
            var picked = AnsiConsole.Prompt(sel);
            return Convert(picked, p.Type);
        }

        if (p.Type == typeof(string)) return PromptString(p, label);
        if (p.Type == typeof(int)) return PromptOf<int>(p, label);
        if (p.Type == typeof(long)) return PromptOf<long>(p, label);
        if (p.Type == typeof(double)) return PromptOf<double>(p, label);
        if (p.Type == typeof(float)) return PromptOf<float>(p, label);
        if (p.Type == typeof(bool)) return PromptOf<bool>(p, label);
        if (p.Type == typeof(TimeSpan)) return PromptTimeSpan(p, label);
        if (p.Type.IsEnum) return PromptEnum(p, label);

        // Fallback — read as string and convert.
        var raw = AnsiConsole.Prompt(new TextPrompt<string>($"{label}:").AllowEmpty());
        return string.IsNullOrEmpty(raw) ? p.Default : Convert(raw, p.Type);
    }

    private static object? PromptString(TestParameter p, string label)
    {
        var prompt = new TextPrompt<string>($"{label}:").AllowEmpty();
        if (p.Default is string s) prompt.DefaultValue(s);
        if (p.Required && p.Default is null)
            prompt.Validate(v => !string.IsNullOrWhiteSpace(v)
                ? ValidationResult.Success()
                : ValidationResult.Error("required"));
        var value = AnsiConsole.Prompt(prompt);
        return string.IsNullOrEmpty(value) ? p.Default : value;
    }

    private static object PromptOf<T>(TestParameter p, string label) where T : notnull
    {
        var prompt = new TextPrompt<T>($"{label}:");
        if (p.Default is T def) prompt.DefaultValue(def);
        return AnsiConsole.Prompt(prompt);
    }

    private static object PromptTimeSpan(TestParameter p, string label)
    {
        var prompt = new TextPrompt<string>($"{label} (hh:mm:ss or seconds):");
        if (p.Default is TimeSpan ts) prompt.DefaultValue(ts.ToString());
        prompt.Validate(v => TryParseTimeSpan(v, out _)
            ? ValidationResult.Success()
            : ValidationResult.Error("Enter hh:mm:ss, a number of seconds, or 30s/500ms"));
        TryParseTimeSpan(AnsiConsole.Prompt(prompt), out var parsed);
        return parsed;
    }

    private static object PromptEnum(TestParameter p, string label)
    {
        var names = Enum.GetNames(p.Type);
        var sel = new SelectionPrompt<string>().Title(label).AddChoices(names);
        var picked = AnsiConsole.Prompt(sel);
        return Enum.Parse(p.Type, picked);
    }

    /// <summary>Accepts <c>hh:mm:ss</c>, a bare number (seconds), <c>30s</c>, or <c>500ms</c>.</summary>
    public static bool TryParseTimeSpan(string raw, out TimeSpan value)
    {
        raw = raw.Trim();
        if (raw.EndsWith("ms", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(raw[..^2], CultureInfo.InvariantCulture, out var ms))
        {
            value = TimeSpan.FromMilliseconds(ms); return true;
        }
        if (raw.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(raw[..^1], CultureInfo.InvariantCulture, out var s))
        {
            value = TimeSpan.FromSeconds(s); return true;
        }
        if (double.TryParse(raw, CultureInfo.InvariantCulture, out var bare))
        {
            value = TimeSpan.FromSeconds(bare); return true;
        }
        return TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out value);
    }

    private static object? Convert(string raw, Type target)
        => System.Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
}
