using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace NAudio.Wave;

/// <summary>
/// Base class for creating an <see cref="ISampleProvider"/> by overriding a
/// <c>Read(float[], int, int)</c> method instead of <see cref="ISampleProvider.Read(Span{float})"/>.
/// Like <see cref="WaveProvider32"/> it also implements <see cref="IWaveProvider"/>, so a derived
/// class can be passed straight to an <see cref="IWavePlayer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This exists for callers that cannot express <see cref="Span{T}"/> in a method signature.
/// The most common case is VB.NET, whose compiler rejects any source that names a ref struct
/// ("Types with embedded references are not supported in this version of your compiler"), making
/// <see cref="ISampleProvider"/> and <see cref="WaveProvider32"/> impossible to implement directly.
/// It is also a convenient landing point when porting NAudio 2 code, whose <c>Read</c> had this
/// exact signature.
/// </para>
/// <para>
/// The bridge rents a buffer from <see cref="ArrayPool{T}.Shared"/> and copies once per read, so
/// C# code should prefer implementing <see cref="ISampleProvider"/> or deriving from
/// <see cref="WaveProvider32"/> directly and avoid the extra copy.
/// </para>
/// <example>
/// In VB.NET:
/// <code>
/// Public Class SineProvider
///     Inherits SampleProviderBase
///
///     Public Sub New()
///         MyBase.New(44100, 1)
///     End Sub
///
///     Public Overrides Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer
///         For i = 0 To count - 1
///             buffer(offset + i) = 0.0F
///         Next
///         Return count
///     End Function
/// End Class
/// </code>
/// </example>
/// </remarks>
public abstract class SampleProviderBase : ISampleProvider, IWaveProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleProviderBase"/> class,
    /// defaulting to 44.1kHz mono.
    /// </summary>
    protected SampleProviderBase()
        : this(44100, 1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SampleProviderBase"/> class with the
    /// specified sample rate and channel count.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="channels">Number of channels.</param>
    protected SampleProviderBase(int sampleRate, int channels)
    {
        SetWaveFormat(sampleRate, channels);
    }

    /// <summary>
    /// Allows you to specify the sample rate and channel count for this provider. Should be set
    /// before the provider is passed to a wave player or mixer.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="channels">Number of channels.</param>
    public void SetWaveFormat(int sampleRate, int channels)
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    /// <summary>
    /// The WaveFormat of this provider, always 32 bit IEEE float.
    /// </summary>
    public WaveFormat WaveFormat { get; private set; }

    /// <summary>
    /// Method to override in derived classes. Fill <paramref name="count"/> samples into
    /// <paramref name="buffer"/>, starting at <paramref name="offset"/>.
    /// </summary>
    /// <param name="buffer">The buffer to fill with 32 bit floating point samples.</param>
    /// <param name="offset">The offset into <paramref name="buffer"/> to start writing at.</param>
    /// <param name="count">The number of samples to write.</param>
    /// <returns>The number of samples written. Return 0 to signal end of stream.</returns>
    public abstract int Read(float[] buffer, int offset, int count);

    /// <summary>
    /// Implements <see cref="ISampleProvider.Read(Span{float})"/> by delegating to the abstract
    /// <see cref="Read(float[], int, int)"/> method via a pooled array.
    /// </summary>
    int ISampleProvider.Read(Span<float> buffer) => ReadIntoSpan(buffer);

    /// <summary>
    /// Implements <see cref="IWaveProvider.Read(Span{byte})"/> by reinterpreting the byte buffer
    /// as 32 bit floats, so a derived class can be played directly by an <see cref="IWavePlayer"/>.
    /// </summary>
    int IWaveProvider.Read(Span<byte> buffer) => ReadIntoSpan(MemoryMarshal.Cast<byte, float>(buffer)) * 4;

    private int ReadIntoSpan(Span<float> buffer)
    {
        if (buffer.IsEmpty) return 0;
        var rented = ArrayPool<float>.Shared.Rent(buffer.Length);
        try
        {
            int read = Read(rented, 0, buffer.Length);
            if ((uint)read > (uint)buffer.Length)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name}.Read returned {read} samples when only {buffer.Length} were requested.");
            }
            rented.AsSpan(0, read).CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(rented);
        }
    }
}
