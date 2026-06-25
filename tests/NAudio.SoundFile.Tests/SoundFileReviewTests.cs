using System;
using System.Collections.Generic;
using System.IO;
using NAudio.SoundFile;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.SoundFile.Tests;

/// <summary>
/// Coverage for the issues raised in the code-review pass: sub-frame
/// carry on writes, float-input path, seek/Position, mono, decode-error
/// surfacing, option effects, tags, Opus guard, raw-format factory,
/// sample accuracy.
/// </summary>
[TestFixture]
public class SoundFileReviewTests : SoundFileTestBase
{
    private string dir;

    [SetUp]
    public void Setup()
    {
        dir = Path.Combine(Path.GetTempPath(), "naudio-sf-rev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    [TearDown]
    public void Teardown()
    {
        if (dir != null && Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
    }

    private static float[] SineFloat(int sampleRate, int channels, double seconds)
    {
        int frames = (int)(sampleRate * seconds);
        var data = new float[frames * channels];
        double phase = 0, step = 2 * Math.PI * 440 / sampleRate;
        for (int f = 0; f < frames; f++)
        {
            float s = (float)(Math.Sin(phase) * 0.25);
            phase += step;
            for (int c = 0; c < channels; c++)
            {
                data[f * channels + c] = s;
            }
        }
        return data;
    }

    private static float[] ReadAllFloat(SoundFileReader r)
    {
        var buf = new float[r.WaveFormat.Channels * 4096];
        var all = new List<float>();
        int n;
        while ((n = ((ISampleProvider)r).Read(buf)) > 0)
        {
            all.AddRange(new ReadOnlySpan<float>(buf, 0, n).ToArray());
        }
        return all.ToArray();
    }

    // RMS of (a-b) relative to RMS of a; ~0 means a faithful copy.
    private static double RelError(float[] a, float[] b)
    {
        int n = Math.Min(a.Length, b.Length);
        double err = 0, sig = 0;
        for (int i = 0; i < n; i++)
        {
            double d = a[i] - b[i];
            err += d * d;
            sig += (double)a[i] * a[i];
        }
        return sig == 0 ? err : Math.Sqrt(err / sig);
    }

    [Test]
    public void FloatInputWavRoundTripIsSampleAccurate()
    {
        var path = Path.Combine(dir, "f.wav");
        var fmt = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var signal = SineFloat(48000, 2, 0.4);

        using (var w = new SoundFileWriter(path, fmt, SoundFileMajorFormat.Wav,
            new SoundFileWriterOptions { Subtype = SoundFileSubtype.Float }))
        {
            w.WriteSamples(signal);
        }

        using var r = new SoundFileReader(path);
        var back = ReadAllFloat(r);
        Assert.Multiple(() =>
        {
            Assert.That(back.Length, Is.EqualTo(signal.Length));
            Assert.That(RelError(signal, back), Is.LessThan(1e-6)); // float WAV is exact
        });
    }

    [Test]
    public void UnalignedChunkedWritesProduceCompleteFile()
    {
        // Drive Write with byte chunk sizes that split frames (frame =
        // 4 bytes for 16-bit stereo) to exercise the carry buffer.
        var path = Path.Combine(dir, "chunked.wav");
        var fmt = new WaveFormat(44100, 16, 2);
        var pcm = new byte[44100 * 4 / 2]; // 0.5 s stereo16, frame = 4 bytes
        new Random(1).NextBytes(pcm);

        using (var w = new SoundFileWriter(path, fmt, SoundFileMajorFormat.Wav))
        {
            // Rotating sub-frame / frame-straddling chunk sizes from
            // start to end — every byte must survive via the carry.
            int[] sizes = { 1, 2, 3, 5, 7, 13, 1001 };
            int pos = 0, k = 0;
            while (pos < pcm.Length)
            {
                int c = Math.Min(sizes[k++ % sizes.Length], pcm.Length - pos);
                w.Write(pcm.AsSpan(pos, c));
                pos += c;
            }
        }

        using var r = new SoundFileReader(path);
        var back = ReadAllFloat(r);
        // Every input frame must survive (16-bit lossless via float read).
        Assert.That(back.Length, Is.EqualTo(pcm.Length / 2));
    }

    [Test]
    public void SeekAndPositionRoundTrip()
    {
        var path = Path.Combine(dir, "seek.wav");
        var fmt = new WaveFormat(48000, 16, 1);
        using (var w = new SoundFileWriter(path, fmt, SoundFileMajorFormat.Wav))
        {
            var tone = new TonePcm16(48000, 0.5);
            var b = new byte[4096];
            int rd;
            while ((rd = tone.Read(b.AsSpan())) > 0)
            {
                w.Write(b.AsSpan(0, rd));
            }
        }

        using var r = new SoundFileReader(path);
        Assert.That(r.CanSeek, Is.True);
        Assert.That(r.Length, Is.GreaterThan(0));

        var first = new float[2000];
        int n1 = ((ISampleProvider)r).Read(first);

        r.Position = 0;
        Assert.That(r.Position, Is.EqualTo(0));
        var again = new float[2000];
        int n2 = ((ISampleProvider)r).Read(again);

        Assert.Multiple(() =>
        {
            Assert.That(n2, Is.EqualTo(n1));
            Assert.That(again, Is.EqualTo(first)); // same samples after rewind
            Assert.That(r.Position, Is.EqualTo(n2 * sizeof(float)));
        });
    }

    [Test]
    public void MonoRoundTrip()
    {
        var path = Path.Combine(dir, "mono.wav");
        var fmt = new WaveFormat(22050, 16, 1);
        using (var w = new SoundFileWriter(path, fmt, SoundFileMajorFormat.Wav))
        {
            var mono = new byte[(22050 * 2 / 3) & ~1]; // ~0.33 s, frame-aligned
            new Random(2).NextBytes(mono);
            w.Write(mono);
        }
        using var r = new SoundFileReader(path);
        Assert.Multiple(() =>
        {
            Assert.That(r.WaveFormat.Channels, Is.EqualTo(1));
            Assert.That(r.WaveFormat.SampleRate, Is.EqualTo(22050));
            Assert.That(ReadAllFloat(r).Length, Is.GreaterThan(1000));
        });
    }

    /// <summary>Seekable stream that throws on Read past a byte offset.</summary>
    private sealed class FailAfterStream : Stream
    {
        private readonly MemoryStream inner;
        private readonly long failAt;
        public FailAfterStream(byte[] data, long failAt)
        {
            inner = new MemoryStream(data);
            this.failAt = failAt;
        }
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() { }
        public override int Read(byte[] b, int o, int c) => Read(b.AsSpan(o, c));
        public override int Read(Span<byte> b)
        {
            if (inner.Position >= failAt)
            {
                throw new IOException("simulated backing-stream failure");
            }
            return inner.Read(b);
        }
        public override long Seek(long o, SeekOrigin r) => inner.Seek(o, r);
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }

    [Test]
    public void BackingStreamFailureSurfacesAsException()
    {
        // A stream that fails mid-decode must raise SoundFileException,
        // NOT be silently mistaken for end-of-file (the vio callback
        // cannot throw across the native boundary).
        var tone = new TonePcm16(48000, 0.5);
        using var ms = new MemoryStream();
        SoundFileWriter.WriteSoundFileToStream(ms, tone, SoundFileMajorFormat.Wav, null);
        var bytes = ms.ToArray();

        using var failing = new FailAfterStream(bytes, failAt: 4000); // past the header
        var ex = Assert.Throws<SoundFileException>(() =>
        {
            using var r = new SoundFileReader(failing);
            var buf = new float[r.WaveFormat.Channels * 4096];
            while (((ISampleProvider)r).Read(buf) > 0) { }
        });
        Assert.That(ex.InnerException, Is.TypeOf<IOException>());
    }

    [Test]
    public void VbrQualityAffectsOutputSize()
    {
        RequireFormat(SoundFileMajorFormat.OggVorbis);
        long Low = Encode(0.1), High = Encode(0.9);
        Assert.That(High, Is.GreaterThan(Low),
            $"q0.9 ({High} B) should exceed q0.1 ({Low} B)");

        long Encode(double q)
        {
            using var ms = new MemoryStream();
            var tone = new TonePcm16(48000, 1.0);
            SoundFileWriter.WriteSoundFileToStream(ms, tone, SoundFileMajorFormat.OggVorbis,
                new SoundFileWriterOptions { VbrQuality = q });
            return ms.Length;
        }
    }

    [Test]
    public void TagsRoundTripWhenSupported()
    {
        RequireFormat(SoundFileMajorFormat.Flac);
        var path = Path.Combine(dir, "tagged.flac");
        var tone = new TonePcm16(48000, 0.2);
        SoundFileWriter.CreateSoundFile(path, tone, SoundFileMajorFormat.Flac,
            new SoundFileWriterOptions
            {
                Tags = new SoundFileTags { Title = "T", Artist = "A", Album = "Al" }
            });

        using var r = new SoundFileReader(path);
        Assert.Multiple(() =>
        {
            Assert.That(r.Tags.Title, Is.EqualTo("T"));
            Assert.That(r.Tags.Artist, Is.EqualTo("A"));
            Assert.That(r.Tags.Album, Is.EqualTo("Al"));
        });
    }

    [Test]
    public void OpusRejectsNonOpusSampleRate()
    {
        // Rate guard fires before codec availability, so this is
        // deterministic regardless of the libsndfile build.
        var fmt = new WaveFormat(44100, 16, 2);
        var ex = Assert.Throws<ArgumentException>(() =>
            _ = new SoundFileWriter(Path.Combine(dir, "x.opus"), fmt, SoundFileMajorFormat.Opus));
        Assert.That(ex.Message, Does.Contain("Opus"));
    }

    [Test]
    public void FromRawFormatFactoryRoundTrips()
    {
        // 0x010000 | 0x0002 == SF_FORMAT_WAV | SF_FORMAT_PCM_16
        var path = Path.Combine(dir, "raw.wav");
        var fmt = new WaveFormat(44100, 16, 2);
        using (var w = SoundFileWriter.FromRawFormat(path, fmt, 0x010000 | 0x0002))
        {
            var tone = new TonePcm16(44100, 0.2);
            var b = new byte[4096];
            int rd;
            while ((rd = tone.Read(b.AsSpan())) > 0)
            {
                w.Write(b.AsSpan(0, rd));
            }
        }
        using var r = new SoundFileReader(path);
        Assert.That(r.WaveFormat.SampleRate, Is.EqualTo(44100));
    }

    [Test]
    public void LibraryVersionIsReported()
    {
        Assert.That(SoundFileCapabilities.LibraryVersion, Does.Contain("libsndfile"));
    }
}
