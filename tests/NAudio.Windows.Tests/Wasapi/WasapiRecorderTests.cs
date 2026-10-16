
using System;
using System.Threading;

using NAudio.Wave;
using NAudio.CoreAudioApi;

using NUnit.Framework;

namespace NAudio.Windows.Tests.Wasapi;

/// <summary>
/// Tests covering WASAPI recorder currently for issue #1479. <br />
/// The issue describes that the WasapiRecorder failed to re-start recording. <br />
/// The below tests now ensure that the issue won't bubble up ever again by simulating 
/// the issue's conditions.
/// </summary>
[TestFixture]
[Platform("Win")]
[Category("IntegrationTest")]
public class WasapiRecorderTests
{
    /**
     * The test verifying that the issue #1479 is fixed: Creates a recorder,
     * starts recording, stops it, and re-starts it.
     */
    [Test]
    public void VerifyThatReinitializationIsSafe_WasapiRecorder()
    {
        var recorder = new WasapiRecorderBuilder().Build();

        Exception exception = null;
        ManualResetEventSlim stopEventWasCalled = new(false);

        recorder.RecordingStopped += (sender, e) => { stopEventWasCalled.Set(); exception = e.Exception; };

        // Start recording: Initialize the recorder, stop it and re-initialize again.
        Assert.DoesNotThrow(recorder.StartRecording);

        // Simulate that we are doing some work.
        Thread.Sleep(100);

        Assert.DoesNotThrow(recorder.StopRecording);

        // Wait for the recording to stop in a time window of 5 seconds.
        Assert.That(stopEventWasCalled.Wait(5000), Is.True, "The RecordingStopped event was not raised.");
        if (exception is not null)
        {
            Assert.Fail($"Failure returned by the recording stopped event: {exception}");
        }

        // Now re-start recording.
        // This is the point that #1479 described: The recording here was originally failed to start.
        Assert.DoesNotThrow(recorder.StartRecording);

        // TEMPORARY: Recording fails when using EventSync with the new code.
        // Old WasapiCapture is not affected.
        // This is temporary fix, the real code should be:
        // Assert.DoesNotThrow(recorder.Dispose);

        Assert.DoesNotThrow(recorder.StopRecording);

        Thread.Sleep(100);

        Assert.DoesNotThrow(recorder.Dispose);
    }

    // Same test as above, but verifies the WasapiCapture class instead.
    [Test]
    public void VerifyThatReinitializationIsSafe_WasapiCapture()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var recorder = new WasapiCapture();
#pragma warning restore CS0618 // Type or member is obsolete

        Exception exception = null;
        ManualResetEventSlim stopEventWasCalled = new(false);

        recorder.RecordingStopped += (sender, e) => { stopEventWasCalled.Set(); exception = e.Exception; };

        // Start recording: Initialize the recorder, stop it and re-initialize again.
        Assert.DoesNotThrow(recorder.StartRecording);

        // Simulate that we are doing some work.
        Thread.Sleep(100);

        Assert.DoesNotThrow(recorder.StopRecording);

        // Wait for the recording to stop in a time window of 5 seconds.
        Assert.That(stopEventWasCalled.Wait(5000), Is.True, "The RecordingStopped event was not raised.");
        if (exception is not null)
        {
            Assert.Fail($"Failure returned by the recording stopped event: {exception}");
        }

        // Now re-start recording.
        // This is the point that #1479 described: The recording here was originally failed to start.
        Assert.DoesNotThrow(recorder.StartRecording);

        Assert.DoesNotThrow(recorder.Dispose);
    }

    // Same test as above, but verifies the WasapiLoopbackCapture class instead.
    [Test]
    public void VerifyThatReinitializationIsSafe_WasapiLoopbackCapture()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var recorder = new WasapiLoopbackCapture();
#pragma warning restore CS0618 // Type or member is obsolete

        Exception exception = null;
        ManualResetEventSlim stopEventWasCalled = new(false);

        recorder.RecordingStopped += (sender, e) => { stopEventWasCalled.Set(); exception = e.Exception; };

        // Start recording: Initialize the recorder, stop it and re-initialize again.
        Assert.DoesNotThrow(recorder.StartRecording);

        // Simulate that we are doing some work.
        Thread.Sleep(100);

        Assert.DoesNotThrow(recorder.StopRecording);

        // Wait for the recording to stop in a time window of 5 seconds.
        Assert.That(stopEventWasCalled.Wait(5000), Is.True, "The RecordingStopped event was not raised.");
        if (exception is not null)
        {
            Assert.Fail($"Failure returned by the recording stopped event: {exception}");
        }

        // Now re-start recording.
        // This is the point that #1479 described: The recording here was originally failed to start.
        Assert.DoesNotThrow(recorder.StartRecording);

        Assert.DoesNotThrow(recorder.Dispose);
    }
}
