
// Created on 27/12/2002 at 20:20

using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// The base class for seekable (repositionable) audio sources. <br />
/// Derives from <see cref="Stream"/>, and 
/// implements <see cref="IWaveProvider"/> so
/// that it can be directly consumed by the entire NAudio audio processing pipeline. <br />
/// Typically, this is used by file format readers, such as the <see cref="WaveFileReader"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>
/// Building a new audio capable class: 
/// <see cref="WaveStream"/> or <see cref="IWaveProvider"/> —
/// what is more suitable to use and when?
/// </b>
/// </para>
/// <para>
/// The <see cref="WaveStream"/> class is suitable to be used as a base class when
/// you are providing a new source into the NAudio audio pipeline that has immutable
/// audio - that is, audio whose samples are not changed and their
/// exact count is known once the object is initialized.
/// File format readers is a primary example. <br />
/// Instead, <see cref="IWaveProvider"/> is the interface that its implementation wraps
/// another <see cref="IWaveProvider"/> and manipulates the audio samples in some way. <br />
/// Using <see cref="IWaveProvider"/> is also suitable when building any signal generator.
/// </para>
/// <para>
/// <b>Implementing a WaveStream — which Read method should I override?</b>
/// </para>
/// <para>
/// <see cref="Stream.Read(byte[], int, int)"/> is abstract on <see cref="Stream"/> and must be overridden.
/// <see cref="Stream.Read(Span{byte})"/> is virtual — its default implementation rents a buffer from
/// <see cref="System.Buffers.ArrayPool{T}.Shared"/>, calls the byte[] overload, and copies into the span.
/// This is functionally correct but incurs one pool rent and one extra copy per read.
/// </para>
/// <para>
/// For best performance, implement your read logic in the <c>Read(Span&lt;byte&gt;)</c> overload and
/// make the byte[] overload forward to it:
/// <code>
/// public override int Read(Span&lt;byte&gt; buffer) { /* real read logic */ }
/// public override int Read(byte[] array, int offset, int count)
///     =&gt; Read(array.AsSpan(offset, count));
/// </code>
/// All of NAudio's built-in readers follow this pattern. Legacy subclasses that only override the byte[]
/// overload continue to work correctly, but pay the pooled-bridge cost on span-based reads.
/// </para>
/// </remarks>
public abstract class WaveStream : Stream, IWaveProvider
{
    // base class includes long Position get; set
    // base class includes long Length get
    // base class includes Read
    // base class includes Dispose

    /// <summary>
    /// We can read from this stream
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// We can seek within this stream
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    /// We can't write to this stream
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Retrieves the <see cref="Wave.WaveFormat"/> for this wave stream.
    /// </summary>
    /// <returns>The wave format.</returns>
    /// <remarks>
    /// The audio format describes the wave data that are provided by successive <see cref="Stream.Read(Span{byte})"/> calls. <br />
    /// Without this, it is not possible to know how the audio samples are laid out. <br /> <br />
    /// For most NAudio implementations, this typically returns a PCM or an IEEE floating-point format.
    /// </remarks>
    public abstract WaveFormat WaveFormat { get; }

    /// <summary>
    /// The block alignment for this wave stream. <br />
    /// Do not modify the Position property to anything that is not a whole multiple of this value
    /// </summary>
    public virtual int BlockAlign => WaveFormat.BlockAlign;

    /// <summary>
    /// Flush does not need to do anything
    /// See <see cref="Stream.Flush"/>
    /// </summary>
    public override void Flush() { }

    /// <summary>Sets the position within the current wave stream.</summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
    /// <returns>The new position within the current wave stream.</returns>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The wave stream is disposed.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin)
            Position = offset;
        else if (origin == SeekOrigin.Current)
            Position += offset;
        else
            Position = Length + offset;
        return Position;
    }

    /// <summary>
    /// Sets the length of the wave stream. <br />
    /// This method call is not supported and will always throw <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="length" />
    /// <exception cref="NotSupportedException">This method call is not supported.</exception>
    public override void SetLength(long length)
        => throw new NotSupportedException("Setting a new length value on a wave stream object is not possible.");

    /// <summary>
    /// Writes to the wave stream. <br />
    /// This method call is not supported and will always throw <see cref="NotSupportedException"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This method call is not supported.</exception>
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("A wave stream is not writeable.");

    /// <summary>
    /// Moves forwards or backwards the specified number of seconds in the wave stream. <br />
    /// If the computed new position exceeds the length of the wave stream, or is less than 0,
    /// the value is clamped in the range [0..length], before setting it as a new position value.
    /// </summary>
    /// <param name="seconds">Number of seconds to move, can be negative</param>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The wave stream is disposed.</exception>
    public void Skip(int seconds)
    {
        long length = Length;
        long newPosition = Position + WaveFormat.AverageBytesPerSecond * seconds;
        if (newPosition > length)
            newPosition = length;
        else if (newPosition < 0L)
            newPosition = 0L;

        Position = newPosition;
    }

    /// <summary>
    /// Gets/sets the current position in the stream in canonical time format. <br />
    /// When this property is set, the resulting byte position is rounded down
    /// to a multiple of <see cref="BlockAlign"/> so that the stream always stay
    /// on a valid block boundary.
    /// </summary>
    /// <returns>The current position in the wave stream, expressed as a <see cref="TimeSpan"/> structure.</returns>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The wave stream is disposed.</exception>
    public virtual TimeSpan CurrentTime
    {
        get => TimeSpan.FromSeconds((double)Position / WaveFormat.AverageBytesPerSecond);
        set
        {
            long length;
            try
            {
                // Attempt to get the length.
                // It is not necessary that all the streams will implement it,
                // so we get the value only if we are able to do so, and otherwise
                // set to 0 to indicate we could not get the length.
                // This is special-cased in the if statement below.
                length = Length;
            }
            catch
            {
                length = 0L;
            }
            long bytePosition = (long)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            if (bytePosition < 0L)
                Position = 0L;
            else if (length > 0L && bytePosition > length) // Do the bound check only if we are able to do so.
                Position = length;
            else
                Position = bytePosition - (bytePosition % BlockAlign);
        }
    }

    /// <summary>
    /// Total length in real-time of the stream (may be an estimate for compressed files)
    /// </summary>
    /// <exception cref="ObjectDisposedException">The wave stream is disposed.</exception>
    public virtual TimeSpan TotalTime
        => TimeSpan.FromSeconds((double)Length / WaveFormat.AverageBytesPerSecond);

    /// <summary>
    /// Queries whether the wave stream has non-zero sample 
    /// data at the current position for the specified <paramref name="count"/>.
    /// </summary>
    /// <param name="count">Number of bytes to read.</param>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The wave stream is disposed.</exception>
    public virtual bool HasData(int count) => Position < Length;
}
