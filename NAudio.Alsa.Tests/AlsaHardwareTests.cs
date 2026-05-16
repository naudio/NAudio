using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests
{
    /// <summary>
    /// Tests that need a real ALSA endpoint (sound card, WSL2/WSLg
    /// PulseAudio, or the <c>snd-aloop</c> loopback module) and so cannot
    /// run headless or in CI. They are <c>IntegrationTest</c> and skip
    /// unless <c>NAUDIO_ALSA_DEVICE</c> names a device. Env vars:
    /// <list type="bullet">
    /// <item><c>NAUDIO_ALSA_DEVICE</c> — playback device (e.g. <c>pulse</c>,
    /// <c>default</c>, <c>hw:0</c>; <c>null</c> for a no-audio harness smoke).</item>
    /// <item><c>NAUDIO_ALSA_CAPTURE_DEVICE</c> — capture device for the
    /// round-trip (defaults to <c>NAUDIO_ALSA_DEVICE</c>).</item>
    /// <item><c>NAUDIO_ALSA_WAV</c> — path to a .wav for the file-playback
    /// test (skipped if unset).</item>
    /// </list>
    /// Most assertions are "did not throw / completed"; audible quality,
    /// the pause gap, and glitch-free pacing are judged by the listener.
    /// </summary>
    [TestFixture]
    [Category("IntegrationTest")]
    public class AlsaHardwareTests : AlsaTestBase
    {
        private static string Device =>
            Environment.GetEnvironmentVariable("NAUDIO_ALSA_DEVICE");

        private static string CaptureDevice =>
            Environment.GetEnvironmentVariable("NAUDIO_ALSA_CAPTURE_DEVICE") ?? Device;

        private static string WavPath =>
            Environment.GetEnvironmentVariable("NAUDIO_ALSA_WAV");

        [SetUp]
        public void RequireDevice()
        {
            if (string.IsNullOrEmpty(Device))
            {
                Assert.Ignore("Set NAUDIO_ALSA_DEVICE to an ALSA device to run this");
            }
        }

        [Test]
        public void PlaysAToneToARealDevice()
        {
            using var outp = new AlsaOut(Device);
            var (stopped, error) = Hook(outp);
            outp.Init(new ToneProvider(1.0));
            outp.Play();
            Assert.That(stopped.Wait(TimeSpan.FromSeconds(8)), Is.True, "playback did not finish");
            Assert.That(error.Value, Is.Null);
        }

        [Test]
        public void PlaysAWavFileToARealDevice()
        {
            if (string.IsNullOrEmpty(WavPath))
            {
                Assert.Ignore("Set NAUDIO_ALSA_WAV to a .wav path to run this");
            }

            Assert.That(File.Exists(WavPath), Is.True, $"file not found: {WavPath}");

            using var reader = new WaveFileReader(WavPath);
            using var outp = new AlsaOut(Device);
            var (stopped, error) = Hook(outp);
            outp.Init(reader);
            outp.Play();
            var limit = reader.TotalTime + TimeSpan.FromSeconds(10);
            Assert.That(stopped.Wait(limit), Is.True, "playback did not finish");
            Assert.That(error.Value, Is.Null);
        }

        [Test]
        public void PauseResumeOnARealDevice()
        {
            // On a real device: ~2 s tone, paused mid-stream — the listener
            // should hear a gap then the tone resume. On the "null" PCM the
            // tone is consumed instantly; this just exercises the calls.
            using var outp = new AlsaOut(Device);
            var (stopped, error) = Hook(outp);
            outp.Init(new ToneProvider(2.0));
            outp.Play();
            Thread.Sleep(700);
            outp.Pause();
            Thread.Sleep(700);
            outp.Play();
            Assert.That(stopped.Wait(TimeSpan.FromSeconds(10)), Is.True, "playback did not finish");
            Assert.That(error.Value, Is.Null);
        }

        [Test]
        public void CaptureThenPlaybackRoundTrip()
        {
            var format = new WaveFormat(44100, 16, 2);
            var path = Path.Combine(Path.GetTempPath(), $"naudio-alsa-{Guid.NewGuid():N}.wav");
            try
            {
                AlsaIn input;
                try
                {
                    input = new AlsaIn(CaptureDevice) { WaveFormat = format };
                }
                catch (AlsaException ex)
                {
                    Assert.Ignore($"capture device '{CaptureDevice}' not available: {ex.Message}");
                    return;
                }

                long target = format.AverageBytesPerSecond;          // ~1 s
                long captured = 0;
                var enough = new ManualResetEventSlim();
                Exception recError = null;
                var recStopped = new ManualResetEventSlim();

                using (input)
                using (var writer = new WaveFileWriter(path, format))
                {
                    input.DataAvailable += (_, a) =>
                    {
                        writer.Write(a.Buffer, 0, a.BytesRecorded);
                        if (Interlocked.Add(ref captured, a.BytesRecorded) >= target)
                        {
                            enough.Set();
                        }
                    };
                    input.RecordingStopped += (_, e) => { recError = e.Exception; recStopped.Set(); };

                    input.StartRecording();
                    // Bounded by bytes, not wall-clock, so the "null" PCM
                    // (no rate limit) can't balloon the temp file.
                    Assert.That(enough.Wait(TimeSpan.FromSeconds(15)), Is.True, "no audio captured");
                    input.StopRecording();
                    Assert.That(recStopped.Wait(TimeSpan.FromSeconds(5)), Is.True);
                    Assert.That(recError, Is.Null);
                }

                Assert.That(new FileInfo(path).Length, Is.GreaterThan(44), "captured WAV is empty");

                using var reader = new WaveFileReader(path);
                using var outp = new AlsaOut(Device);
                var (stopped, error) = Hook(outp);
                outp.Init(reader);
                outp.Play();
                Assert.That(stopped.Wait(TimeSpan.FromSeconds(15)), Is.True, "playback did not finish");
                Assert.That(error.Value, Is.Null);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static (ManualResetEventSlim stopped, StrongBox<Exception> error) Hook(AlsaOut outp)
        {
            var stopped = new ManualResetEventSlim();
            var error = new StrongBox<Exception>();
            outp.PlaybackStopped += (_, e) => { error.Value = e.Exception; stopped.Set(); };
            return (stopped, error);
        }
    }
}
