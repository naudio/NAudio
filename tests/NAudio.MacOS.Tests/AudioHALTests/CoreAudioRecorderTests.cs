
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
    [Test]
    public void RecordFromDefaultDevice_TryWithEvent()
    {
        CoreAudioRecorder recorder = new();

        Assert.Throws<InvalidOperationException>(recorder.StartRecording);

        Assert.DoesNotThrow(() => recorder.InitializeRecording());

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

        recorder.Dispose();
    }

    [Test]
    public async Task RecordFromDefaultDevice_TryWithAsyncModel()
    {
        CoreAudioRecorder recorder = new();

        Assert.Throws<InvalidOperationException>(recorder.StartRecording);

        int recordedBytes = 0, I = 0;

        recorder.DataAvailable += (o, _, _) =>
        {
            Assert.Fail("Should not return data while using the async model");
        };

        recorder.RecordingStopped += (sender, args) =>
        {
            if (args.Exception is not null)
            {
                throw args.Exception;
            }
        };

        // Attempt to capture 15 buffers.
        await foreach (CaptureBuffer buffer in recorder.CaptureAsync())
        {
            recordedBytes += buffer.Buffer.Length;
            if (I == 15) { break; }
            I++;
        }

        Assert.DoesNotThrow(() => Assert.IsNotNull(recorder.CaptureFormat));

        Assert.Greater(recordedBytes, 0);

        Assert.DoesNotThrow(recorder.StopRecording);

        await recorder.DisposeAsync();
    }
}