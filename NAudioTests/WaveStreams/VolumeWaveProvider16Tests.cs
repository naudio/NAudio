using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using Moq;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class VolumeWaveProvider16Tests
    {
        [Test]
        public void DefaultVolumeIs1()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            Assert.AreEqual(1.0f, vwp.Volume);
        }

        [Test]
        public void PassesThroughSourceWaveFormat()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            Assert.AreSame(testProvider.WaveFormat, vwp.WaveFormat);
        }

        [Test]
        public void PassesThroughDataUnchangedAtVolume1()
        {
            var testProvider= new TestWaveProvider(new WaveFormat(44100,16,2));
            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            byte[] buffer = new byte[20];
            int bytesRead = vwp.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, bytesRead);
            Assert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }, buffer);
        }

        [Test]
        public void HalfVolumeWorks()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            testProvider.ConstValue = 100;
            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 0.5f;
            byte[] buffer = new byte[4];
            int bytesRead = vwp.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(new byte[] { 50, 50, 50, 50 }, buffer);
        }

        [Test]
        public void ZeroVolumeWorks()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 2));
            testProvider.ConstValue = 100;
            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 0f;
            byte[] buffer = new byte[4];
            int bytesRead = vwp.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(new byte[] { 0, 0, 0, 0 }, buffer);
        }

        [Test]
        public void DoubleVolumeWorks()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 1));
            testProvider.ConstValue = 2;
            short sampleValue = BitConverter.ToInt16(new byte[] { 2, 2 }, 0);
            sampleValue = (short)(sampleValue * 2);

            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 2f;
            byte[] buffer = new byte[2];
            int bytesRead = vwp.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(BitConverter.GetBytes(sampleValue), buffer);
        }

        [Test]
        public void DoubleVolumeClips()
        {
            var testProvider = new TestWaveProvider(new WaveFormat(44100, 16, 1));
            testProvider.ConstValue = 100;
            short sampleValue = BitConverter.ToInt16(new byte[] { 100, 100 }, 0);
            sampleValue = Int16.MaxValue;

            VolumeWaveProvider16 vwp = new VolumeWaveProvider16(testProvider);
            vwp.Volume = 2f;
            byte[] buffer = new byte[2];
            int bytesRead = vwp.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(BitConverter.GetBytes(sampleValue), buffer);
        }
    }
}
