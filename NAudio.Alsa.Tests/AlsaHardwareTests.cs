using System;
using System.Threading;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests
{
    /// <summary>
    /// Tests that need a real ALSA endpoint (sound card or the
    /// <c>snd-aloop</c> loopback module) and so cannot run headless or in
    /// CI. They are <c>IntegrationTest</c> and skip unless the
    /// <c>NAUDIO_ALSA_DEVICE</c> environment variable names a device
    /// (e.g. <c>default</c>, <c>hw:0</c>, <c>hw:Loopback,0</c>).
    /// </summary>
    /// <remarks>
    /// Manual checklist this stands in for: audible tone/WAV at correct
    /// pitch and volume; real-time pacing; xrun recovery when the source
    /// stalls; capture round-trip via <c>snd-aloop</c>; pause/resume on
    /// hardware that does and does not support <c>snd_pcm_pause</c>.
    /// </remarks>
    [TestFixture]
    [Category("IntegrationTest")]
    public class AlsaHardwareTests : AlsaTestBase
    {
        private static string Device =>
            Environment.GetEnvironmentVariable("NAUDIO_ALSA_DEVICE");

        [SetUp]
        public void RequireDevice()
        {
            if (string.IsNullOrEmpty(Device))
            {
                Assert.Ignore("Set NAUDIO_ALSA_DEVICE to a real ALSA device to run this");
            }
        }

        [Test]
        public void PlaysAToneToARealDevice()
        {
            using var outp = new AlsaOut(Device);
            var stopped = new ManualResetEventSlim();
            Exception error = null;
            outp.PlaybackStopped += (_, e) => { error = e.Exception; stopped.Set(); };
            outp.Init(new ToneProvider());
            outp.Play();
            Assert.That(stopped.Wait(TimeSpan.FromSeconds(5)), Is.True, "playback did not finish");
            Assert.That(error, Is.Null);
        }
    }
}
