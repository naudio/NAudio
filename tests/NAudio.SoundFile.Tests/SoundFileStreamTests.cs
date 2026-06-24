using System;
using System.IO;
using NAudio.SoundFile;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.SoundFile.Tests
{
    [TestFixture]
    public class SoundFileStreamTests : SoundFileTestBase
    {
        /// <summary>A write-only, forward-only stream wrapper.</summary>
        private sealed class ForwardOnlyStream : Stream
        {
            private readonly Stream inner;
            public ForwardOnlyStream(Stream inner) => this.inner = inner;
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() => inner.Flush();
            public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
            public override long Seek(long o, SeekOrigin r) => throw new NotSupportedException();
            public override void SetLength(long v) => throw new NotSupportedException();
            public override void Write(byte[] b, int o, int c) => inner.Write(b, o, c);
            public override void Write(ReadOnlySpan<byte> b) => inner.Write(b);
        }

        [Test]
        public void WriteWavToMemoryStreamThenRead()
        {
            var tone = new TonePcm16(44100, 0.3);
            using var ms = new MemoryStream();

            SoundFileWriter.WriteSoundFileToStream(ms, tone, SoundFileMajorFormat.Wav, null);
            Assert.That(ms.Length, Is.GreaterThan(1000));

            ms.Position = 0;
            using var reader = new SoundFileReader(ms);
            Assert.Multiple(() =>
            {
                Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(44100));
                Assert.That(reader.WaveFormat.Channels, Is.EqualTo(2));
            });

            var buf = new float[reader.WaveFormat.Channels * 2048];
            long total = 0;
            int n;
            while ((n = ((ISampleProvider)reader).Read(buf)) > 0)
            {
                total += n;
            }
            // 0.3 s @ 44100 Hz stereo = 13230 frames = 26460 interleaved
            // samples; WAV is lossless so the count is exact.
            Assert.That(total, Is.EqualTo(26460).Within(64));
        }

        [Test]
        public void ReaderDoesNotDisposeCallerStream()
        {
            var tone = new TonePcm16(44100, 0.1);
            var ms = new MemoryStream();
            SoundFileWriter.WriteSoundFileToStream(ms, tone, SoundFileMajorFormat.Wav, null);
            ms.Position = 0;

            using (var reader = new SoundFileReader(ms))
            {
                _ = ((ISampleProvider)reader).Read(new float[256]);
            }

            // If the reader had disposed the stream this would throw.
            Assert.That(ms.CanRead, Is.True);
            ms.Dispose();
        }

        [Test]
        public void NonSeekableStreamRejectsWav()
        {
            var tone = new TonePcm16();
            using var ms = new MemoryStream();
            using var forward = new ForwardOnlyStream(ms);

            Assert.Throws<ArgumentException>(() =>
                _ = new SoundFileWriter(forward, tone.WaveFormat, SoundFileMajorFormat.Wav));
        }

        [Test]
        public void NonSeekableStreamAcceptsFlac()
        {
            RequireFormat(SoundFileMajorFormat.Flac);
            var tone = new TonePcm16(48000, 0.2);
            using var ms = new MemoryStream();

            using (var forward = new ForwardOnlyStream(ms))
            using (var writer = new SoundFileWriter(forward, tone.WaveFormat, SoundFileMajorFormat.Flac))
            {
                var buf = new byte[4096];
                int read;
                while ((read = tone.Read(buf.AsSpan())) > 0)
                {
                    writer.Write(buf.AsSpan(0, read));
                }
            }

            Assert.That(ms.Length, Is.GreaterThan(1000));

            ms.Position = 0;
            using var reader = new SoundFileReader(ms);
            Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(48000));
        }
    }
}
