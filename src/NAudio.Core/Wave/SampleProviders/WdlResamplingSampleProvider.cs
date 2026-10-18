using System;
using NAudio.Dsp;

namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Fully managed resampling sample provider, based on the WDL Resampler
/// </summary>
/// <remarks>
/// Uses 64-tap windowed sinc interpolation (<see cref="WdlResamplerQuality.Sinc64"/>) by default, which is
/// flat across the audio band. Pass <see cref="WdlResamplerQuality.Linear"/> for the cheaper algorithm used
/// before NAudio 3.3. See <see cref="WdlResamplerQuality"/> for the trade-offs.
/// </remarks>
public class WdlResamplingSampleProvider : ISampleProvider
{
    private readonly WdlResampler resampler;
    private readonly WaveFormat outFormat;
    private readonly ISampleProvider source;
    private readonly int channels;

    /// <summary>
    /// Constructs a new resampler using <see cref="WdlResamplerQuality.Sinc64"/>
    /// </summary>
    /// <param name="source">Source to resample</param>
    /// <param name="newSampleRate">Desired output sample rate</param>
    public WdlResamplingSampleProvider(ISampleProvider source, int newSampleRate)
        : this(source, newSampleRate, WdlResamplerQuality.Sinc64)
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
        // A short read can mean the end of the stream or a source that is temporarily under-fed (a
        // BufferedWaveProvider with ReadFully off, say). Padding it with silence is right for the first
        // and wrong for the second, because in sinc mode the last few output samples would be computed
        // from the padding. So only flush once the source returns nothing; until then, hold back the
        // output that still needs input the source hasn't delivered. Linear mode reaches no further
        // than the next input sample, so it keeps flushing on every short read as it always has.
        bool flush = Quality == WdlResamplerQuality.Linear || inAvailable == 0;
        int outAvailable = resampler.ResampleOut(buffer, inAvailable, framesRequested, channels, flush);
        if (outAvailable == 0 && inAvailable > 0)
        {
            // Too little real input to complete a single output sample. Returning 0 would tell the
            // caller the stream had ended, so flush what is buffered instead.
            resampler.ResamplePrepare(framesRequested, channels, out _);
            outAvailable = resampler.ResampleOut(buffer, 0, framesRequested, channels, true);
        }
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
