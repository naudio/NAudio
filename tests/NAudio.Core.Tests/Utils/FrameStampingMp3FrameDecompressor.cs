using System;
using NAudio.Wave;

namespace NAudio.Core.Tests.Utils;

/// <summary>
/// Test-only <see cref="IMp3FrameDecompressor"/> that stamps every decoded sample with the
/// source frame's <see cref="Mp3Frame.FileOffset"/> instead of producing audio. That makes
/// the frame a seek actually landed on directly readable out of the PCM, which
/// <see cref="FakeMp3FrameDecompressor"/> (all-silence) cannot distinguish.
/// </summary>
/// <remarks>
/// Requires a stereo 16-bit output format — each 4-byte sample carries the offset as
/// little-endian low half in the left channel, high half in the right. Decode with
/// <see cref="ReadStamp"/>.
/// <para>
/// Set <paramref name="legacyByteArrayOnly"/> to mimic the shape of a pre-Span third-party
/// decoder such as NLayer's <c>Mp3FrameDecompressor</c>, which overrides only the byte[]
/// overload and reaches the reader through the default-interface-method fallback.
/// </para>
/// </remarks>
internal class FrameStampingMp3FrameDecompressor : IMp3FrameDecompressor
{
    private const int SamplesPerFrame = 1152;
    private readonly bool legacyByteArrayOnly;

    public FrameStampingMp3FrameDecompressor(WaveFormat sourceFormat, bool legacyByteArrayOnly = false)
    {
        OutputFormat = new WaveFormat(sourceFormat.SampleRate, 16, sourceFormat.Channels);
        this.legacyByteArrayOnly = legacyByteArrayOnly;
    }

    public WaveFormat OutputFormat { get; }

    /// <summary>Number of <see cref="Reset"/> calls seen so far.</summary>
    public int ResetCount { get; private set; }

    /// <summary>File offsets of every frame handed to this decompressor, in order.</summary>
    public System.Collections.Generic.List<long> FramesDecoded { get; } = new();

    /// <summary>Reads back a stamp written by this decompressor from a PCM buffer.</summary>
    public static long ReadStamp(ReadOnlySpan<byte> pcm, int sampleIndex = 0)
    {
        int o = sampleIndex * 4;
        return (uint)(pcm[o] | (pcm[o + 1] << 8) | (pcm[o + 2] << 16) | (pcm[o + 3] << 24));
    }

    public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        => Stamp(frame, dest.AsSpan(destOffset));

    public int DecompressFrame(Mp3Frame frame, Span<byte> dest)
        => legacyByteArrayOnly
            // Force the reader down the DIM fallback, exactly as an old NLayer build would.
            ? this.DecompressFrameViaLegacy(frame, dest)
            : Stamp(frame, dest);

    private int Stamp(Mp3Frame frame, Span<byte> dest)
    {
        FramesDecoded.Add(frame.FileOffset);
        int bytes = SamplesPerFrame * OutputFormat.Channels * (OutputFormat.BitsPerSample / 8);
        uint stamp = (uint)frame.FileOffset;
        for (int o = 0; o < bytes; o += 4)
        {
            dest[o] = (byte)stamp;
            dest[o + 1] = (byte)(stamp >> 8);
            dest[o + 2] = (byte)(stamp >> 16);
            dest[o + 3] = (byte)(stamp >> 24);
        }
        return bytes;
    }

    public void Reset() => ResetCount++;

    public void Dispose() { }
}

internal static class LegacyDecompressorRouting
{
    /// <summary>
    /// Invokes the byte[] overload the way the interface's default Span implementation does,
    /// so a test double can opt into that path without reimplementing it.
    /// </summary>
    public static int DecompressFrameViaLegacy(this IMp3FrameDecompressor decompressor, Mp3Frame frame, Span<byte> dest)
    {
        var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(dest.Length);
        try
        {
            int written = decompressor.DecompressFrame(frame, rented, 0);
            rented.AsSpan(0, written).CopyTo(dest);
            return written;
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
