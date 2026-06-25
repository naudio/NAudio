using System;
using NAudio.Dmo;

namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Utility class for converting to SampleProvider
/// </summary>
internal static class SampleProviderConverters
{
    /// <summary>
    /// Helper function to go from IWaveProvider to a SampleProvider
    /// Must already be PCM or IEEE float (including WAVE_FORMAT_EXTENSIBLE
    /// whose SubFormat is PCM or IEEE float)
    /// </summary>
    /// <param name="waveProvider">The WaveProvider to convert</param>
    /// <returns>A sample provider</returns>
    public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider)
    {
        // Multichannel / high bit depth PCM and float audio is usually described as
        // WAVE_FORMAT_EXTENSIBLE. Re-present such a source under the equivalent standard
        // PCM or IEEE float format (the sample bytes are identical) so it both dispatches
        // here and is accepted by the converters' encoding checks. See issue #639.
        var source = AsStandardProvider(waveProvider);

        ISampleProvider sampleProvider;
        if (source.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            // go to float
            if (source.WaveFormat.BitsPerSample == 8)
            {
                sampleProvider = new Pcm8BitToSampleProvider(source);
            }
            else if (source.WaveFormat.BitsPerSample == 16)
            {
                sampleProvider = new Pcm16BitToSampleProvider(source);
            }
            else if (source.WaveFormat.BitsPerSample == 24)
            {
                sampleProvider = new Pcm24BitToSampleProvider(source);
            }
            else if (source.WaveFormat.BitsPerSample == 32)
            {
                sampleProvider = new Pcm32BitToSampleProvider(source);
            }
            else
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
        }
        else if (source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            if (source.WaveFormat.BitsPerSample == 64)
                sampleProvider = new WaveToSampleProvider64(source);
            else
                sampleProvider = new WaveToSampleProvider(source);
        }
        else
        {
            throw new ArgumentException("Unsupported source encoding");
        }
        return sampleProvider;
    }

    /// <summary>
    /// If the provider's format is a PCM or IEEE float WAVE_FORMAT_EXTENSIBLE, wraps it so
    /// it reports the equivalent standard format; otherwise returns it unchanged. The wave
    /// data is left untouched - only the reported WaveFormat differs.
    /// </summary>
    private static IWaveProvider AsStandardProvider(IWaveProvider waveProvider)
    {
        if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.Extensible)
        {
            return waveProvider;
        }
        var standardFormat = ToStandardWaveFormat(waveProvider.WaveFormat);
        // leave unrecognised extensible subformats untouched so the dispatcher still throws
        return standardFormat == null ? waveProvider : new WaveFormatAdapter(waveProvider, standardFormat);
    }

    /// <summary>
    /// Returns the standard PCM or IEEE float WaveFormat equivalent to an extensible format,
    /// or null if the SubFormat is neither PCM nor IEEE float.
    /// </summary>
    private static WaveFormat ToStandardWaveFormat(WaveFormat waveFormat)
    {
        // A WaveFormatExtensible (e.g. built in code) exposes the SubFormat directly,
        // whereas reading from a file (WaveFileReader) yields a WaveFormatExtraData whose
        // 22-byte extension carries it after wValidBitsPerSample (2) and dwChannelMask (4).
        Guid subFormat;
        if (waveFormat is WaveFormatExtensible extensible)
        {
            subFormat = extensible.SubFormat;
        }
        else if (waveFormat is WaveFormatExtraData { ExtraSize: >= 22 } extraData)
        {
            subFormat = new Guid(extraData.ExtraData.AsSpan(6, 16));
        }
        else
        {
            return null;
        }

        if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM)
        {
            return new WaveFormat(waveFormat.SampleRate, waveFormat.BitsPerSample, waveFormat.Channels);
        }
        if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT)
        {
            return WaveFormat.CreateCustomFormat(WaveFormatEncoding.IeeeFloat,
                waveFormat.SampleRate, waveFormat.Channels, waveFormat.AverageBytesPerSecond,
                waveFormat.BlockAlign, waveFormat.BitsPerSample);
        }
        return null;
    }

    /// <summary>
    /// Re-presents a wave provider under a different (byte-compatible) WaveFormat. Reads
    /// pass straight through to the underlying provider.
    /// </summary>
    private class WaveFormatAdapter : IWaveProvider
    {
        private readonly IWaveProvider source;

        public WaveFormatAdapter(IWaveProvider source, WaveFormat waveFormat)
        {
            this.source = source;
            WaveFormat = waveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<byte> buffer) => source.Read(buffer);
    }
}
