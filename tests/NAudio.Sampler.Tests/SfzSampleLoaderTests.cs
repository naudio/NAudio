using System;
using System.IO;
using System.Text;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Sampler.Tests;

/// <summary>
/// Tests for sample loading: the generalised decode-into-memory path that
/// backs both WAV and (via NAudio.SoundFile) FLAC/Ogg, the reading of loop
/// points authored in a WAV's <c>smpl</c> chunk, and the graceful failure of
/// the file loader when a file can't be decoded.
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

    private static string WriteWav(int frames = 100, int channels = 1)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
        using var writer = new WaveFileWriter(path, new WaveFormat(44100, 16, channels));
        var block = new short[frames * channels];
        for (int i = 0; i < block.Length; i++) block[i] = 16384;
        writer.WriteSamples(block, 0, block.Length);
        return path;
    }

    // Appends a standard `smpl` chunk (or arbitrary raw bytes) after the data
    // chunk and patches the RIFF size so chunk discovery finds it.
    private static void AppendSmplChunk(string path, params (uint Start, uint End)[] loops)
    {
        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        for (int i = 0; i < 7; i++) w.Write(0u); // manufacturer..smpteOffset
        w.Write((uint)loops.Length);             // numSampleLoops
        w.Write(0u);                             // samplerData
        foreach (var loop in loops)
        {
            w.Write(0u);         // cuePointId
            w.Write(0u);         // type (forward)
            w.Write(loop.Start); // dwStart (frame, inclusive)
            w.Write(loop.End);   // dwEnd (frame, INCLUSIVE per the smpl spec)
            w.Write(0u);         // fraction
            w.Write(0u);         // playCount
        }
        AppendChunk(path, "smpl", body.ToArray());
    }

    private static void AppendChunk(string path, string id, byte[] body)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
        var w = new BinaryWriter(fs);
        fs.Seek(0, SeekOrigin.End);
        w.Write(Encoding.ASCII.GetBytes(id));
        w.Write(body.Length);
        w.Write(body);
        if (body.Length % 2 != 0) w.Write((byte)0); // word-align
        fs.Seek(4, SeekOrigin.Begin);               // patch the RIFF size
        w.Write((uint)(fs.Length - 8));
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
        string path = WriteWav();
        try
        {
            var loader = new FileSfzSampleLoader(Path.GetDirectoryName(path));
            Assert.That(loader.TryLoad(Path.GetFileName(path), out var left, out _, out var rate, out _), Is.True);
            Assert.That(rate, Is.EqualTo(44100));
            Assert.That(left.Length, Is.EqualTo(100));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void WavWithoutSmplChunkHasNoEmbeddedLoop()
    {
        string path = WriteWav();
        try
        {
            Assert.That(WaveSampleLoader.TryLoad(path, out _, out _, out _, out var loop), Is.True);
            Assert.That(loop, Is.Null);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void SmplChunkLoopIsReadWithInclusiveEndConverted()
    {
        // smpl dwStart/dwEnd are sample frames with dwEnd INCLUSIVE; the
        // loader converts to the engine's exclusive end (dwEnd + 1)
        string path = WriteWav();
        try
        {
            AppendSmplChunk(path, (10u, 49u));
            Assert.That(WaveSampleLoader.TryLoad(path, out _, out _, out _, out var loop), Is.True);
            Assert.That(loop, Is.Not.Null);
            Assert.That(loop.Value.Start, Is.EqualTo(10));
            Assert.That(loop.Value.End, Is.EqualTo(50), "dwEnd=49 is inclusive -> exclusive End 50");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void SmplChunkFirstLoopWins()
    {
        string path = WriteWav();
        try
        {
            AppendSmplChunk(path, (20u, 79u), (0u, 99u));
            Assert.That(WaveSampleLoader.TryLoad(path, out _, out _, out _, out var loop), Is.True);
            Assert.That(loop.Value.Start, Is.EqualTo(20));
            Assert.That(loop.Value.End, Is.EqualTo(80));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void FileLoaderSurfacesTheEmbeddedWavLoop()
    {
        string path = WriteWav();
        try
        {
            AppendSmplChunk(path, (5u, 89u));
            var loader = new FileSfzSampleLoader(Path.GetDirectoryName(path));
            Assert.That(loader.TryLoad(Path.GetFileName(path), out _, out _, out _, out var loop), Is.True);
            Assert.That(loop, Is.Not.Null);
            Assert.That(loop.Value.Start, Is.EqualTo(5));
            Assert.That(loop.Value.End, Is.EqualTo(90));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void MalformedSmplChunksAreIgnoredNotFatal()
    {
        // a truncated chunk (too short for a header + one loop), a chunk
        // declaring zero loops, and an inverted loop must all load fine
        // with no embedded loop reported
        foreach (var corrupt in new Action<string>[]
        {
            p => AppendChunk(p, "smpl", new byte[10]),       // truncated
            p => AppendSmplChunk(p),                         // numSampleLoops = 0
            p => AppendSmplChunk(p, (50u, 10u)),             // end before start
        })
        {
            string path = WriteWav();
            try
            {
                corrupt(path);
                Assert.That(WaveSampleLoader.TryLoad(path, out var left, out _, out _, out var loop), Is.True);
                Assert.That(left.Length, Is.EqualTo(100), "audio still decodes");
                Assert.That(loop, Is.Null, "the malformed smpl chunk is ignored");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
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
            Assert.That(loader.TryLoad(Path.GetFileName(path), out _, out _, out _, out _), Is.False);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    public void FileLoaderReturnsFalseForMissingFile()
    {
        var loader = new FileSfzSampleLoader("/nonexistent");
        Assert.That(loader.TryLoad("nope.flac", out _, out _, out _, out _), Is.False);
    }
}
