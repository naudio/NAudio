using System;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// Maps between NAudio <see cref="WaveFormat"/> and ALSA <see cref="PCMFormat"/>
    /// for the interleaved little-endian formats this backend supports.
    /// </summary>
    internal static class AlsaFormat
    {
        /// <summary>
        /// The ALSA sample format equivalent to <paramref name="waveFormat"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The encoding / bit depth has no supported ALSA mapping.
        /// </exception>
        public static PCMFormat FromWaveFormat(WaveFormat waveFormat)
        {
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.IeeeFloat when waveFormat.BitsPerSample == 32:
                    return PCMFormat.SND_PCM_FORMAT_FLOAT_LE;
                case WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 8:
                    return PCMFormat.SND_PCM_FORMAT_U8;
                case WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 16:
                    return PCMFormat.SND_PCM_FORMAT_S16_LE;
                case WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 24:
                    return PCMFormat.SND_PCM_FORMAT_S24_3LE;
                case WaveFormatEncoding.Pcm when waveFormat.BitsPerSample == 32:
                    return PCMFormat.SND_PCM_FORMAT_S32_LE;
                default:
                    throw new NotSupportedException(
                        $"No ALSA mapping for {waveFormat.Encoding} {waveFormat.BitsPerSample}-bit");
            }
        }

        /// <summary>Bytes per interleaved frame for <paramref name="waveFormat"/>.</summary>
        public static int FrameBytes(WaveFormat waveFormat)
            => waveFormat.BitsPerSample / 8 * waveFormat.Channels;
    }
}
