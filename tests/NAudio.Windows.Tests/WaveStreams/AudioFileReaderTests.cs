using System;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.WaveStreams
{
    /// <summary>
    /// AudioFileReader lives in the NAudio meta-package (not NAudio.Core), which cross-targets a
    /// Windows TFM. These tests therefore live here rather than in NAudio.Core.Tests, so that the
    /// cross-platform test project doesn't need a reference to the meta-package (and the Windows
    /// targeting pack) just to exercise AudioFileReader.
    /// </summary>
    [TestFixture]
    public class AudioFileReaderTests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanBeDisposedMoreThanOnce()
        {
            // Resolve relative to the test assembly (bin/<cfg>/<tfm>/), which sits
            // five levels under the repo root: <tfm>/<cfg>/bin/<project>/tests/<root>.
            var path = Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "samples", "SampleData", "Drums", "closed-hat-trimmed.wav");
            if (!File.Exists(path))
                Assert.Ignore("test file not found");
            var reader = new AudioFileReader(path);
            reader.Dispose();
            Assert.DoesNotThrow(() => reader.Dispose());
        }

        [Test]
        public void AudioFileReader_SpanAndByteArrayRead_Agree()
        {
            var wav = Build16BitMonoPcmWav();
            var tmp = Path.Combine(Path.GetTempPath(), "naudio-span-test-" + Guid.NewGuid() + ".wav");
            File.WriteAllBytes(tmp, wav);
            try
            {
                using var reader = new AudioFileReader(tmp);
                AssertReadParity(reader, chunkSize: 2048); // float stereo output
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        /// <summary>
        /// Build a 1kHz sine-in-WAV byte array we can feed to the reader.
        /// </summary>
        private static byte[] Build16BitMonoPcmWav(int sampleCount = 4096, int sampleRate = 44100)
        {
            var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(sampleRate, 16, 1)))
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = (short)(Math.Sin(2 * Math.PI * 1000.0 * i / sampleRate) * 16000);
                    writer.WriteSample(sample / 32768f);
                }
            }
            return ms.ToArray();
        }

        private static byte[] ReadAllViaByteArray(WaveStream stream, int chunkSize)
        {
            stream.Position = 0;
            long bound = stream.Length;
            var ms = new MemoryStream();
            var buffer = new byte[chunkSize];
            int read;
            while (ms.Length < bound && (read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, bound - ms.Length))) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        private static byte[] ReadAllViaSpan(WaveStream stream, int chunkSize)
        {
            stream.Position = 0;
            long bound = stream.Length;
            var ms = new MemoryStream();
            var buffer = new byte[chunkSize];
            int read;
            while (ms.Length < bound && (read = stream.Read(buffer.AsSpan(0, (int)Math.Min(buffer.Length, bound - ms.Length)))) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        private static void AssertReadParity(WaveStream stream, int chunkSize = 1024)
        {
            var viaArray = ReadAllViaByteArray(stream, chunkSize);
            var viaSpan = ReadAllViaSpan(stream, chunkSize);
            Assert.That(viaSpan, Is.EqualTo(viaArray),
                $"Span read and byte[] read produced different data on {stream.GetType().Name}");
            Assert.That(viaArray.Length, Is.GreaterThan(0),
                $"Test did not actually read anything from {stream.GetType().Name}");
        }
    }
}
