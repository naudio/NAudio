using System.Text;
using NAudioAsioFakeDriverTest;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Asio;

/// <summary>
/// Runs NAudio.Asio's whole native binding (vtable, struct layouts, strings, callbacks) against an
/// in-process fake driver, so it's covered without ASIO hardware. The checks live in
/// tests/NAudioAsioFakeDriverTest, which CI also publishes and runs with Native AOT.
/// </summary>
[TestFixture]
[Category("UnitTest")]
[NonParallelizable] // the fake driver keeps its state in statics
public class AsioFakeDriverTests
{
    [Test]
    public void BindingWorksAgainstFakeDriver()
    {
        var log = new StringBuilder();
        int failures = FakeAsioDriverChecks.Run(line => log.AppendLine(line));
        TestContext.Out.WriteLine(log.ToString());
        Assert.That(failures, Is.Zero, log.ToString());
    }
}
