using System;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests
{
    [TestFixture]
    public class AlsaPlaybackTests : AlsaTestBase
    {
        [Test]
        public void InitNegotiatesOutputFormatAndVolume()
        {
            using var outp = new AlsaOut("null");
            outp.Init(new ToneProvider());
            Assert.That(outp.OutputWaveFormat, Is.Not.Null);
            outp.Volume = 0.5f;
            Assert.That(outp.Volume, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void InitTwiceThrows()
        {
            using var outp = new AlsaOut("null");
            outp.Init(new ToneProvider());
            Assert.Throws<InvalidOperationException>(() => outp.Init(new ToneProvider()));
        }

        [Test]
        public void PlaysToEndAndRaisesPlaybackStoppedWithoutError()
        {
            using var outp = new AlsaOut("null");
            var stopped = new ManualResetEventSlim();
            Exception error = null;
            outp.PlaybackStopped += (_, e) => { error = e.Exception; stopped.Set(); };
            outp.Init(new ToneProvider());
            outp.Play();
            Assert.That(stopped.Wait(TimeSpan.FromSeconds(5)), Is.True, "PlaybackStopped not raised");
            Assert.That(error, Is.Null);
        }

        [Test]
        public void ExplicitStopRaisesPlaybackStoppedWithoutError()
        {
            using var outp = new AlsaOut("null");
            var stopped = new ManualResetEventSlim();
            Exception error = null;
            outp.PlaybackStopped += (_, e) => { error = e.Exception; stopped.Set(); };
            outp.Init(new ToneProvider());
            outp.Play();
            Thread.Sleep(50);
            outp.Stop();
            Assert.That(stopped.Wait(TimeSpan.FromSeconds(3)), Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void DisposeIsIdempotentAndReInitWorks()
        {
            var outp = new AlsaOut("null");
            outp.Init(new ToneProvider());
            outp.Play();
            Thread.Sleep(30);
            outp.Dispose();
            Assert.DoesNotThrow(outp.Dispose);

            using var again = new AlsaOut("null");
            again.Init(new ToneProvider());
            again.Play();
            Thread.Sleep(30);
            Assert.DoesNotThrow(again.Stop);
        }
    }
}
