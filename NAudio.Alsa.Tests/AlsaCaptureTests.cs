using System;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests
{
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
}
