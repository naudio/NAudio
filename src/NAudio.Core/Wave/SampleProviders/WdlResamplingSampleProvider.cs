using System;
using NAudio.Dsp;

namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Fully managed resampling sample provider, based on the WDL Resampler
/// </summary>
/// <remarks>
/// Uses 64-tap windowed sinc interpolation (<see cref="WdlResamplerQuality.Sinc64"/>) by default, which is
/// flat across the audio band. Pass <see cref="WdlResamplerQuality.Linear"/> for the cheaper algorithm used
/// before NAudio 3.4. See <see cref="WdlResamplerQuality"/> for the trade-offs.
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
    /// What to do when the source returns fewer samples than requested. Off by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When false, a short read from the source is taken as the end of the stream, which is what it
    /// means for a well-behaved source: the remaining output is flushed with silence standing in for
    /// the input after the end, and the provider returns its own short read followed by 0.
    /// </para>
    /// <para>
    /// Set this to true for a source that returns short reads mid-stream, such as a
    /// <see cref="BufferedWaveProvider"/> with <see cref="BufferedWaveProvider.ReadFully"/> off that is
    /// read faster than it is filled. Flushing every short read would compute the last few output samples
    /// of each read in the sinc modes from that silence instead of the input still to come. With this on,
    /// the provider holds back the output that still needs input the source hasn't delivered (about half
    /// the sinc kernel) and returns it on a later read, flushing only when the source returns 0. The cost
    /// is that this provider is then not well-behaved itself: its short read can be followed by more
    /// samples, so a consumer that treats a short read as the end (<see cref="MixingSampleProvider"/>
    /// removes the input) loses the held-back tail.
    /// </para>
    /// <para>
    /// Has no effect in <see cref="WdlResamplerQuality.Linear"/> mode, which never reaches past the next
    /// input sample.
    /// </para>
    /// </remarks>
    public bool HoldBackOnShortReads { get; set; }

    /// <summary>
    /// Reads from this sample provider
    /// </summary>
    public int Read(Span<float> buffer)
    {
        int framesRequested = buffer.Length / channels;
        int inNeeded = resampler.ResamplePrepare(framesRequested, outFormat.Channels, out Span<float> inBuffer);
        int inAvailable = source.Read(inBuffer[..(inNeeded * channels)]) / channels;
        // A short read from a well-behaved source is the end of the stream, so flush it. Only an
        // under-fed source opted into with HoldBackOnShortReads waits for the source to return nothing.
        bool flush = !HoldBackOnShortReads || Quality == WdlResamplerQuality.Linear || inAvailable == 0;
        int outAvailable = resampler.ResampleOut(buffer, inAvailable, framesRequested, channels, flush);
        if (!flush && outAvailable == 0 && inAvailable > 0)
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
