using System;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class WdlResamplingSampleProviderTests
    {
        [Test]
        public void CanDownsampleAnMp3File()
        {
            string testFile = @"D:\Audio\Music\Coldplay\Mylo Xyloto\03 - Paradise.mp3";
            if (!File.Exists(testFile)) Assert.Ignore(testFile);
            string outFile = @"d:\test22.wav";
            using (var reader = new AudioFileReader(testFile))
            {
                // downsample to 22kHz
                var resampler = new WdlResamplingSampleProvider(reader, 22050);
                var wp = new SampleToWaveProvider(resampler);
                using (var writer = new WaveFileWriter(outFile, wp.WaveFormat))
                {
                    byte[] b = new byte[wp.WaveFormat.AverageBytesPerSecond];
                    while (true)
                    {
                        int read = wp.Read(b, 0, b.Length);
                        if (read > 0)
                            writer.Write(b, 0, read);
                        else
                            break;
                    }
                }
                //WaveFileWriter.CreateWaveFile(outFile, );
            }
        }

        [TestCase(8000, 16000)]
        [TestCase(8000, 22050)]
        [TestCase(8000, 32000)]
        [TestCase(8000, 44100)]
        [TestCase(8000, 48000)]
        [TestCase(8000, 96000)]
        [TestCase(44100, 8000)]
        [TestCase(44100, 16000)]
        [TestCase(44100, 22050)]
        [TestCase(44100, 32000)]
        [TestCase(44100, 48000)]
        [TestCase(44100, 96000)]
        [TestCase(48000, 8000)]
        [TestCase(48000, 16000)]
        [TestCase(48000, 22050)]
        [TestCase(48000, 32000)]
        [TestCase(48000, 44100)]
        [TestCase(48000, 96000)]
        public void CanResampleUpAndDown(int from, int to)
        {
            var channels = 1;
            var offset = CreateSignalGenerator(@from, channels);
            var resampler = new WdlResamplingSampleProvider(offset, to);
            //string fileName = "From {0}"
            //WaveFileWriter.CreateWaveFile16(;
            var buffer = new float[to * channels];
            Console.WriteLine("From {0} to {1}", from, to);
            for (int n = 0; n < 10; n++)
            {
                var read = resampler.Read(buffer, 0, buffer.Length);
                Console.WriteLine("read {0}", read);
            }

        }

        private static OffsetSampleProvider CreateSignalGenerator(int @from, int channels)
        {
            var signalGenerator = new SignalGenerator(@from, channels);
            signalGenerator.Type = SignalGeneratorType.SawTooth;
            signalGenerator.Frequency = 512;
            signalGenerator.Gain = 0.3f;
            var offset = new OffsetSampleProvider(signalGenerator);
            offset.TakeSamples = @from * channels * 5; // 5 seconds
            return offset;
        }
    }
}
