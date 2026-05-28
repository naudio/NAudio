using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams
{
    /// <summary>
    /// Exercises the pure-managed surface of <see cref="IWaveLatency"/> on the WinMM
    /// players that don't touch hardware until <c>Init</c> / <c>StartRecording</c>:
    /// the constructors only initialise managed state, so the buffer-config formulas
    /// and the pre-Init fallback path can be unit-tested without a real audio device.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class WaveLatencyTests
    {
        [TestCase(50, 3, 125.0)]
        [TestCase(100, 2, 150.0)]
        [TestCase(10, 4, 35.0)]
        public void WaveOut_AverageLatency_Follows_BufferFormula(int bufferMs, int numBuffers, double expectedMs)
        {
            var waveOut = new WaveOut { BufferMilliseconds = bufferMs, NumberOfBuffers = numBuffers };
            Assert.That(waveOut.AverageLatency, Is.EqualTo(TimeSpan.FromMilliseconds(expectedMs)));
        }

        [Test]
        public void WaveOut_CurrentLatency_BeforeInit_Returns_AverageLatency()
        {
            var waveOut = new WaveOut { BufferMilliseconds = 100, NumberOfBuffers = 2 };
            Assert.That(waveOut.CurrentLatency, Is.EqualTo(waveOut.AverageLatency));
        }

        [TestCase(50, 3, 125.0)]
        [TestCase(100, 3, 250.0)]
        [TestCase(200, 2, 300.0)]
        public void WaveIn_AverageLatency_Follows_BufferFormula(int bufferMs, int numBuffers, double expectedMs)
        {
            var waveIn = new WaveIn { BufferMilliseconds = bufferMs, NumberOfBuffers = numBuffers };
            Assert.That(waveIn.AverageLatency, Is.EqualTo(TimeSpan.FromMilliseconds(expectedMs)));
        }

        [Test]
        public void WaveIn_CurrentLatency_BeforeRecording_Returns_AverageLatency()
        {
            var waveIn = new WaveIn();
            Assert.That(waveIn.CurrentLatency, Is.EqualTo(waveIn.AverageLatency));
        }

        [Test]
        public void Players_And_Captures_Implement_IWaveLatency()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new WaveOut(), Is.InstanceOf<IWaveLatency>());
                Assert.That(new WaveIn(), Is.InstanceOf<IWaveLatency>());
            });
        }
    }
}
