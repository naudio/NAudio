using Spectre.Console;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Wraps an <see cref="IConsoleTest"/> invocation with exception → <see cref="TestResult.Fail"/>
/// translation. Both the menu and the CLI route through this so test bodies never need to
/// catch their own exceptions just to format them.
/// </summary>
public static class ConsoleTestRunner
{
    /// <summary>
    /// Builds a <see cref="TestContext"/>, wires <see cref="Console.CancelKeyPress"/> to a
    /// fresh <see cref="CancellationTokenSource"/> so Ctrl+C cancels just the running test
    /// (the second Ctrl+C is left to the runtime so the process can still be killed), and
    /// invokes the test. Both menu and CLI paths use this — test bodies just poll
    /// <c>ctx.Cancellation</c> and trust that something else handled Ctrl+C.
    /// </summary>
    public static TestResult InvokeWithCancellation(IConsoleTest test,
        IReadOnlyDictionary<string, object?> values, bool interactive)
    {
        using var cts = new CancellationTokenSource();
        void Handler(object? sender, ConsoleCancelEventArgs e)
        {
            // First Ctrl+C cancels the test but keeps the process alive; second one falls through.
            if (cts.IsCancellationRequested) return;
            e.Cancel = true;
            cts.Cancel();
        }
        Console.CancelKeyPress += Handler;
        try
        {
            var ctx = new TestContext(values, interactive, cts.Token);
            return Invoke(test, ctx);
        }
        finally
        {
            Console.CancelKeyPress -= Handler;
        }
    }

    public static TestResult Invoke(IConsoleTest test, TestContext context)
    {
        try
        {
            return test.Run(context);
        }
        catch (OperationCanceledException)
        {
            return TestResult.Fail("Cancelled");
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrWhiteSpace(ex.Message) ? "(no message)" : ex.Message;
            return TestResult.Fail($"{ex.GetType().Name}: {message}");
        }
    }

    /// <summary>
    /// Renders a <see cref="TestResult"/> for human consumption (used by the interactive menu).
    /// CLI/batch callers should render their own machine-readable form instead.
    /// </summary>
    public static void PrintResult(TestResult result)
    {
        switch (result.Outcome)
        {
            case TestOutcome.Pass:
                if (!string.IsNullOrEmpty(result.Message))
                    AnsiConsole.MarkupLine($"\n[green]{Markup.Escape(result.Message)}[/]");
                break;
            case TestOutcome.Fail:
                AnsiConsole.MarkupLine($"\n[red]FAILED: {Markup.Escape(result.Message ?? "")}[/]");
                break;
            case TestOutcome.Skipped:
                AnsiConsole.MarkupLine($"\n[yellow]Skipped: {Markup.Escape(result.Message ?? "")}[/]");
                break;
            case TestOutcome.NotAutomatable:
                AnsiConsole.MarkupLine($"\n[yellow]Not automatable: {Markup.Escape(result.Message ?? "")}[/]");
                break;
        }
    }
}
