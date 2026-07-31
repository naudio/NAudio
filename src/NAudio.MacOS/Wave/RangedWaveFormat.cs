
using System;
using System.Runtime.Versioning;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.Wave;

/// <summary>
/// Provides information about a <see cref="WaveFormat"/> and it's acceptable sample rate range. <br />
/// This is the translated counterpart of the <c>AudioStreamRangedDescription</c> structure, and as such
/// is only existing and relevant with the macOS assembly.
/// </summary>
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("macos")]
public sealed class RangedWaveFormat
{
    private readonly WaveFormat baseFormat;
    private readonly double rangeMin, rangeMax;

    /// <summary>
    /// Constructs a new instance of the <see cref="RangedWaveFormat"/> class.
    /// </summary>
    /// <param name="format">The wave format that is the base descriptor of the audio stream.</param>
    /// <param name="sampleRateRangeMin">The minimum inclusive bound of the acceptable sample rate range.</param>
    /// <param name="sampleRateRangeMax">The maximum inclusive bound of the acceptable sample rate range.</param>
    public RangedWaveFormat(WaveFormat format, double sampleRateRangeMin, double sampleRateRangeMax)
    {
        ArgumentNullException.ThrowIfNull(format);
        baseFormat = format;
        rangeMin = sampleRateRangeMin;
        rangeMax = sampleRateRangeMax;
    }

    internal RangedWaveFormat(AudioStreamRangedDescription desc)
    {
        baseFormat = MacUtils.ConstructWaveFormatFromASBD(desc.mFormat);
        rangeMin = desc.mSampleRateRange.mMinimum;
        rangeMax = desc.mSampleRateRange.mMaximum;
    }

    /// <summary>
    /// The base format describing the most properties of the audio stream that this instance is attached to.
    /// </summary>
    public WaveFormat Format => baseFormat;

    /// <summary>
    /// The minimum inclusive bound of the acceptable sample range in application-provided formats.
    /// </summary>
    public double AcceptableSampleRangeMin => rangeMin;

    /// <summary>
    /// The maximum inclusive bound of the acceptable sample range in application-provided formats.
    /// </summary>
    public double AcceptableSampleRangeMax => rangeMax;

    /// <summary>
    /// Tests whether the specified audio format can be used instead.
    /// </summary>
    /// <param name="otherFormat">The audio format to use.</param>
    /// <returns>A value whether <paramref name="otherFormat"/> is satisfied by the requirements imposed by this class instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="otherFormat"/> is <see langword="null"/>.</exception>
    public bool IsWaveFormatSatisfied(WaveFormat otherFormat)
    {
        ArgumentNullException.ThrowIfNull(otherFormat);
        return otherFormat.BitsPerSample == baseFormat.BitsPerSample &&
                otherFormat.BlockAlign == baseFormat.BlockAlign &&
                otherFormat.Channels == baseFormat.Channels &&
                otherFormat.SampleRate >= rangeMin && otherFormat.SampleRate <= rangeMax;
    }

    /// <inheritdoc />
    public override string ToString() => $"Ranged Wave Format: {{ Format = {baseFormat}, AcceptableSampleRangeMin = {rangeMin}, AcceptableSampleRangeMax = {rangeMax} }}";
}