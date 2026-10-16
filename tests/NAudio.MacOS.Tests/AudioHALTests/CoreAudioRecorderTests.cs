
using System;
using System.Threading;
using System.Threading.Tasks;

using NAudio.Wave;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
[Category("IntegrationTest")]
public class CoreAudioRecorderTests
{
    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast(10, 5);

    [Test]
    public void RecordFromDefaultDevice_TryWithEvent()
    {
        CoreAudioRecorder recorder = new();

        Assert.Throws<InvalidOperationException>(recorder.StartRecording);

        Assert.DoesNotThrow(recorder.InitializeRecording);

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
        CoreAudioRecorder recorder = new();

        Assert.Throws<InvalidOperationException>(recorder.StartRecording);

        Exception capturedException = null;
        bool dataAvailableDetected = false;
        int recordedBytes = 0, I = 0;

        recorder.DataAvailable += (o, _, _) =>
        {
            dataAvailableDetected = true;
        };

        recorder.RecordingStopped += (sender, args) =>
        {
            capturedException = args.Exception;
        };

        // Attempt to capture 15 buffers.
        await foreach (CoreAudioCaptureBuffer buffer in recorder.CaptureAsync())
        {
            recordedBytes += buffer.Buffer.Length;
            if (I == 15) { break; }
            I++;
        }

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
        var recorder = new CoreAudioRecorder();

        Exception exception = null;
        ManualResetEventSlim stopEventWasCalled = new(false);

        recorder.RecordingStopped += (sender, e) => { stopEventWasCalled.Set(); exception = e.Exception; };

        Assert.DoesNotThrow(recorder.InitializeRecording);

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
