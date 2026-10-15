using System;
using System.Buffers;

namespace NAudio.Wave;

/// <summary>
/// Base class for creating an <see cref="IWaveProvider"/> of any format by overriding a
/// <c>Read(byte[], int, int)</c> method instead of <see cref="IWaveProvider.Read(Span{byte})"/>.
/// </summary>
/// <remarks>
/// <para>
/// This exists for callers that cannot express <see cref="Span{T}"/> in a method signature.
/// The most common case is VB.NET, whose compiler rejects any source that names a ref struct
/// ("Types with embedded references are not supported in this version of your compiler"), making
/// <see cref="IWaveProvider"/> impossible to implement directly. It is also a convenient landing
/// point when porting NAudio 2 code, whose <c>Read</c> had this exact signature.
/// </para>
/// <para>
/// If your provider produces 32 bit float samples, prefer <see cref="SampleProviderBase"/>, which
/// also implements <see cref="ISampleProvider"/> so it can be used in a sample pipeline. If you
/// need seeking and a length, derive from <see cref="WaveStream"/> instead — its inherited
/// <see cref="System.IO.Stream.Read(byte[], int, int)"/> overload is likewise overridable without
/// naming a span.
/// </para>
/// <para>
/// The bridge rents a buffer from <see cref="ArrayPool{T}.Shared"/> and copies once per read, so
/// C# code should prefer implementing <see cref="IWaveProvider"/> directly and avoid the extra copy.
/// </para>
/// <example>
/// In VB.NET:
/// <code>
/// Public Class SilenceProvider
///     Inherits WaveProviderBase
///
///     Public Sub New()
///         MyBase.New(New WaveFormat(44100, 16, 2))
///     End Sub
///
///     Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
///         Array.Clear(buffer, offset, count)
///         Return count
///     End Function
/// End Class
/// </code>
/// </example>
/// </remarks>
public abstract class WaveProviderBase : IWaveProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WaveProviderBase"/> class.
    /// </summary>
    /// <param name="waveFormat">The WaveFormat of the audio this provider produces.</param>
    protected WaveProviderBase(WaveFormat waveFormat)
    {
        WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
    }

    /// <summary>
    /// The WaveFormat of this provider.
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Method to override in derived classes. Fill <paramref name="count"/> bytes into
    /// <paramref name="buffer"/>, starting at <paramref name="offset"/>.
    /// </summary>
    /// <param name="buffer">The buffer to fill with audio data.</param>
    /// <param name="offset">The offset into <paramref name="buffer"/> to start writing at.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <returns>The number of bytes written. Return 0 to signal end of stream.</returns>
    public abstract int Read(byte[] buffer, int offset, int count);

    /// <summary>
    /// Implements <see cref="IWaveProvider.Read(Span{byte})"/> by delegating to the abstract
    /// <see cref="Read(byte[], int, int)"/> method via a pooled array.
    /// </summary>
    int IWaveProvider.Read(Span<byte> buffer)
    {
        if (buffer.IsEmpty) return 0;
        var rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            int read = Read(rented, 0, buffer.Length);
            if ((uint)read > (uint)buffer.Length)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.Read returned {read} bytes when only {buffer.Length} were requested.");
            }
            rented.AsSpan(0, read).CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
