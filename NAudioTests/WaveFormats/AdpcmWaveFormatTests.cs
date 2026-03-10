using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudioTests.WaveFormats
{
    [TestFixture]
    [Category("UnitTest")]
    public class AdpcmWaveFormatTests
    {
        [Test]
        public void StructureSizeIsCorrect()
        {
            WaveFormat waveFormat = new WaveFormat(8000, 16, 1);
            Assert.That(Marshal.SizeOf(waveFormat), Is.EqualTo(18), "WaveFormat Size");
            AdpcmWaveFormat adpcmWaveFormat = new AdpcmWaveFormat(8000,1);
            Assert.That(Marshal.SizeOf(adpcmWaveFormat), Is.EqualTo(18 + 32), "WaveFormat Size");
        }

        [Test]
        public void StructureContentsAreCorrect()
        {
            AdpcmWaveFormat adpcmWaveFormat = new AdpcmWaveFormat(8000,1);
            Assert.That(adpcmWaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Adpcm), "Encoding");
            Assert.That(adpcmWaveFormat.SampleRate, Is.EqualTo(8000), "Sample Rate");
            Assert.That(adpcmWaveFormat.Channels, Is.EqualTo(1), "Channels");
            Assert.That(adpcmWaveFormat.BitsPerSample, Is.EqualTo(4), "Bits Per Sample");
            Assert.That(adpcmWaveFormat.AverageBytesPerSecond, Is.EqualTo(4096), "Average Bytes Per Second");
            Assert.That(adpcmWaveFormat.ExtraSize, Is.EqualTo(32), "Extra Size");
            Assert.That(adpcmWaveFormat.BlockAlign, Is.EqualTo(256), "Block Align");
            Assert.That(adpcmWaveFormat.SamplesPerBlock, Is.EqualTo(500), "Channels");
            Assert.That(adpcmWaveFormat.NumCoefficients, Is.EqualTo(7), "NumCoefficients");
            Assert.That(adpcmWaveFormat.Coefficients[0], Is.EqualTo(256), "Coefficient 0");
            Assert.That(adpcmWaveFormat.Coefficients[1], Is.EqualTo(0), "Coefficient 1");
            Assert.That(adpcmWaveFormat.Coefficients[2], Is.EqualTo(512), "Coefficient 2");
            Assert.That(adpcmWaveFormat.Coefficients[3], Is.EqualTo(-256), "Coefficient 3");
            Assert.That(adpcmWaveFormat.Coefficients[4], Is.EqualTo(0), "Coefficient 4");
            Assert.That(adpcmWaveFormat.Coefficients[5], Is.EqualTo(0), "Coefficient 5");
            Assert.That(adpcmWaveFormat.Coefficients[6], Is.EqualTo(192), "Coefficient 6");
            Assert.That(adpcmWaveFormat.Coefficients[7], Is.EqualTo(64), "Coefficient 7");
            Assert.That(adpcmWaveFormat.Coefficients[8], Is.EqualTo(240), "Coefficient 8");
            Assert.That(adpcmWaveFormat.Coefficients[9], Is.EqualTo(0), "Coefficient 9");
            Assert.That(adpcmWaveFormat.Coefficients[10], Is.EqualTo(460), "Coefficient 10");
            Assert.That(adpcmWaveFormat.Coefficients[11], Is.EqualTo(-208), "Coefficient 11");
            Assert.That(adpcmWaveFormat.Coefficients[12], Is.EqualTo(392), "Coefficient 12");
            Assert.That(adpcmWaveFormat.Coefficients[13], Is.EqualTo(-232), "Coefficient 13");
        }
    }
}
