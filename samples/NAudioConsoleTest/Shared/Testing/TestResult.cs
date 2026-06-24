namespace NAudioConsoleTest.Shared.Testing;

public enum TestOutcome
{
    Pass,
    Fail,
    Skipped,
    NotAutomatable,
}

/// <summary>
/// Result of running a single test. <see cref="Diagnostics"/> is an optional bag of
/// key/value strings the test wants surfaced in batch reports (e.g. <c>output-size=12345</c>).
/// </summary>
public sealed record TestResult(
    TestOutcome Outcome,
    string? Message = null,
    IReadOnlyDictionary<string, string>? Diagnostics = null)
{
    public static TestResult Pass(string? message = null,
        IReadOnlyDictionary<string, string>? diagnostics = null)
        => new(TestOutcome.Pass, message, diagnostics);

    public static TestResult Fail(string message,
        IReadOnlyDictionary<string, string>? diagnostics = null)
        => new(TestOutcome.Fail, message, diagnostics);

    public static TestResult Skipped(string reason,
        IReadOnlyDictionary<string, string>? diagnostics = null)
        => new(TestOutcome.Skipped, reason, diagnostics);
    public static TestResult NotAutomatable(string reason) => new(TestOutcome.NotAutomatable, reason);
}
