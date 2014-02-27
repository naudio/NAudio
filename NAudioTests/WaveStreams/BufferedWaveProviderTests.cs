using System;
using System.ComponentModel;
using System.Linq;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class BufferedWaveProviderTests
    {
        [Test]
        public void CanClearBeforeWritingSamples()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.ClearBuffer();
            Assert.AreEqual(0, bwp.BufferedBytes);
        }
        
        [Test]
        public void BufferedBytesAreReturned()
        {
            var bytesToBuffer = 1000;
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            var data = Enumerable.Range(1, bytesToBuffer).Select(n => (byte)(n % 256)).ToArray();
            bwp.AddSamples(data, 0, data.Length);
            Assert.AreEqual(bytesToBuffer, bwp.BufferedBytes);
            var readBuffer = new byte[bytesToBuffer];
            var bytesRead = bwp.Read(readBuffer, 0, bytesToBuffer);
            Assert.AreEqual(bytesRead, bytesToBuffer);
            Assert.AreEqual(readBuffer,data);
            Assert.AreEqual(0, bwp.BufferedBytes);
        }
    }
}
