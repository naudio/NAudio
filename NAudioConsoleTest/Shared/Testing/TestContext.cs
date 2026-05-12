namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Per-invocation state passed to <see cref="IConsoleTest.Run"/>. Tests read parameter values
/// from <see cref="Get{T}"/> rather than prompting the user directly — both menu and CLI fill
/// the same dictionary so the test body is identical in either mode.
/// </summary>
public sealed class TestContext
{
    private readonly IReadOnlyDictionary<string, object?> values;

    public TestContext(IReadOnlyDictionary<string, object?> values, bool interactive, CancellationToken cancellation)
    {
        this.values = values;
        Interactive = interactive;
        Cancellation = cancellation;
    }

    /// <summary>True when launched from the menu; false in CLI/batch mode.</summary>
    public bool Interactive { get; }

    public CancellationToken Cancellation { get; }

    public T Get<T>(string name)
    {
        if (!values.TryGetValue(name, out var raw))
            throw new KeyNotFoundException($"Parameter '{name}' was not provided.");
        return (T)raw!;
    }

    public bool TryGet<T>(string name, out T value)
    {
        if (values.TryGetValue(name, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }
}
