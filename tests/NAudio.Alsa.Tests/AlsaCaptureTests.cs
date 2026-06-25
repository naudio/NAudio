using System;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests;

[TestFixture]
public class AlsaCaptureTests : AlsaTestBase
{
    [Test]
    public void DefaultWaveFormatIsSet()
    {
        using var input = new AlsaIn("null");
        Assert.That(input.WaveFormat, Is.Not.Null);
    }

    [Test]
    public void CapturesAndRaisesRecordingStoppedWithoutError()
    {
        using var input = new AlsaIn("null") { WaveFormat = new WaveFormat(44100, 16, 2) };
        int blocks = 0;
        Exception error = null;
        var stopped = new ManualResetEventSlim();
        input.DataAvailable += (_, _) => Interlocked.Increment(ref blocks);
        input.RecordingStopped += (_, e) => { error = e.Exception; stopped.Set(); };

        input.StartRecording();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (Volatile.Read(ref blocks) < 1 && sw.ElapsedMilliseconds < 3000)
        {
            Thread.Sleep(20);
        }

        input.StopRecording();
        Assert.That(stopped.Wait(TimeSpan.FromSeconds(3)), Is.True, "RecordingStopped not raised");
        Assert.That(error, Is.Null);
        Assert.That(Volatile.Read(ref blocks), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void StopRecordingFromDataAvailableHandlerDoesNotDeadlock()
    {
        // StopRecording() called from DataAvailable runs on the capture
        // worker thread; StopWorker must not self-join (regression: #1).
        using var input = new AlsaIn("null") { WaveFormat = new WaveFormat(44100, 16, 2) };
        Exception error = null;
        var stopped = new ManualResetEventSlim();
        int calls = 0;
        input.DataAvailable += (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                input.StopRecording();
            }
        };
        input.RecordingStopped += (_, e) => { error = e.Exception; stopped.Set(); };

        input.StartRecording();
        Assert.That(stopped.Wait(TimeSpan.FromSeconds(5)), Is.True, "deadlocked / RecordingStopped not raised");
        Assert.That(error, Is.Null);
    }

    [Test]
    public void ChangingWaveFormatWhileRecordingThrows()
    {
        using var input = new AlsaIn("null") { WaveFormat = new WaveFormat(44100, 16, 2) };
        input.StartRecording();
        try
        {
            Assert.Throws<InvalidOperationException>(
                () => input.WaveFormat = new WaveFormat(48000, 16, 2));
        }
        finally
        {
            input.StopRecording();
        }
    }
}
