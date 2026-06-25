using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave;

/// <summary>
/// Resample-free format adaptation shared by <see cref="WasapiPlayer"/> and the obsolete
/// <see cref="WasapiOut"/>. Adapts a source's bit depth and channel count (PCM↔float, mono↔stereo)
/// to a device-supported format <em>without</em> changing the sample rate — converting the sample rate
/// would add latency, so callers handle that case explicitly (fall back or throw).
/// </summary>
internal static class WasapiFormatAdaptation
{
    /// <summary>
    /// Wraps <paramref name="source"/> so its output is byte-compatible with <paramref name="target"/>,
    /// converting bit depth and channel count but never the sample rate. Returns the source unchanged
    /// when it already matches (zero-copy), or null when adaptation would require resampling or an
    /// unsupported conversion.
    /// </summary>
    public static IWaveProvider AdaptProvider(IWaveProvider source, WaveFormat target)
    {
        var sourceFormat = source.WaveFormat;
        if (!TryDescribeAdaptation(sourceFormat, target, null))
            return null;

        if (IsByteCompatible(sourceFormat, target))
            return source;

        // TryDescribeAdaptation has guaranteed the channel change and encoding are both reachable,
        // so AdaptChannels and ConvertSamplesToWave will not return null here.
        var sample = source.ToSampleProvider();
        if (sample.WaveFormat.Channels != target.Channels)
            sample = AdaptChannels(sample, target.Channels);
        return ConvertSamplesToWave(sample, target);
    }

    /// <summary>
    /// Single source of truth for "can we adapt <paramref name="source"/> to <paramref name="target"/>
    /// without resampling?". Returns false if it would need a sample-rate change, an unsupported channel
    /// remap, or a bit depth we have no converter for. When <paramref name="conversions"/> is non-null
    /// it is filled with human-readable descriptions of each conversion step (empty if already
    /// byte-compatible). Both <see cref="AdaptProvider"/> and the capability API go through here so the
    /// executed pipeline and the advertised capability never disagree.
    /// </summary>
    public static bool TryDescribeAdaptation(WaveFormat source, WaveFormat target, List<string> conversions)
    {
        if (source.SampleRate != target.SampleRate)
            return false;
        if (IsByteCompatible(source, target))
            return true;

        var src = source.AsStandardWaveFormat();
        var tgt = target.AsStandardWaveFormat();

        if (source.Channels != target.Channels)
        {
            if (!CanAdaptChannels(source.Channels, target.Channels))
                return false;
            conversions?.Add($"{DescribeChannels(source.Channels)} → {DescribeChannels(target.Channels)}");
        }

        if (src.Encoding != tgt.Encoding || src.BitsPerSample != tgt.BitsPerSample)
        {
            if (!IsSupportedTargetEncoding(tgt))
                return false;
            conversions?.Add($"{DescribeEncoding(src)} → {DescribeEncoding(tgt)}");
        }

        return true;
    }

    /// <summary>
    /// Finds a device-supported exclusive-mode format at the source's sample rate that we can reach by
    /// latency-free bit-depth/channel conversion. Sample rate is fixed (never resampled), and only bit
    /// depths (16/24-bit PCM, 32-bit float) and channel counts we can actually convert to are
    /// considered. Returns null when no such format exists.
    /// </summary>
    public static WaveFormatExtensible FindSupportedExclusiveFormatAtSampleRate(AudioClient audioClient, WaveFormat sourceFormat)
    {
        int sampleRate = sourceFormat.SampleRate;
        int sourceChannels = sourceFormat.Channels;

        // Bit depths we have latency-free converters for, source's preferred first.
        var bitDepthsToTry = new List<int>();
        foreach (var b in new[] { sourceFormat.BitsPerSample, 32, 24, 16 })
            if ((b == 16 || b == 24 || b == 32) && !bitDepthsToTry.Contains(b))
                bitDepthsToTry.Add(b);

        // Channel counts we can adapt to without a custom mix matrix (passthrough or mono↔stereo).
        var channelCountsToTry = new List<int> { sourceChannels };
        foreach (var c in new[] { 2, 1 })
            if (!channelCountsToTry.Contains(c) && CanAdaptChannels(sourceChannels, c))
                channelCountsToTry.Add(c);

        foreach (var channelCount in channelCountsToTry)
            foreach (var bitDepth in bitDepthsToTry)
            {
                // channelMask 0 lets WaveFormatExtensible derive the canonical layout for the count.
                var format = new WaveFormatExtensible(sampleRate, bitDepth, channelCount, 0);
                if (audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, format))
                    return format;
            }
        return null;
    }

    /// <summary>
    /// True when two formats have the same interleaved byte layout (encoding, bit depth, channels and
    /// sample rate), so one can be played as the other with no conversion.
    /// </summary>
    public static bool IsByteCompatible(WaveFormat a, WaveFormat b)
    {
        var sa = a.AsStandardWaveFormat();
        var sb = b.AsStandardWaveFormat();
        return sa.Encoding == sb.Encoding &&
               sa.SampleRate == sb.SampleRate &&
               sa.Channels == sb.Channels &&
               sa.BitsPerSample == sb.BitsPerSample;
    }

    /// <summary>
    /// Whether <see cref="AdaptChannels"/> can convert between the given channel counts without a custom
    /// mixing matrix (identity, mono→stereo or stereo→mono).
    /// </summary>
    public static bool CanAdaptChannels(int from, int to) =>
        from == to || (from == 1 && to == 2) || (from == 2 && to == 1);

    /// <summary>Target encodings <see cref="ConvertSamplesToWave"/> can produce.</summary>
    public static bool IsSupportedTargetEncoding(WaveFormat standardFormat) =>
        (standardFormat.Encoding == WaveFormatEncoding.IeeeFloat && standardFormat.BitsPerSample == 32) ||
        (standardFormat.Encoding == WaveFormatEncoding.Pcm && (standardFormat.BitsPerSample == 16 || standardFormat.BitsPerSample == 24));

    private static ISampleProvider AdaptChannels(ISampleProvider sample, int targetChannels)
    {
        int sourceChannels = sample.WaveFormat.Channels;
        if (sourceChannels == targetChannels) return sample;
        if (sourceChannels == 1 && targetChannels == 2) return sample.ToStereo();
        if (sourceChannels == 2 && targetChannels == 1) return sample.ToMono();
        return null;
    }

    private static IWaveProvider ConvertSamplesToWave(ISampleProvider sample, WaveFormat target)
    {
        var std = target.AsStandardWaveFormat();
        if (std.Encoding == WaveFormatEncoding.IeeeFloat && std.BitsPerSample == 32)
            return new SampleToWaveProvider(sample);
        if (std.Encoding == WaveFormatEncoding.Pcm && std.BitsPerSample == 16)
            return new SampleToWaveProvider16(sample);
        if (std.Encoding == WaveFormatEncoding.Pcm && std.BitsPerSample == 24)
            return new SampleToWaveProvider24(sample);
        return null;
    }

    private static string DescribeChannels(int channels) => channels switch
    {
        1 => "mono",
        2 => "stereo",
        _ => $"{channels}ch"
    };

    private static string DescribeEncoding(WaveFormat standardFormat) =>
        standardFormat.Encoding == WaveFormatEncoding.IeeeFloat
            ? $"{standardFormat.BitsPerSample}-bit float"
            : $"{standardFormat.BitsPerSample}-bit PCM";
}
