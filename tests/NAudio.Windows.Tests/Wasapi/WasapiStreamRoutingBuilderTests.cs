using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Wasapi;

/// <summary>
/// Builder-validation tests for automatic stream routing (WithDefaultDeviceStreamRouting).
/// These exercise pure builder logic — the rejection of incompatible option combinations and the
/// sync/async build split — without activating any audio device, so they need no hardware and are
/// not marked IntegrationTest. (Actually activating the routing endpoint is covered separately.)
/// </summary>
[TestFixture]
public class WasapiStreamRoutingBuilderTests
{
    // ---- WasapiPlayerBuilder ----

    [Test]
    public void PlayerRouting_Build_ThrowsDirectingToBuildAsync()
    {
        var builder = new WasapiPlayerBuilder().WithDefaultDeviceStreamRouting();
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(ex.Message, Does.Contain("BuildAsync"));
    }

    [Test]
    public void PlayerRouting_WithExclusiveMode_BuildAsyncThrows()
    {
        var builder = new WasapiPlayerBuilder().WithDefaultDeviceStreamRouting().WithExclusiveMode();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.That(ex.Message, Does.Contain("shared mode"));
    }

    [Test]
    public void PlayerRouting_WithLowLatency_BuildAsyncThrows()
    {
        var builder = new WasapiPlayerBuilder().WithDefaultDeviceStreamRouting().WithLowLatency();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.That(ex.Message, Does.Contain("low latency").IgnoreCase);
    }

    // (The WithDevice + routing conflict needs a real MMDevice, which requires audio hardware,
    // so it is exercised by integration tests rather than here.)

    // ---- WasapiRecorderBuilder ----

    [Test]
    public void RecorderRouting_Build_ThrowsDirectingToBuildAsync()
    {
        var builder = new WasapiRecorderBuilder().WithDefaultDeviceStreamRouting();
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(ex.Message, Does.Contain("BuildAsync"));
    }

    [Test]
    public void RecorderRouting_WithExclusiveMode_BuildAsyncThrows()
    {
        var builder = new WasapiRecorderBuilder().WithDefaultDeviceStreamRouting().WithExclusiveMode();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.That(ex.Message, Does.Contain("shared mode"));
    }

    [Test]
    public void RecorderRouting_WithLoopbackCapture_BuildAsyncThrows()
    {
        var builder = new WasapiRecorderBuilder().WithDefaultDeviceStreamRouting().WithLoopbackCapture();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.That(ex.Message, Does.Contain("WithLoopbackCapture"));
    }

    [Test]
    public void RecorderRouting_WithLowLatency_BuildAsyncThrows()
    {
        var builder = new WasapiRecorderBuilder().WithDefaultDeviceStreamRouting().WithLowLatency();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync());
        Assert.That(ex.Message, Does.Contain("low latency").IgnoreCase);
    }
}
