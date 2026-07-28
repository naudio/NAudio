
using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

using NUnit.Framework;

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

    // Async code path seems to block, pending verification of the bug and
    // proposing a fix for it.
    // [Test]
    [CancelAfter(2000)]
    public async Task RecordFromDefaultDevice_TryWithAsyncModel(CancellationToken token)
    {
        CoreAudioRecorder recorder = new();

        Assert.Throws<InvalidOperationException>(recorder.StartRecording);

        int recordedBytes = 0;

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

        int i = 0;

        await foreach (CaptureBuffer buffer in recorder.CaptureAsync(token))
        {
            recordedBytes += buffer.Buffer.Length;
            if (i == 15) { break; }
            i++;
        }

        Assert.DoesNotThrow(() => Assert.IsNotNull(recorder.CaptureFormat));

        Assert.Greater(recordedBytes, 0);

        Assert.DoesNotThrow(recorder.StopRecording);

        recorder.Dispose();
    }
}