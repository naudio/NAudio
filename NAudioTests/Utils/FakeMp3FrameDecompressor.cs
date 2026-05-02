using System;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    /// <summary>
    /// Test-only <see cref="IMp3FrameDecompressor"/> that does not invoke any real codec
    /// (no ACM, no DMO, no MediaFoundation). Returns a deterministic PCM output:
    /// 1152 samples per frame as 16-bit signed silence. Used by Mp3FileReaderBase tests
    /// that exercise reposition / TOC / length logic without depending on a codec.
    /// </summary>
    internal class FakeMp3FrameDecompressor : IMp3FrameDecompressor
    {
        private const int SamplesPerFrame = 1152;
        private readonly WaveFormat outputFormat;

        public FakeMp3FrameDecompressor(WaveFormat sourceFormat)
        {
            outputFormat = new WaveFormat(sourceFormat.SampleRate, 16, sourceFormat.Channels);
        }

        public WaveFormat OutputFormat => outputFormat;

        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
            => DecompressFrame(frame, dest.AsSpan(destOffset));

        public int DecompressFrame(Mp3Frame frame, Span<byte> dest)
        {
            int bytes = SamplesPerFrame * outputFormat.Channels * (outputFormat.BitsPerSample / 8);
            dest.Slice(0, bytes).Clear();
            return bytes;
        }

        public void Reset() { }

        public void Dispose() { }
    }
}
