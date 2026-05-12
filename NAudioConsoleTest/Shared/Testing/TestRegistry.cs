namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Central registry of every <see cref="IConsoleTest"/> in the harness. Tests are hand-registered
/// here (no reflection scan) so the list stays explicit and trim-friendly. Both the menu builder
/// and the CLI dispatch read from <see cref="All"/>.
/// </summary>
public static class TestRegistry
{
    private static readonly Dictionary<string, IConsoleTest> tests = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(IConsoleTest test)
    {
        if (tests.ContainsKey(test.Id))
            throw new InvalidOperationException($"Duplicate test id: {test.Id}");
        tests[test.Id] = test;
    }

    public static bool TryGet(string id, out IConsoleTest test)
        => tests.TryGetValue(id, out test!);

    public static IReadOnlyCollection<IConsoleTest> All => tests.Values;
}
