using System;
using NAudio.Dsp;

namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Fully managed resampling sample provider, based on the WDL Resampler
/// </summary>
/// <remarks>
/// The default (<see cref="WdlResamplerQuality.Linear"/>) is cheap but rolls off high frequencies and
/// lets some aliasing through. For quality-sensitive work, pass <see cref="WdlResamplerQuality.Sinc64"/>,
/// which is flat across the audio band. See <see cref="WdlResamplerQuality"/> for the trade-offs.
/// </remarks>
public class WdlResamplingSampleProvider : ISampleProvider
{
    private readonly WdlResampler resampler;
    private readonly WaveFormat outFormat;
    private readonly ISampleProvider source;
    private readonly int channels;

    /// <summary>
    /// Constructs a new resampler using <see cref="WdlResamplerQuality.Linear"/>
    /// </summary>
    /// <param name="source">Source to resample</param>
    /// <param name="newSampleRate">Desired output sample rate</param>
    public WdlResamplingSampleProvider(ISampleProvider source, int newSampleRate)
        : this(source, newSampleRate, WdlResamplerQuality.Linear)
    {
    }

    /// <summary>
    /// Constructs a new resampler with the specified quality
    /// </summary>
    /// <param name="source">Source to resample</param>
    /// <param name="newSampleRate">Desired output sample rate</param>
    /// <param name="quality">Resampling algorithm to use</param>
    public WdlResamplingSampleProvider(ISampleProvider source, int newSampleRate, WdlResamplerQuality quality)
    {
        channels = source.WaveFormat.Channels;
        outFormat = WaveFormat.CreateIeeeFloatWaveFormat(newSampleRate, channels);
        this.source = source;
        Quality = quality;

        resampler = new WdlResampler();
        switch (quality)
        {
            case WdlResamplerQuality.Linear:
                resampler.SetMode(true, 2, false);
                resampler.SetFilterParms();
                break;
            case WdlResamplerQuality.Sinc16:
                resampler.SetMode(false, 0, true, 16, 32);
                break;
            case WdlResamplerQuality.Sinc64:
                resampler.SetMode(false, 0, true, 64, 32);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(quality), quality, "Unknown resampler quality");
        }
        resampler.SetFeedMode(false); // output driven
        resampler.SetRates(source.WaveFormat.SampleRate, newSampleRate);
    }

    /// <summary>
    /// The resampling algorithm this provider was constructed with
    /// </summary>
    public WdlResamplerQuality Quality { get; }

    /// <summary>
    /// Reads from this sample provider
    /// </summary>
    public int Read(Span<float> buffer)
    {
        int framesRequested = buffer.Length / channels;
        int inNeeded = resampler.ResamplePrepare(framesRequested, outFormat.Channels, out Span<float> inBuffer);
        int inAvailable = source.Read(inBuffer[..(inNeeded * channels)]) / channels;
        int outAvailable = resampler.ResampleOut(buffer, inAvailable, framesRequested, channels);
        return outAvailable * channels;
    }

    /// <summary>
    /// Output WaveFormat
    /// </summary>
    public WaveFormat WaveFormat
    {
        get { return outFormat; }
    }
}
