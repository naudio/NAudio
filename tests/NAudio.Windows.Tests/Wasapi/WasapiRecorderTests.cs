
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

    // A format no endpoint supports in exclusive mode, so starting always fails.
    private static WaveFormat UnsupportedExclusiveFormat => new(12345, 8, 7);

    /**
     * A failed start must leave the recorder Stopped (#1490), and a retry must fail with the
     * same error again rather than "Already recording".
     */
    [Test]
    public void FailedStartLeavesRecorderStopped_WasapiRecorder()
    {
        using var recorder = new WasapiRecorderBuilder()
            .WithExclusiveMode()
            .WithFormat(UnsupportedExclusiveFormat)
            .Build();

        var first = Assert.Catch(recorder.StartRecording);
        Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));

        var second = Assert.Catch(recorder.StartRecording);
        Assert.That(second.GetType(), Is.EqualTo(first.GetType()));
        Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
    }

    // Same as above, but through CaptureAsync.
    [Test]
    public void FailedCaptureAsyncLeavesRecorderStopped_WasapiRecorder()
    {
        using var recorder = new WasapiRecorderBuilder()
            .WithExclusiveMode()
            .WithFormat(UnsupportedExclusiveFormat)
            .Build();

        Assert.CatchAsync(async () => { await foreach (var _ in recorder.CaptureAsync()) { } });
        Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
    }

    // Same as above, but verifies the WasapiCapture class instead.
    [Test]
    public void FailedStartLeavesRecorderStopped_WasapiCapture()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        using var capture = new WasapiCapture();
#pragma warning restore CS0618 // Type or member is obsolete
        capture.ShareMode = AudioClientShareMode.Exclusive;
        capture.WaveFormat = UnsupportedExclusiveFormat;

        var first = Assert.Catch(capture.StartRecording);
        Assert.That(capture.CaptureState, Is.EqualTo(CaptureState.Stopped));

        var second = Assert.Catch(capture.StartRecording);
        Assert.That(second.GetType(), Is.EqualTo(first.GetType()));
        Assert.That(capture.CaptureState, Is.EqualTo(CaptureState.Stopped));
    }

    /**
     * When initialization fails after IAudioClient::Initialize has already succeeded (here: the
     * endpoint has no AEC reference control), a retry must report the same NotSupportedException,
     * not AUDCLNT_E_ALREADY_INITIALIZED from initializing the same audio client twice.
     */
    [Test]
    public void RetryAfterFailureLateInInitialization_WasapiRecorder()
    {
        using var recorder = new WasapiRecorderBuilder()
            .WithEchoCancellationReferenceEndpoint()
            .Build();

        try
        {
            recorder.StartRecording();
        }
        catch (NotSupportedException)
        {
            Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
            Assert.Throws<NotSupportedException>(recorder.StartRecording);
            return;
        }

        Assert.Ignore("The default capture endpoint supports the AEC reference control, so initialization cannot be made to fail late.");
    }
}
