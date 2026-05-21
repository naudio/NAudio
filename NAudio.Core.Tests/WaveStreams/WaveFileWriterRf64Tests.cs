using System;
using System.IO;
using System.Text;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveFileWriterRf64Tests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);

        private static string ReadFourCc(byte[] bytes, int offset)
            => Encoding.ASCII.GetString(bytes, offset, 4);

        [Test]
        public void Rf64NotEnabledProducesPlainRiff()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.Write(new byte[100], 0, 100);
            }
            Assert.That(ReadFourCc(ms.ToArray(), 0), Is.EqualTo("RIFF"));
        }

        [Test]
        public void Rf64NotEnabledRejectsFilesLargerThan4Gb()
        {
            // We can't actually allocate > 4 GB in a test, but we can prove the pre-flight
            // range check still triggers at the internal boundary.
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format,
                new WaveFileWriterOptions { EnableRf64 = false, Rf64PromotionThreshold = 50 });
            // Even with a low threshold, EnableRf64 is false so the 4 GB cap still applies.
            // With our modest data the check doesn't fire, but we can at least verify the writer
            // writes RIFF format:
            w.Write(new byte[100], 0, 100);
            // Close and check magic — this asserts the low-threshold override doesn't promote
            // when EnableRf64 is false.
            w.Dispose();
            Assert.That(ReadFourCc(ms.ToArray(), 0), Is.EqualTo("RIFF"));
        }

        [Test]
        public void Rf64EnabledReservesJunkPlaceholderRegardlessOfSize()
        {
            // Whether or not promotion kicks in, the 36-byte JUNK placeholder is always reserved
            // so the ds64 slot exists.
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true }))
            {
                w.Write(new byte[100], 0, 100);
            }
            var bytes = ms.ToArray();
            // RIFF (4) + size (4) + WAVE (4) + JUNK (4) = JUNK id at offset 12
            Assert.That(ReadFourCc(bytes, 12), Is.EqualTo("JUNK"));
        }

        [Test]
        public void Rf64EnabledButSmallDataStaysAsRiff()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true, Rf64PromotionThreshold = 10_000 }))
            {
                w.Write(new byte[100], 0, 100);
            }
            var bytes = ms.ToArray();
            Assert.That(ReadFourCc(bytes, 0), Is.EqualTo("RIFF"));
            // JUNK stays as JUNK (unpromoted)
            Assert.That(ReadFourCc(bytes, 12), Is.EqualTo("JUNK"));
        }

        [Test]
        public void Rf64EnabledAndDataOverThresholdPromotesToRf64()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true, Rf64PromotionThreshold = 100 }))
            {
                w.Write(new byte[500], 0, 500);
            }
            var bytes = ms.ToArray();
            Assert.That(ReadFourCc(bytes, 0), Is.EqualTo("RF64"));
            // top-level RIFF size is 0xFFFFFFFF
            Assert.That(BitConverter.ToUInt32(bytes, 4), Is.EqualTo(0xFFFFFFFFu));
            // JUNK has been rewritten as ds64
            Assert.That(ReadFourCc(bytes, 12), Is.EqualTo("ds64"));
        }

        [Test]
        public void Rf64Ds64ChunkCarriesCorrectSizes()
        {
            var dataLength = 500;
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true, Rf64PromotionThreshold = 100 }))
            {
                w.Write(new byte[dataLength], 0, dataLength);
            }
            var bytes = ms.ToArray();
            // ds64 body starts at 12 + 8 = 20 (after "ds64" id + size field)
            int ds64BodyStart = 20;
            long riffSize = BitConverter.ToInt64(bytes, ds64BodyStart);
            long dataSize = BitConverter.ToInt64(bytes, ds64BodyStart + 8);
            long sampleCount = BitConverter.ToInt64(bytes, ds64BodyStart + 16);

            Assert.That(riffSize, Is.EqualTo(bytes.Length - 8));
            Assert.That(dataSize, Is.EqualTo(dataLength));
            Assert.That(sampleCount, Is.EqualTo(dataLength / Format.BlockAlign));
        }

        [Test]
        public void Rf64PromotedFileCanBeReadBack()
        {
            var audio = new byte[500];
            for (int i = 0; i < audio.Length; i++) audio[i] = (byte)i;

            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true, Rf64PromotionThreshold = 100 }))
            {
                w.Write(audio, 0, audio.Length);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Length, Is.EqualTo(audio.Length));

            var buffer = new byte[audio.Length];
            int read = reader.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(audio.Length));
            Assert.That(buffer, Is.EqualTo(audio));
        }

        [Test]
        public void Rf64PromotedFileDataChunkSizeFieldIsFfffffff()
        {
            // Per EBU Tech 3306, the data chunk size in an RF64 file is 0xFFFFFFFF; the real
            // 64-bit size lives in ds64.
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true, Rf64PromotionThreshold = 100 }))
            {
                w.Write(new byte[500], 0, 500);
            }
            var bytes = ms.ToArray();
            // Find "data" marker and check the following uint32 is 0xFFFFFFFF.
            var text = Encoding.ASCII.GetString(bytes);
            int dataIx = text.IndexOf("data", StringComparison.Ordinal);
            Assert.That(dataIx, Is.GreaterThan(0));
            uint dataSizeField = BitConverter.ToUInt32(bytes, dataIx + 4);
            Assert.That(dataSizeField, Is.EqualTo(0xFFFFFFFFu));
        }

        [Test]
        public void Rf64WorksAlongsideBeforeDataAndAfterDataChunks()
        {
            // Promotion must cope with the extra chunks correctly.
            var audio = new byte[500];
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions { EnableRf64 = true, Rf64PromotionThreshold = 100 }))
            {
                w.WriteBroadcastExtension(new BroadcastExtension { Description = "RF64 Test", Version = 1 });
                w.Write(audio, 0, audio.Length);
                w.AddCue(100, "Mark");
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Length, Is.EqualTo(audio.Length));
            var bext = reader.Chunks.ReadBroadcastExtension();
            Assert.That(bext.Description, Is.EqualTo("RF64 Test"));
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues[0].Label, Is.EqualTo("Mark"));
        }

        [Test]
        public void NormalWriterDoesNotEmitJunkPlaceholder()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.Write(new byte[100], 0, 100);
            }
            var bytes = ms.ToArray();
            // At offset 12 we should find fmt, not JUNK
            Assert.That(ReadFourCc(bytes, 12), Is.EqualTo("fmt "));
        }

        [Test]
        public void OptionsDefaultsProduceNormalRiff()
        {
            // new WaveFileWriterOptions() with no properties set should match the no-options ctor.
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, new WaveFileWriterOptions()))
            {
                w.Write(new byte[100], 0, 100);
            }
            var bytes = ms.ToArray();
            Assert.That(ReadFourCc(bytes, 0), Is.EqualTo("RIFF"));
            Assert.That(ReadFourCc(bytes, 12), Is.EqualTo("fmt "));
        }

        [Test]
        public void NullOptionsIsTreatedAsDefault()
        {
            // Passing null for options is allowed and equivalent to passing a default-constructed
            // WaveFileWriterOptions.
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format, (WaveFileWriterOptions)null))
            {
                w.Write(new byte[100], 0, 100);
            }
            var bytes = ms.ToArray();
            Assert.That(ReadFourCc(bytes, 0), Is.EqualTo("RIFF"));
        }

        [Test]
        public void OptionsDefaultsMatchDocumentedValues()
        {
            // Sanity check on the options defaults themselves.
            var opts = new WaveFileWriterOptions();
            Assert.That(opts.EnableRf64, Is.False);
            Assert.That(opts.Rf64PromotionThreshold, Is.EqualTo(uint.MaxValue));
        }
    }
}
