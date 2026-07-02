using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Wasapi;

/// <summary>
/// Builder-validation tests for raw mode (WithRawMode). These exercise pure builder logic — the
/// rejection of incompatible option combinations — without activating any audio device, so they need
/// no hardware and are not marked IntegrationTest. Whether the device actually honours raw mode is
/// covered by integration tests instead.
/// </summary>
[TestFixture]
public class WasapiRawModeBuilderTests
{
    [Test]
    public void Recorder_RawMode_WithCommunicationsMode_BuildThrows()
    {
        var builder = new WasapiRecorderBuilder().WithRawMode().WithCommunicationsMode();
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(ex.Message, Does.Contain("Raw mode"));
    }

    [Test]
    public void Recorder_CommunicationsMode_ThenRawMode_BuildThrows()
    {
        // Order of builder calls must not matter — validation happens at Build time.
        var builder = new WasapiRecorderBuilder().WithCommunicationsMode().WithRawMode();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void Recorder_RawMode_WithEchoCancellationReference_BuildThrows()
    {
        // WithEchoCancellationReferenceEndpoint implies communications mode, which raw mode contradicts.
        var builder = new WasapiRecorderBuilder().WithRawMode().WithEchoCancellationReferenceEndpoint();
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(ex.Message, Does.Contain("Raw mode"));
    }

    [Test]
    public void Recorder_RawMode_WithProcessLoopback_BuildAsyncThrows()
    {
        var builder = new WasapiRecorderBuilder().WithRawMode().WithProcessLoopback(1234);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.That(ex.Message, Does.Contain("process-loopback").IgnoreCase);
    }
}
