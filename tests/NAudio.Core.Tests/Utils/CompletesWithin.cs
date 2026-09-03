using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using NUnit.Framework;

namespace NAudio.Core.Tests.Utils;

/// <summary>
/// Helper for regression tests that guard against parser infinite loops. A spinning loop
/// can't be cancelled cooperatively, so the work runs on a background thread and the test
/// fails (rather than hanging the whole run) if it doesn't finish in time.
/// </summary>
internal static class CompletesWithin
{
    private const int DefaultTimeoutMilliseconds = 10000;

    /// <summary>
    /// Runs <paramref name="action"/> on a background thread, failing the test if it hasn't
    /// returned (normally or by throwing) within the timeout. Any exception it threw is
    /// rethrown on the calling thread so the caller can assert on it.
    /// </summary>
    public static void Run(Action action, int timeoutMilliseconds = DefaultTimeoutMilliseconds)
    {
        Exception thrown = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { thrown = ex; }
        })
        { IsBackground = true };

        thread.Start();
        if (!thread.Join(timeoutMilliseconds))
        {
            Assert.Fail($"Operation did not complete within {timeoutMilliseconds}ms - it is most likely stuck in an infinite loop.");
        }
        if (thrown != null)
        {
            ExceptionDispatchInfo.Capture(thrown).Throw();
        }
    }
}
