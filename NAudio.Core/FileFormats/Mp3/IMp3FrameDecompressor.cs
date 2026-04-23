using System;
using System.Buffers;

namespace NAudio.Wave
{
    /// <summary>
    /// Interface for MP3 frame by frame decoder
    /// </summary>
    public interface IMp3FrameDecompressor : IDisposable
    {
        /// <summary>
        /// Decompress a single MP3 frame into the supplied byte buffer.
        /// </summary>
        /// <param name="frame">Frame to decompress</param>
        /// <param name="dest">Output buffer — must be large enough to hold one frame's PCM output from <paramref name="destOffset"/> onwards</param>
        /// <param name="destOffset">Offset within output buffer</param>
        /// <returns>Bytes written to output buffer</returns>
        /// <remarks>
        /// Prefer the <see cref="DecompressFrame(Mp3Frame, Span{byte})"/> overload where possible.
        /// This overload is retained for backward compatibility with existing implementations
        /// (including external decoders such as NLayer).
        /// </remarks>
        int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset);

        /// <summary>
        /// Decompress a single MP3 frame into the supplied span.
        /// </summary>
        /// <param name="frame">Frame to decompress</param>
        /// <param name="dest">Output span — must be large enough to hold one frame's PCM output</param>
        /// <returns>Bytes written to <paramref name="dest"/></returns>
        /// <remarks>
        /// The default implementation routes through <see cref="DecompressFrame(Mp3Frame, byte[], int)"/>
        /// via an <see cref="ArrayPool{T}"/>-backed byte buffer, so existing implementations written
        /// against the byte[] overload continue to work unchanged. Implementations that can write
        /// directly into a span should override this method to avoid the intermediate copy.
        /// </remarks>
        int DecompressFrame(Mp3Frame frame, Span<byte> dest)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(dest.Length);
            try
            {
                int written = DecompressFrame(frame, rented, 0);
                int toCopy = Math.Min(written, dest.Length);
                rented.AsSpan(0, toCopy).CopyTo(dest);
                return written;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Tell the decoder that we have repositioned
        /// </summary>
        void Reset();

        /// <summary>
        /// PCM format that we are converting into
        /// </summary>
        WaveFormat OutputFormat { get; }
    }
}
