using System;
using System.IO;

namespace NAudio.Core.Tests.Utils;

/// <summary>
/// A seekable read-only stream that returns at most a fixed number of bytes per Read, the
/// way network, deflate, crypto and pipe streams can. Used to check that readers keep
/// asking until their buffer is full rather than passing a short read straight on.
/// </summary>
internal sealed class ShortReadStream : Stream
{
    private readonly MemoryStream inner;
    private readonly int maxBytesPerRead;

    public ShortReadStream(byte[] data, int maxBytesPerRead)
    {
        inner = new MemoryStream(data);
        this.maxBytesPerRead = maxBytesPerRead;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => inner.Read(buffer, offset, Math.Min(count, maxBytesPerRead));

    public override int Read(Span<byte> buffer)
        => inner.Read(buffer.Slice(0, Math.Min(buffer.Length, maxBytesPerRead)));

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
