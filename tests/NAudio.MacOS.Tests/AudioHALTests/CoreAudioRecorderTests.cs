
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

        Assert.Greater(recordedBytes, 0);

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
            Assert.IsNotNull(cf, "Capture format cannot be null!");
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

        Assert.Greater(recordedBytes, 0);

        Assert.DoesNotThrow(recorder.StopRecording);

        await recorder.DisposeAsync();
    }
}