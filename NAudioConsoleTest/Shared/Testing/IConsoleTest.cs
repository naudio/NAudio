namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Contract every interactive/automatable test in the harness implements. Both the Spectre
/// menu and the command-line front door discover tests through <see cref="TestRegistry"/> and
/// invoke them via <see cref="Run"/> — the test body itself doesn't care which one called it.
/// </summary>
public interface IConsoleTest
{
    /// <summary>Stable, period-separated identifier used by the CLI (e.g. <c>"Asio.ListDrivers"</c>).</summary>
    string Id { get; }

    /// <summary>One-line description shown by <c>describe</c> and in the menu's hint area.</summary>
    string Description { get; }

    /// <summary>Where this test appears in the menu, or null for CLI-only tests.</summary>
    MenuPath? MenuLocation { get; }

    /// <summary>Parameters this test accepts. Empty list for zero-param tests.</summary>
    IReadOnlyList<TestParameter> Parameters { get; }

    /// <summary>Run the test. Throwing is allowed — the harness converts exceptions to a failed result.</summary>
    TestResult Run(TestContext context);
}
