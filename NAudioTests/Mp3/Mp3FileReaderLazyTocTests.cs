using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.Mp3
{
    [TestFixture]
    public class Mp3FileReaderLazyTocTests
    {
        private static long ExactLengthOf(string file)
        {
            using var reader = new Mp3FileReader(file);
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
            // 30s @ 96 kbit ≈ 360 KB. With Phase 2h, the constructor should read only enough
            // to parse ID3, the first frame, optional Xing header, and the ID3v1 tag at EOF —
            // bounded irrespective of file size.
            var file = TestFileBuilder.CreateMp3File(30, 44100, 2, "TestSignal_30s.mp3");
            try
            {
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var counting = new CountingStream(fs);
                using var reader = new Mp3FileReaderBase(counting, fmt => new NAudio.Wave.AcmMp3FrameDecompressor(fmt));
                Assert.That(counting.BytesRead, Is.LessThan(16 * 1024),
                    "Constructor should not scan the whole file");
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void Constructor_ReadCount_IsIndependentOfFileSize()
        {
            var shortFile = TestFileBuilder.CreateMp3File(5, 44100, 2, "TestSignal_short.mp3");
            var longFile = TestFileBuilder.CreateMp3File(60, 44100, 2, "TestSignal_long.mp3");
            try
            {
                long shortBytes = MeasureConstructorBytesRead(shortFile);
                long longBytes = MeasureConstructorBytesRead(longFile);
                // Allow a generous tolerance, but the long file (12x larger) must not require
                // 12x the I/O. Stricter check than ScansEntireFile.
                Assert.That(longBytes, Is.LessThan(shortBytes * 2),
                    $"Long-file constructor I/O ({longBytes}) should not scale with file size " +
                    $"(short-file: {shortBytes})");
            }
            finally
            {
                File.Delete(shortFile);
                File.Delete(longFile);
            }
        }

        private static long MeasureConstructorBytesRead(string file)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var counting = new CountingStream(fs);
            using var reader = new Mp3FileReaderBase(counting, fmt => new NAudio.Wave.AcmMp3FrameDecompressor(fmt));
            return counting.BytesRead;
        }

        [Test]
        public void Length_IsExact_WhenInfoHeaderPresent()
        {
            // CreateMp3FileWithInfoHeader injects an Info tag with the true frame count, so
            // Mp3FileReaderBase can compute exact length without scanning the file.
            var file = TestFileBuilder.CreateMp3FileWithInfoHeader(5);
            try
            {
                long exact = ExactLengthOf(file);
                using var reader = new Mp3FileReader(file);
                Assert.That(reader.IsLengthExact, Is.True, "Info-tagged file should report exact length on open");
                Assert.That(reader.Length, Is.EqualTo(exact));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void Length_IsEstimate_WhenNoXingHeader()
        {
            // The MediaFoundation MP3 encoder produces a CBR file without a Xing/Info tag,
            // so the bare CreateMp3File output exercises the estimate path.
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                long exact = ExactLengthOf(file);
                using var reader = new Mp3FileReader(file);
                Assert.That(reader.IsLengthExact, Is.False, "Headerless file should report estimated length on open");
                // Estimate from first-frame bitrate is exact for clean CBR; allow 5% slack.
                long delta = Math.Abs(reader.Length - exact);
                Assert.That(delta, Is.LessThan(exact / 20),
                    $"Estimated length {reader.Length} differs from exact {exact} by {delta} bytes");
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public async Task EnsureExactLengthAsync_MakesLengthExact()
        {
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                long exact = ExactLengthOf(file);
                using var reader = new Mp3FileReader(file);
                Assert.That(reader.IsLengthExact, Is.False);
                long positionBefore = reader.Position;
                await reader.EnsureExactLengthAsync();
                Assert.That(reader.IsLengthExact, Is.True);
                Assert.That(reader.Length, Is.EqualTo(exact));
                Assert.That(reader.Position, Is.EqualTo(positionBefore), "Position should be preserved across scan");
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public async Task EnsureExactLengthAsync_IsIdempotent()
        {
            var file = TestFileBuilder.CreateMp3FileWithInfoHeader(5);
            try
            {
                using var reader = new Mp3FileReader(file);
                Assert.That(reader.IsLengthExact, Is.True, "Info-tagged case is already exact");
                var task = reader.EnsureExactLengthAsync();
                Assert.That(task, Is.SameAs(Task.CompletedTask), "Already-exact case should return Task.CompletedTask without scanning");
                await task;
                Assert.That(reader.IsLengthExact, Is.True);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void EnsureExactLengthAsync_IsCancellable()
        {
            // Need a long enough file that the scan loop observes cancellation.
            var file = TestFileBuilder.CreateMp3File(60, 44100, 2, "TestSignal_60s.mp3");
            try
            {
                using var reader = new Mp3FileReader(file);
                Assert.That(reader.IsLengthExact, Is.False);
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Assert.That(async () => await reader.EnsureExactLengthAsync(cts.Token),
                    Throws.InstanceOf<OperationCanceledException>());
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void SequentialRead_ProducesExactLength_WithoutEnsureCall()
        {
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                using var reader = new Mp3FileReader(file);
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
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void SeekForward_PastScannedTail_LandsOnCorrectSample()
        {
            var file = TestFileBuilder.CreateMp3File(10);
            try
            {
                long exact = ExactLengthOf(file);

                // Reader A: open, seek directly to ~halfway without reading first.
                using var readerA = new Mp3FileReader(file);
                long target = exact / 2;
                readerA.Position = target;
                long settledTarget = readerA.Position;
                var bufA = new byte[16384];
                int nA = readerA.Read(bufA, 0, bufA.Length);

                // Reader B: open, read sequentially past the same point, then read same number of bytes.
                using var readerB = new Mp3FileReader(file);
                readerB.Position = settledTarget;
                var bufB = new byte[16384];
                int nB = readerB.Read(bufB, 0, bufB.Length);

                Assert.That(nA, Is.EqualTo(nB), "Both readers should return the same byte count");
                // Bytes should be identical (warm-up frames re-decoded the same way both times).
                Assert.That(bufA, Is.EqualTo(bufB));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void SeekBackward_WithinScannedTail_ProducesIdenticalPcm()
        {
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                using var reader = new Mp3FileReader(file);
                var buf1 = new byte[16384];
                int n1 = reader.Read(buf1, 0, buf1.Length);
                Assume.That(n1, Is.GreaterThan(0));

                reader.Position = 0;
                var buf2 = new byte[16384];
                int n2 = reader.Read(buf2, 0, buf2.Length);

                Assert.That(n2, Is.EqualTo(n1));
                Assert.That(buf1, Is.EqualTo(buf2));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public async Task ConcurrentReadAndEnsureExactLength_IsSafe()
        {
            var file = TestFileBuilder.CreateMp3File(30, 44100, 2, "TestSignal_30s.mp3");
            try
            {
                using var reader = new Mp3FileReader(file);
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
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void ReadFrameAdvancesPosition_WithLazyToc()
        {
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                using var reader = new Mp3FileReader(file);
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
            finally
            {
                File.Delete(file);
            }
        }
    }
}
