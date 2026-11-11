
using System;
using System.Threading;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.MacOS.CoreAudio;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioQueueTests;

[TestFixture]
[Category("IntegrationTest")]
public class AudioQueueRecorderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 5);

    [Test]
    public void RecordFromDefaultDevice_TryWithEvent()
    {
        AudioQueueRecorder recorder = new AudioQueueRecorderBuilder().Build();

        int recordedBytes = 0;

        recorder.DataAvailable += (o, _, _) =>
        {
            recordedBytes += o.Length;
        };

        recorder.RecordingStopped += (sender, args) =>
        {
            if (args.Exception is not null)
            {
                throw args.Exception;
            }
        };

        Assert.DoesNotThrow(recorder.StartRecording);

        Thread.Sleep(2000);

        Assert.That(recordedBytes, Is.GreaterThan(0));

        Assert.DoesNotThrow(recorder.StopRecording);

        Assert.DoesNotThrow(recorder.Dispose);
    }

    [Test]
    public async Task RecordFromDefaultDevice_TryWithAsyncModel()
    {
        AudioQueueRecorder recorder = new AudioQueueRecorderBuilder().Build();

        Exception capturedException = null;
        bool dataAvailableDetected = false;
        int recordedBytes = 0;
        // PENDING: Adding async support to the recorder.
        // int I = 0;

        recorder.DataAvailable += (o, _, _) =>
        {
            dataAvailableDetected = true;
        };

        recorder.RecordingStopped += (sender, args) =>
        {
            capturedException = args.Exception;
        };

        // PENDING: Adding async support to the recorder.

        /*

        // Attempt to capture 15 buffers.
        await foreach (CoreAudioCaptureBuffer buffer in recorder.CaptureAsync())
        {
            recordedBytes += buffer.Buffer.Length;
            if (I == 15) { break; }
            I++;
        }
        */

        if (dataAvailableDetected)
        {
            Assert.Fail("Should not return data while using the async model");
        }

        if (capturedException is not null)
        {
            Assert.Fail("The player reported an exception: " + capturedException);
        }

        Assert.DoesNotThrow(() =>
        {
            var cf = recorder.CaptureFormat;
            Assert.That(cf, Is.Not.Null, "Capture format cannot be null!");
            if (cf is WaveFormatExtensible extFormat)
            {
                System.Console.WriteLine(
                    "Capture Format:\n" +
                    "Sample Rate: {0}\n" +
                    "Channels: {1}\n" +
                    "Bits per sample: {2}\n" +
                    "Avg. bytes per second: {3}\n" +
                    "Block align: {4}\n" +
                    "Valid bits per sample: {5}\n" +
                    "Channel mask: {6}\n" +
                    "Sub format: {7}",
                    extFormat.SampleRate,
                    extFormat.Channels,
                    extFormat.BitsPerSample,
                    extFormat.AverageBytesPerSecond,
                    extFormat.BlockAlign,
                    extFormat.ValidBitsPerSample,
                    (Speakers)extFormat.ChannelMask,
                    extFormat.SubFormat
                );
            }
            else
            {
                System.Console.WriteLine(
                    "Capture Format:\n" +
                    "Sample Rate: {0}\n" +
                    "Channels: {1}\n" +
                    "Bits per sample: {2}\n" +
                    "Avg. bytes per second: {3}\n" +
                    "Block align: {4}\n" +
                    "Encoding: {5}",
                    cf.SampleRate,
                    cf.Channels,
                    cf.BitsPerSample,
                    cf.AverageBytesPerSecond,
                    cf.BlockAlign,
                    cf.Encoding
                );
            }
        });

        Assert.That(recordedBytes, Is.GreaterThan(0));

        Assert.DoesNotThrow(recorder.StopRecording);

        await recorder.DisposeAsync();
    }

    /**
     * The test verifying that the issue #1479 is fixed: Creates a recorder,
     * starts recording, stops it, and re-starts it.
     */
    [Test]
    public void VerifyThatReinitializationIsSafe()
    {
        var recorder = new AudioQueueRecorderBuilder().Build();

        Exception exception = null;
        ManualResetEventSlim stopEventWasCalled = new(false);

        recorder.RecordingStopped += (sender, e) => { stopEventWasCalled.Set(); exception = e.Exception; };

        // Start recording: start the recorder, stop it and re-initialize again.
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

    private static void RequireInputDevice()
    {
        if (AudioSystemObject.Instance.DefaultInputDevice is null) { Assert.Ignore("No input device is available."); }
    }

    [Test]
    public void RecorderDeliversAudioAndCanRestart()
    {
        RequireInputDevice();
        using var recorder = new AudioQueueRecorderBuilder().Build();

        WaveFormat format = null;
        Assert.DoesNotThrow(() => format = recorder.CaptureFormat);

        Assert.That(
            format.Encoding == WaveFormatEncoding.Pcm ||
            format.Encoding == WaveFormatEncoding.IeeeFloat || (
                format is WaveFormatExtensible extensible && (
                    extensible.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM ||
                    extensible.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT
                )
            ),
            "The capture format must be PCM, IEEE floating-point, or an extensible format being PCM or IEEE floating-point."
        );

        long bytes = 0;
        int stops = 0;
        recorder.DataAvailable += (data, _, _) => Interlocked.Add(ref bytes, data.Length);
        recorder.RecordingStopped += (_, _) => Interlocked.Increment(ref stops);

        for (int I = 0; I < 2; I++)
        {
            Interlocked.Exchange(ref bytes, 0);
            recorder.StartRecording();
            Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Capturing));
            Thread.Sleep(500);
            recorder.StopRecording();
            Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));
            Assert.That(Interlocked.Read(ref bytes), Is.GreaterThan(0L));
            Assert.That(Interlocked.Read(ref bytes) % recorder.CaptureFormat.BlockAlign, Is.EqualTo(0L));
        }

        // The events are posted to the synchronization context captured when building.
        Assert.That(SpinWait.SpinUntil(() => Volatile.Read(ref stops) == 2, Timeout), Is.True);
    }

    [Test]
    public void RecorderHandlerFailureIsReported()
    {
        RequireInputDevice();
        using var recorder = new AudioQueueRecorderBuilder().Build();
        var source = new TaskCompletionSource<StoppedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        recorder.DataAvailable += (_, _, _) => throw new InvalidOperationException("handler failure");
        recorder.RecordingStopped += (_, e) => source.TrySetResult(e);

        recorder.StartRecording();

        Assert.That(source.Task.Wait(Timeout), Is.True);
        Assert.That(source.Task.Result.Exception, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void StopRecordingFromDataAvailableDoesNotDeadlock()
    {
        RequireInputDevice();
        using var recorder = new AudioQueueRecorderBuilder().Build();
        using var returned = new ManualResetEventSlim(false);
        using var stopped = new ManualResetEventSlim(false);
        Exception thrown = null;
        int callbacks = 0;

        recorder.RecordingStopped += (_, _) => stopped.Set();
        recorder.DataAvailable += (_, _, _) =>
        {
            // Stop from the queue's own thread; this must not wait for the running callback.
            if (Interlocked.Increment(ref callbacks) != 3) { return; }
            try { recorder.StopRecording(); }
            catch (Exception ex) { thrown = ex; }
            returned.Set();
        };

        recorder.StartRecording();

        Assert.That(returned.Wait(Timeout), Is.True, "StopRecording called from DataAvailable did not return.");
        Assert.That(thrown, Is.Null);
        Assert.That(stopped.Wait(Timeout), Is.True, "RecordingStopped was not raised.");
        Assert.That(recorder.CaptureState, Is.EqualTo(CaptureState.Stopped));

        // The recorder stays usable after stopping itself.
        Assert.DoesNotThrow(recorder.StartRecording);
        Assert.DoesNotThrow(recorder.StopRecording);
    }
}
