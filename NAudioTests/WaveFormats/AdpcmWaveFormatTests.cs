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
            Assert.AreEqual(18, Marshal.SizeOf(waveFormat), "WaveFormat Size");
            AdpcmWaveFormat adpcmWaveFormat = new AdpcmWaveFormat(8000,1);
            Assert.AreEqual(18 + 32, Marshal.SizeOf(adpcmWaveFormat), "WaveFormat Size");            
        }

        [Test]
        public void StructureContentsAreCorrect()
        {
            AdpcmWaveFormat adpcmWaveFormat = new AdpcmWaveFormat(8000,1);
            Assert.AreEqual(WaveFormatEncoding.Adpcm, adpcmWaveFormat.Encoding, "Encoding");
            Assert.AreEqual(8000, adpcmWaveFormat.SampleRate, "Sample Rate");
            Assert.AreEqual(1, adpcmWaveFormat.Channels, "Channels");
            Assert.AreEqual(4, adpcmWaveFormat.BitsPerSample, "Bits Per Sample");
            Assert.AreEqual(4096, adpcmWaveFormat.AverageBytesPerSecond, "Average Bytes Per Second");
            Assert.AreEqual(32, adpcmWaveFormat.ExtraSize, "Extra Size");
            Assert.AreEqual(256, adpcmWaveFormat.BlockAlign, "Block Align");
            Assert.AreEqual(500, adpcmWaveFormat.SamplesPerBlock, "Channels");
            Assert.AreEqual(7, adpcmWaveFormat.NumCoefficients, "NumCoefficients");
            Assert.AreEqual(256, adpcmWaveFormat.Coefficients[0], "Coefficient 0");
            Assert.AreEqual(0, adpcmWaveFormat.Coefficients[1], "Coefficient 1");
            Assert.AreEqual(512, adpcmWaveFormat.Coefficients[2], "Coefficient 2");
            Assert.AreEqual(-256, adpcmWaveFormat.Coefficients[3], "Coefficient 3");
            Assert.AreEqual(0, adpcmWaveFormat.Coefficients[4], "Coefficient 4");
            Assert.AreEqual(0, adpcmWaveFormat.Coefficients[5], "Coefficient 5");
            Assert.AreEqual(192, adpcmWaveFormat.Coefficients[6], "Coefficient 6");
            Assert.AreEqual(64, adpcmWaveFormat.Coefficients[7], "Coefficient 7");
            Assert.AreEqual(240, adpcmWaveFormat.Coefficients[8], "Coefficient 8");
            Assert.AreEqual(0, adpcmWaveFormat.Coefficients[9], "Coefficient 9");
            Assert.AreEqual(460, adpcmWaveFormat.Coefficients[10], "Coefficient 10");
            Assert.AreEqual(-208, adpcmWaveFormat.Coefficients[11], "Coefficient 11");
            Assert.AreEqual(392, adpcmWaveFormat.Coefficients[12], "Coefficient 12");
            Assert.AreEqual(-232, adpcmWaveFormat.Coefficients[13], "Coefficient 13");
        }
    }
}
