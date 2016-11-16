using System;
using System.Linq;
using NUnit.Framework;
using NAudio.Wave;
using System.IO;
using NAudio.Wave.SampleProviders;

namespace NAudioTests.Acm
{
    [TestFixture]
    public class GsmEncodeTest
    {
        private string tempFolder;
        [TestFixtureSetUp]
        public void Setup()
        {
            tempFolder = Path.Combine(Path.GetTempPath(), "NAudio");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
        }

        private static WaveStream CreatePcmTestStream()
        {
            var outFormat = new WaveFormat(8000, 16, 1);
            const int durationInSeconds = 5;
            var sg = new SignalGenerator(outFormat.SampleRate, outFormat.Channels)
            {
                Frequency = 1000,
                Gain = 0.25,
                Type = SignalGeneratorType.Sin
            };
            var sp = sg.ToWaveProvider16();


            byte[] data = new byte[outFormat.AverageBytesPerSecond * durationInSeconds];
            var bytesRead = sp.Read(data, 0, data.Length);
            Assert.AreEqual(bytesRead, data.Length);
            return new RawSourceWaveStream(new MemoryStream(data), outFormat);
        }

        [Test]
        public void CanEncodeGsm()
        {            
            using (var reader = CreatePcmTestStream())
            {
                using (var gsm = new WaveFormatConversionStream(new Gsm610WaveFormat(), reader))
                {
                    WaveFileWriter.CreateWaveFile(Path.Combine(tempFolder, "gsm.wav"), gsm);
                }
            }
        }

        [Test]
        public void CanDecodeGsm()
        {
            var testFile = Path.Combine(tempFolder, "gsm.wav");
            if (!File.Exists(testFile))
            {
                Assert.Ignore("Missing test file (created by the another test)");
            }
            using (var reader = new WaveFileReader(testFile))
            {
                using (var pcm = WaveFormatConversionStream.CreatePcmStream(reader))
                {
                    WaveFileWriter.CreateWaveFile(Path.Combine(tempFolder, "gsm-decoded.wav"), pcm);
                }
            }
        }
    }
}
