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
}
