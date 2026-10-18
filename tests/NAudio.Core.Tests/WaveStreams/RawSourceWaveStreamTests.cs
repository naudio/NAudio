using System;
using System.IO;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
[Category("UnitTest")]
public class RawSourceWaveStreamTests
{
    private static readonly WaveFormat Format = new(44100, 16, 1);

    [Test]
    public void DisposeLeavesCallerSuppliedStreamOpen()
    {
        // The caller owns a stream they pass to the constructor, so it must not be disposed.
        var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var raw = new RawSourceWaveStream(ms, Format);
        raw.Dispose();

        Assert.That(ms.CanRead, Is.True, "Caller's stream should still be open after dispose");
    }

    [Test]
    public void DisposeDisposesInternallyCreatedMemoryStream()
    {
        // The byte[] constructor creates a MemoryStream internally; disposing the
        // RawSourceWaveStream should dispose it too (previously it was leaked).
        var raw = new RawSourceWaveStream(new byte[] { 1, 2, 3, 4 }, 0, 4, Format);

        // Reading works before dispose...
        var buffer = new byte[4];
        int readBeforeDispose = raw.Read(buffer, 0, buffer.Length);
        Assert.That(readBeforeDispose, Is.EqualTo(4));

        raw.Dispose();

        // ...and the internal stream is disposed, so reads now throw.
        Assert.Throws<ObjectDisposedException>(() =>
        {
            int readAfterDispose = raw.Read(buffer, 0, buffer.Length);
            Assert.That(readAfterDispose, Is.Zero); // unreachable - the read above throws
        });
    }

    [TestCase(3)]
    [TestCase(7)]
    public void ReadFillsBufferFromStreamThatReturnsShortReads(int maxBytesPerRead)
    {
        var format = new WaveFormat(8000, 16, 2);
        int bufferBytes = format.BlockAlign * 50;
        var audio = new byte[bufferBytes * 2 + format.BlockAlign * 10];
        new Random(7).NextBytes(audio);

        using var raw = new RawSourceWaveStream(new Utils.ShortReadStream(audio, maxBytesPerRead), format);
        var buffer = new byte[bufferBytes];
        var actual = new MemoryStream();
        var reads = new System.Collections.Generic.List<int>();
        int read;
        while ((read = raw.Read(buffer, 0, buffer.Length)) > 0)
        {
            reads.Add(read);
            actual.Write(buffer, 0, read);
        }

        Assert.That(reads, Is.EqualTo(new[] { bufferBytes, bufferBytes, format.BlockAlign * 10 }));
        Assert.That(actual.ToArray(), Is.EqualTo(audio));
    }

    [Test]
    public void ReadDropsTrailingPartialFrameForBlockAlignedRequest()
    {
        var format = new WaveFormat(8000, 16, 2);
        var audio = new byte[format.BlockAlign * 3 + 2];

        using var raw = new RawSourceWaveStream(new Utils.ShortReadStream(audio, 3), format);
        var buffer = new byte[format.BlockAlign * 8];

        Assert.That(raw.Read(buffer, 0, buffer.Length), Is.EqualTo(format.BlockAlign * 3));
        Assert.That(raw.Read(buffer, 0, buffer.Length), Is.Zero);
    }
}
