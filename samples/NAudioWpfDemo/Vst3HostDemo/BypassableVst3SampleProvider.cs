using System;
using NAudio.Vst3;
using NAudio.Wave;

namespace NAudioWpfDemo.Vst3HostDemo;

/// <summary>
/// Routes a source provider through a <see cref="Vst3Plugin"/> (like
/// <see cref="Vst3EffectSampleProvider"/>) but adds a live <see cref="Bypass"/> toggle that
/// passes the dry signal through. Reads run on the audio thread; <see cref="Bypass"/> is set from
/// the UI thread, hence the volatile backing field.
/// </summary>
/// <remarks>
/// The dry path is delayed by the plug-in's reported latency (<see cref="Vst3Plugin.LatencySamples"/>)
/// so that switching bypass on and off is time-aligned with the wet (processed) signal — no
/// jump-forward by the latency amount on engage, no jump-back on bypass. The delay line is kept
/// running even while the effect is active, so a later bypass is already aligned. For zero-latency
/// plug-ins (the common case) this is a straight passthrough.
/// </remarks>
internal class BypassableVst3SampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly Vst3Plugin plugin;
    private readonly float[] scratch;
    private readonly bool canBypass;
    private readonly float[] dryDelay; // ring buffer delaying the dry path by the plug-in's latency
    private int dryDelayPos;
    private volatile bool bypass;

    public BypassableVst3SampleProvider(ISampleProvider source, Vst3Plugin plugin)
    {
        if (source.WaveFormat.Channels != plugin.InputChannelCount)
            throw new ArgumentException($"Source must be {plugin.InputChannelCount} channels.", nameof(source));
        if (source.WaveFormat.SampleRate != plugin.SampleRate)
            throw new ArgumentException($"Source sample rate must be {plugin.SampleRate} Hz.", nameof(source));

        this.source = source;
        this.plugin = plugin;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(plugin.SampleRate, plugin.OutputChannelCount);
        scratch = new float[plugin.MaxBlockSize * plugin.InputChannelCount];
        // Dry passthrough only makes sense when the channel count is unchanged by the effect.
        canBypass = plugin.InputChannelCount == plugin.OutputChannelCount;
        var delaySamples = (int)plugin.LatencySamples * plugin.OutputChannelCount;
        dryDelay = canBypass && delaySamples > 0 ? new float[delaySamples] : null;
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>Whether the effect can be bypassed (input and output channel counts match).</summary>
    public bool CanBypass => canBypass;

    /// <summary>When <c>true</c> (and <see cref="CanBypass"/>), the dry signal is passed through.</summary>
    public bool Bypass
    {
        get => bypass;
        set => bypass = value && canBypass;
    }

    public int Read(Span<float> buffer)
    {
        int blockMax = plugin.MaxBlockSize;
        int inChan = plugin.InputChannelCount;
        int outChan = plugin.OutputChannelCount;
        int framesRequested = buffer.Length / outChan;
        bool dry = bypass; // sample once per Read so the path can't flip mid-buffer

        int framesWritten = 0;
        while (framesWritten < framesRequested)
        {
            int framesThisBlock = Math.Min(framesRequested - framesWritten, blockMax);
            int read = source.Read(scratch.AsSpan(0, framesThisBlock * inChan));
            if (read == 0)
            {
                break;
            }
            int blockFrames = read / inChan;
            var inBlock = scratch.AsSpan(0, blockFrames * inChan);
            var outBlock = buffer.Slice(framesWritten * outChan, blockFrames * outChan);

            if (dry)
            {
                // Dry, latency-compensated, so a bypass toggle stays aligned with the wet path.
                AdvanceDryDelay(inBlock, outBlock);
            }
            else
            {
                plugin.Process(inBlock, outBlock, blockFrames);
                // Keep the dry delay line running while active so a later bypass is already aligned.
                AdvanceDryDelay(inBlock, Span<float>.Empty);
            }

            framesWritten += blockFrames;
            if (blockFrames < framesThisBlock)
            {
                break;
            }
        }
        return framesWritten * outChan;
    }

    // Pushes one block through the dry delay line. When output is non-empty, writes the delayed
    // signal into it; when empty, just advances the line (used while the effect is active). With no
    // delay line (zero-latency plug-in) it's a straight copy / no-op.
    private void AdvanceDryDelay(ReadOnlySpan<float> input, Span<float> output)
    {
        if (dryDelay == null)
        {
            if (!output.IsEmpty) input.CopyTo(output);
            return;
        }
        int len = dryDelay.Length;
        for (int i = 0; i < input.Length; i++)
        {
            float delayed = dryDelay[dryDelayPos];
            dryDelay[dryDelayPos] = input[i];
            if (++dryDelayPos == len) dryDelayPos = 0;
            if (!output.IsEmpty) output[i] = delayed;
        }
    }
}
