using System;
using System.IO;
using NAudio.Sampler;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for sample loading: the generalised decode-into-memory path that
    /// backs both WAV and (via NAudio.SoundFile) FLAC/Ogg, and the graceful
    /// failure of the file loader when a file can't be decoded.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SfzSampleLoaderTests
    {
        // a finite stereo sample provider standing in for any decoder (libsndfile etc.)
        private sealed class StubProvider : ISampleProvider
        {
            private int remainingFrames;
            public StubProvider(int frames) => remainingFrames = frames;
            public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            public int Read(Span<float> buffer)
            {
                int frames = Math.Min(remainingFrames, buffer.Length / 2);
                for (int f = 0; f < frames; f++) { buffer[f * 2] = 0.5f; buffer[f * 2 + 1] = -0.5f; }
                remainingFrames -= frames;
                return frames * 2;
            }
        }

        [Test]
        public void DecodesAnySampleProviderIntoChannels()
        {
            // this is the exact path FLAC/Ogg take through SoundFileReader
            Assert.That(WaveSampleLoader.TryLoad(new StubProvider(100), out var left, out var right, out var rate), Is.True);
            Assert.That(rate, Is.EqualTo(48000));
            Assert.That(left.Length, Is.EqualTo(100));
            Assert.That(left[0], Is.EqualTo(0.5f));
            Assert.That(right, Is.Not.Null);
            Assert.That(right[0], Is.EqualTo(-0.5f));
        }

        [Test]
        public void FileLoaderReadsWav()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
            try
            {
                using (var writer = new WaveFileWriter(path, new WaveFormat(44100, 16, 1)))
                {
                    var block = new short[100];
                    for (int i = 0; i < block.Length; i++) block[i] = 16384;
                    writer.WriteSamples(block, 0, block.Length);
                }

                var loader = new FileSfzSampleLoader(Path.GetDirectoryName(path));
                Assert.That(loader.TryLoad(Path.GetFileName(path), out var left, out _, out var rate), Is.True);
                Assert.That(rate, Is.EqualTo(44100));
                Assert.That(left.Length, Is.EqualTo(100));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void FileLoaderFailsGracefullyOnUndecodableFile()
        {
            // a .flac that isn't real FLAC: whether libsndfile is present (bad data)
            // or absent (no native lib), the loader must return false, not throw
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".flac");
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                var loader = new FileSfzSampleLoader(Path.GetDirectoryName(path));
                Assert.That(loader.TryLoad(Path.GetFileName(path), out _, out _, out _), Is.False);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void FileLoaderReturnsFalseForMissingFile()
        {
            var loader = new FileSfzSampleLoader("/nonexistent");
            Assert.That(loader.TryLoad("nope.flac", out _, out _, out _), Is.False);
        }
    }
}
