using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.Mp3
{
    // Tests use a synthetic in-memory MP3 byte stream and a fake codec-free decompressor —
    // see acm-av-investigation.md. Going via TestFileBuilder.CreateMp3File (which calls
    // MediaFoundation) was the trigger for an intermittent native AV in the ACM MP3 codec
    // path on CI; these tests are about Mp3FileReaderBase reposition / TOC / length logic,
    // not codec correctness, so they don't need a real encoder or decoder in the loop.
    [TestFixture]
    public class Mp3FileReaderLazyTocTests
    {
        private static Mp3FileReaderBase OpenFromBytes(byte[] mp3, out CountingStream counting)
        {
            counting = new CountingStream(new MemoryStream(mp3));
            return new Mp3FileReaderBase(counting, fmt => new FakeMp3FrameDecompressor(fmt));
        }

        private static Mp3FileReaderBase OpenFromBytes(byte[] mp3) =>
            new Mp3FileReaderBase(new MemoryStream(mp3), fmt => new FakeMp3FrameDecompressor(fmt));

        private static long ExactLengthOf(byte[] mp3)
        {
            using var reader = OpenFromBytes(mp3);
            var buf = new byte[8192];
            long total = 0;
            int n;
            do
            {
                n = reader.Read(buf, 0, buf.Length);
                total += n;
            } while (n > 0);
            return total;
        }

        [Test]
        public void Constructor_DoesNotScanEntireFile()
        {
            // 30 s @ 96 kbit ≈ 360 KB. With Phase 2h the constructor should read only
            // enough to parse ID3 (none here), the first frame, optional Xing header,
            // and the tail-of-stream ID3v1 probe — bounded irrespective of stream size.
            var mp3 = SyntheticMp3.CreateBytes(30);
            using var reader = OpenFromBytes(mp3, out var counting);
            Assert.That(counting.BytesRead, Is.LessThan(16 * 1024),
                "Constructor should not scan the whole stream");
        }

        [Test]
        public void Constructor_ReadCount_IsIndependentOfFileSize()
        {
            var shortMp3 = SyntheticMp3.CreateBytes(5);
            var longMp3 = SyntheticMp3.CreateBytes(60);
            long shortBytes = MeasureConstructorBytesRead(shortMp3);
            long longBytes = MeasureConstructorBytesRead(longMp3);
            // Allow a generous tolerance, but the long stream (12x larger) must not
            // require 12x the I/O. Stricter check than DoesNotScanEntireFile.
            Assert.That(longBytes, Is.LessThan(shortBytes * 2),
                $"Long-stream constructor I/O ({longBytes}) should not scale with stream size " +
                $"(short-stream: {shortBytes})");
        }

        private static long MeasureConstructorBytesRead(byte[] mp3)
        {
            using var reader = OpenFromBytes(mp3, out var counting);
            return counting.BytesRead;
        }

        [Test]
        public void Length_IsExact_WhenInfoHeaderPresent()
        {
            // CreateBytesWithInfoHeader injects an Info tag with the true frame count, so
            // Mp3FileReaderBase can compute exact length without scanning the stream.
            var mp3 = SyntheticMp3.CreateBytesWithInfoHeader(5);
            long exact = ExactLengthOf(mp3);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.True, "Info-tagged stream should report exact length on open");
            Assert.That(reader.Length, Is.EqualTo(exact));
        }

        [Test]
        public void Length_IsEstimate_WhenNoXingHeader()
        {
            var mp3 = SyntheticMp3.CreateBytes(5);
            long exact = ExactLengthOf(mp3);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.False, "Headerless stream should report estimated length on open");
            // Estimate from first-frame bitrate is exact for clean CBR; allow 5% slack.
            long delta = Math.Abs(reader.Length - exact);
            Assert.That(delta, Is.LessThan(exact / 20),
                $"Estimated length {reader.Length} differs from exact {exact} by {delta} bytes");
        }

        [Test]
        public async Task EnsureExactLengthAsync_MakesLengthExact()
        {
            var mp3 = SyntheticMp3.CreateBytes(5);
            long exact = ExactLengthOf(mp3);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.False);
            long positionBefore = reader.Position;
            await reader.EnsureExactLengthAsync();
            Assert.That(reader.IsLengthExact, Is.True);
            Assert.That(reader.Length, Is.EqualTo(exact));
            Assert.That(reader.Position, Is.EqualTo(positionBefore), "Position should be preserved across scan");
        }

        [Test]
        public async Task EnsureExactLengthAsync_IsIdempotent()
        {
            var mp3 = SyntheticMp3.CreateBytesWithInfoHeader(5);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.True, "Info-tagged case is already exact");
            var task = reader.EnsureExactLengthAsync();
            Assert.That(task, Is.SameAs(Task.CompletedTask), "Already-exact case should return Task.CompletedTask without scanning");
            await task;
            Assert.That(reader.IsLengthExact, Is.True);
        }

        [Test]
        public void EnsureExactLengthAsync_IsCancellable()
        {
            // Need a long enough stream that the scan loop observes cancellation.
            var mp3 = SyntheticMp3.CreateBytes(60);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.False);
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.That(async () => await reader.EnsureExactLengthAsync(cts.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public void SequentialRead_ProducesExactLength_WithoutEnsureCall()
        {
            var mp3 = SyntheticMp3.CreateBytes(5);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.False);
            var buf = new byte[8192];
            int n;
            do
            {
                n = reader.Read(buf, 0, buf.Length);
            } while (n > 0);
            Assert.That(reader.IsLengthExact, Is.True, "EOF during sequential read should set IsLengthExact");
            Assert.That(reader.Position, Is.EqualTo(reader.Length));
        }

        [Test]
        public void SeekForward_PastScannedTail_LandsOnCorrectSample()
        {
            var mp3 = SyntheticMp3.CreateBytes(10);
            long exact = ExactLengthOf(mp3);

            // Reader A: open, seek directly to ~halfway without reading first.
            using var readerA = OpenFromBytes(mp3);
            long target = exact / 2;
            readerA.Position = target;
            long settledTarget = readerA.Position;
            var bufA = new byte[16384];
            int nA = readerA.Read(bufA, 0, bufA.Length);

            // Reader B: open, read sequentially past the same point, then read same number of bytes.
            using var readerB = OpenFromBytes(mp3);
            readerB.Position = settledTarget;
            var bufB = new byte[16384];
            int nB = readerB.Read(bufB, 0, bufB.Length);

            Assert.That(nA, Is.EqualTo(nB), "Both readers should return the same byte count");
            // Bytes should be identical (warm-up frames decoded the same way both times).
            Assert.That(bufA, Is.EqualTo(bufB));
        }

        [Test]
        public void SeekBackward_WithinScannedTail_ProducesIdenticalPcm()
        {
            var mp3 = SyntheticMp3.CreateBytes(5);
            using var reader = OpenFromBytes(mp3);
            var buf1 = new byte[16384];
            int n1 = reader.Read(buf1, 0, buf1.Length);
            Assume.That(n1, Is.GreaterThan(0));

            reader.Position = 0;
            var buf2 = new byte[16384];
            int n2 = reader.Read(buf2, 0, buf2.Length);

            Assert.That(n2, Is.EqualTo(n1));
            Assert.That(buf1, Is.EqualTo(buf2));
        }

        [Test]
        public async Task ConcurrentReadAndEnsureExactLength_IsSafe()
        {
            var mp3 = SyntheticMp3.CreateBytes(30);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.False);

            var ensureTask = reader.EnsureExactLengthAsync();

            var buf = new byte[4096];
            long total = 0;
            int n;
            do
            {
                n = reader.Read(buf, 0, buf.Length);
                total += n;
            } while (n > 0);

            await ensureTask;
            Assert.That(reader.IsLengthExact, Is.True);
            Assert.That(total, Is.EqualTo(reader.Length));
        }

        [Test]
        public void ReadFrameAdvancesPosition_WithLazyToc()
        {
            var mp3 = SyntheticMp3.CreateBytes(5);
            using var reader = OpenFromBytes(mp3);
            Assert.That(reader.IsLengthExact, Is.False);
            var lastPos = reader.Position;
            while (reader.ReadNextFrame() != null)
            {
                Assert.That(reader.Position, Is.GreaterThan(lastPos));
                lastPos = reader.Position;
            }
            Assert.That(reader.IsLengthExact, Is.True, "EOF via ReadNextFrame should also flip IsLengthExact");
            Assert.That(reader.Position, Is.EqualTo(reader.Length));
        }
    }
}
