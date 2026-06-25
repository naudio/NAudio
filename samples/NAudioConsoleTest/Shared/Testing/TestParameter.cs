namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Metadata for a single parameter a test accepts. The CLI parser uses this to validate
/// <c>--name=value</c> input; the menu prompter uses it to interactively ask for missing values.
/// </summary>
/// <remarks>
/// <see cref="Choices"/> is a static list — use it when the valid values are known at compile
/// time (sample rates, encoding modes). <see cref="ChoiceProvider"/> is a lazy callback that
/// the prompter invokes at run time — use it for choices that depend on the machine
/// (installed audio devices, ASIO drivers, etc.). If both are supplied, <see cref="Choices"/>
/// wins.
/// <para><see cref="CliOnly"/> marks parameters that only make sense in non-interactive use
/// (e.g. <c>maxDuration</c> caps for playback tests — interactively the user just presses ESC).
/// The prompter skips them and falls through to <see cref="Default"/>; the CLI still parses
/// them; <c>describe</c> tags them so script authors know they exist.</para>
/// <para><see cref="InteractivePrompter"/> overrides the default Spectre prompt in menu mode
/// only — use it for richer pickers like ASIO channel multi-select. The callback receives the
/// values already prompted for earlier parameters (so a channel picker can read the chosen
/// driver) and returns the value in the same shape the CLI parser would produce (typically a
/// string), so the test body doesn't have to branch on origin.</para>
/// <para><see cref="IsFilePath"/> marks a string parameter as a path to an existing file. In menu
/// mode the prompter then offers a most-recently-used picker (see
/// <c>FilePathPrompter</c> / <c>RecentFilesStore</c>) so the user can re-select a previous file
/// instead of pasting a path each time. <see cref="FileCategory"/> groups the remembered files —
/// tests that consume the same kind of file (e.g. <c>"audio"</c>) share one recent list. The CLI
/// path is unaffected (it still takes the literal <c>--name=value</c>).</para>
/// </remarks>
public sealed record TestParameter(
    string Name,
    Type Type,
    bool Required,
    object? Default = null,
    string? Help = null,
    IReadOnlyList<string>? Choices = null,
    Func<IReadOnlyList<string>>? ChoiceProvider = null,
    bool CliOnly = false,
    Func<IReadOnlyDictionary<string, object?>, object?>? InteractivePrompter = null,
    bool IsFilePath = false,
    string? FileCategory = null)
{
    /// <summary>Returns the static <see cref="Choices"/> if set, otherwise invokes the
    /// <see cref="ChoiceProvider"/>. Returns null when neither is supplied.</summary>
    public IReadOnlyList<string>? GetEffectiveChoices()
        => Choices ?? ChoiceProvider?.Invoke();
}
