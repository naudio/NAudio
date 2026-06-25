using System;
using NAudio.Wave;

namespace NAudio.Extras;

/// <summary>
/// Loopable WaveStream
/// </summary>
public class LoopStream : WaveStream
{
    readonly WaveStream sourceStream;

    /// <summary>
    /// Creates a new Loop stream
    /// </summary>
    public LoopStream(WaveStream source)
    {
        sourceStream = source;
    }

    /// <summary>
    /// The WaveFormat of this stream
    /// </summary>
    public override WaveFormat WaveFormat
    {
        get { return sourceStream.WaveFormat; }
    }

    /// <summary>
    /// Length in bytes of this stream (effectively infinite)
    /// </summary>
    public override long Length
    {
        get { return long.MaxValue / 32; }
    }

    /// <summary>
    /// Position within this stream in bytes
    /// </summary>
    public override long Position
    {
        get
        {
            return sourceStream.Position;
        }
        set
        {
            sourceStream.Position = value;
        }
    }

    /// <summary>
    /// Always has data available
    /// </summary>
    public override bool HasData(int count)
    {
        // infinite loop
        return true;
    }

    /// <summary>
    /// Read data from this stream
    /// </summary>
    public override int Read(Span<byte> buffer)
    {
        int count = buffer.Length;
        int read = 0;
        while (read < count)
        {
            int required = count - read;
            int readThisTime = sourceStream.Read(buffer.Slice(read, required));
            if (readThisTime == 0 && sourceStream.Position == 0)
            {
                // Nothing read from the very start of the source: it's empty
                // (or can't satisfy a request smaller than its block alignment),
                // so stop rather than spin forever. A zero read when we're not
                // at the start just means end-of-pass - the rewind below loops
                // us back to the start to keep filling the buffer.
                break;
            }

            if (readThisTime < required)
            {
                sourceStream.Position = 0;
            }

            if (sourceStream.Position >= sourceStream.Length)
            {
                sourceStream.Position = 0;
            }
            read += readThisTime;
        }
        return read;
    }

    /// <summary>
    /// Read data from this stream
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    /// <summary>
    /// Dispose this WaveStream (disposes the source)
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        sourceStream.Dispose();
        base.Dispose(disposing);
    }
}
