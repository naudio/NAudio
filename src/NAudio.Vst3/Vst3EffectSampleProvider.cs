using System;
using NAudio.Wave;

namespace NAudio.Vst3;

/// <summary>
/// Wraps a <see cref="Vst3Plugin"/> as an <see cref="ISampleProvider"/> — pulls samples from a
/// source provider, feeds them through the plug-in, and returns the processed output to the
/// caller. By default also <i>renders the plug-in's tail</i> past source EOF, so reverbs, delays
/// and other effects with a release tail produce a full, untruncated render without the caller
/// having to know the tail length in advance.
/// </summary>
/// <remarks>
/// <para>
/// Block-size handling: callers may request any number of samples; the wrapper pulls from the
/// source in chunks no larger than <see cref="Vst3Plugin.MaxBlockSize"/>, processes each chunk,
/// and writes it to the caller's buffer.
/// </para>
/// <para>
/// Tail rendering (<see cref="RenderTail"/>, default <c>true</c>) takes over after the source
/// returns 0: the wrapper keeps feeding zero-input blocks to the plug-in and watches the output's
/// RMS. After <see cref="TailSilenceBlocks"/> consecutive blocks whose RMS is below
/// <see cref="TailSilenceThresholdDb"/> the wrapper declares the tail drained and returns 0.
/// <see cref="MaxTailDuration"/> caps the wait for plug-ins that never settle (granular freeze,
/// infinite feedback, etc.). Set <see cref="RenderTail"/> to <c>false</c> to recover the old
/// source-bound behaviour — useful when building a chain where downstream handles the tail.
/// </para>
/// <para>
/// Channel and sample-rate handling: the source must match the plug-in's negotiated
/// <see cref="Vst3Plugin.InputChannelCount"/> and <see cref="Vst3Plugin.SampleRate"/>. Output is
/// at the plug-in's <see cref="Vst3Plugin.OutputChannelCount"/> (often the same as input, but
/// mono→stereo plug-ins do change channel count).
/// </para>
/// </remarks>
public sealed class Vst3EffectSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly Vst3Plugin _plugin;
    private readonly float[] _scratchInput;

    // Phase state — strictly single-consumer (the Read caller).
    private bool _sourceExhausted;
    private bool _tailDrained;
    private long _tailFramesEmitted;
    private int _consecutiveSilentBlocks;

    // Latency compensation: -1 until the first Read snapshots the plug-in's latency, then counts down
    // the output frames still to be discarded from the front. _skipScratch is the throwaway sink.
    private int _framesToSkip = -1;
    private float[]? _skipScratch;

    /// <summary>
    /// Wires <paramref name="source"/> through <paramref name="plugin"/>. The source's
    /// <see cref="WaveFormat"/> must match the plug-in's input channel count and sample rate.
    /// </summary>
    public Vst3EffectSampleProvider(ISampleProvider source, Vst3Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(plugin);
        if (source.WaveFormat.Channels != plugin.InputChannelCount)
        {
            throw new ArgumentException(
                $"Source must be {plugin.InputChannelCount} channels; got {source.WaveFormat.Channels}.",
                nameof(source));
        }
        if (source.WaveFormat.SampleRate != plugin.SampleRate)
        {
            throw new ArgumentException(
                $"Source sample rate must match plug-in ({plugin.SampleRate} Hz); got {source.WaveFormat.SampleRate} Hz.",
                nameof(source));
        }

        _source = source;
        _plugin = plugin;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(plugin.SampleRate, plugin.OutputChannelCount);
        _scratchInput = new float[plugin.MaxBlockSize * Math.Max(1, plugin.InputChannelCount)];
    }

    /// <inheritdoc/>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// When <c>true</c> (the default), after the source returns 0 the wrapper keeps feeding the
    /// plug-in zero-input blocks until its output settles to silence, so reverb / delay tails are
    /// rendered in full. When <c>false</c>, the wrapper stops as soon as the source ends — the
    /// classic source-bound behaviour, suitable when the wrapper is one stage of a longer chain
    /// or when the caller is feeding an infinite (live) source.
    /// </summary>
    public bool RenderTail { get; init; } = true;

    /// <summary>
    /// Safety cap on the tail-rendering phase. Plug-ins that report <c>kInfiniteTail</c> or that
    /// genuinely never settle (granular freeze, infinite-feedback delay) hit this limit instead
    /// of hanging the render. Default: 30 seconds.
    /// </summary>
    public TimeSpan MaxTailDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Per-block output RMS threshold (in dBFS) for declaring a block "silent". A block whose RMS
    /// is below this value counts toward the consecutive-silent-blocks gate. Default −80 dBFS —
    /// well below any practical hearing threshold for typical playback levels.
    /// </summary>
    public double TailSilenceThresholdDb { get; init; } = -80.0;

    /// <summary>
    /// Number of consecutive silent output blocks required before the tail is considered drained.
    /// Default 4 — enough to avoid stopping on a momentary dip without significantly extending
    /// the render. Each block is up to <see cref="Vst3Plugin.MaxBlockSize"/> samples.
    /// </summary>
    public int TailSilenceBlocks { get; init; } = 4;

    /// <summary>
    /// When <c>true</c> (the default), the plug-in's processing latency
    /// (<see cref="Vst3Plugin.LatencySamples"/>) is compensated by discarding that many output frames
    /// from the front of the render, so the output is sample-aligned with the input. A no-op for the
    /// common zero-latency case. The dropped leading frames are the plug-in's pre-roll; the matching
    /// real samples at the end emerge during tail rendering (so keep <see cref="RenderTail"/> on for a
    /// full aligned render). Latency is snapshotted on the first <see cref="Read"/> and is <b>not</b>
    /// re-applied if the plug-in changes its latency at runtime (e.g. a linear-phase toggle that raises
    /// <see cref="Vst3Plugin.LatencyChanged"/>): a later change leaves a fixed alignment offset for the
    /// rest of the stream. Fine for an offline render; for live use, rebuild the chain to re-align.
    /// </summary>
    public bool CompensateLatency { get; init; } = true;

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="buffer"/>'s length must be a whole number of frames (a multiple of
    /// <see cref="WaveFormat"/>'s channel count). A non-frame-aligned length silently drops the
    /// trailing partial frame.
    /// </remarks>
    public int Read(Span<float> buffer)
    {
        if (_framesToSkip < 0)
        {
            _framesToSkip = CompensateLatency ? (int)_plugin.LatencySamples : 0;
        }
        if (_framesToSkip > 0)
        {
            SkipLatencyFrames();
        }
        return ReadCore(buffer);
    }

    // Produces and discards the plug-in's latency worth of output frames once, up front, so the signal
    // the caller sees is time-aligned with the source. Drives the same source/tail path as a normal read.
    private void SkipLatencyFrames()
    {
        var outChan = _plugin.OutputChannelCount;
        _skipScratch ??= new float[_plugin.MaxBlockSize * outChan];
        while (_framesToSkip > 0)
        {
            var chunkFrames = Math.Min(_framesToSkip, _plugin.MaxBlockSize);
            var produced = ReadCore(_skipScratch.AsSpan(0, chunkFrames * outChan));
            if (produced == 0)
            {
                _framesToSkip = 0; // source and tail drained before the latency was consumed
                break;
            }
            _framesToSkip -= produced / outChan;
        }
    }

    private int ReadCore(Span<float> buffer)
    {
        if (!_sourceExhausted)
        {
            var written = ReadFromSource(buffer);
            if (written > 0) return written;
            // Source just returned 0 — fall through to the tail phase if it's enabled.
        }
        if (RenderTail && !_tailDrained)
        {
            return ReadTailUntilSilent(buffer);
        }
        return 0;
    }

    private int ReadFromSource(Span<float> buffer)
    {
        var blockMax = _plugin.MaxBlockSize;
        var inChan = _plugin.InputChannelCount;
        var outChan = _plugin.OutputChannelCount;
        var framesRequested = buffer.Length / outChan;

        var framesWritten = 0;
        while (framesWritten < framesRequested)
        {
            var framesThisBlock = Math.Min(framesRequested - framesWritten, blockMax);
            var inSamplesThisBlock = framesThisBlock * inChan;

            var sourceSamples = _source.Read(_scratchInput.AsSpan(0, inSamplesThisBlock));
            if (sourceSamples == 0)
            {
                _sourceExhausted = true;
                break;
            }

            var blockFrames = sourceSamples / inChan;
            var outRegion = buffer.Slice(framesWritten * outChan, blockFrames * outChan);
            _plugin.Process(
                _scratchInput.AsSpan(0, blockFrames * inChan),
                outRegion,
                blockFrames);

            framesWritten += blockFrames;

            if (blockFrames < framesThisBlock)
            {
                // Source partially filled its request — treat as end-of-stream so the next Read
                // moves into the tail phase (rather than pestering a drained source).
                _sourceExhausted = true;
                break;
            }
        }

        return framesWritten * outChan;
    }

    private int ReadTailUntilSilent(Span<float> buffer)
    {
        var blockMax = _plugin.MaxBlockSize;
        var inChan = _plugin.InputChannelCount;
        var outChan = _plugin.OutputChannelCount;
        var framesRequested = buffer.Length / outChan;

        // RMS threshold pre-squared so we don't sqrt per block.
        var thresholdLinear = Math.Pow(10.0, TailSilenceThresholdDb / 20.0);
        var thresholdSquared = thresholdLinear * thresholdLinear;
        var maxTailFrames = (long)(MaxTailDuration.TotalSeconds * _plugin.SampleRate);

        var framesWritten = 0;
        while (framesWritten < framesRequested)
        {
            var framesThisBlock = Math.Min(framesRequested - framesWritten, blockMax);
            // Feed silence on the plug-in's input bus. Channel count can be zero for instruments;
            // _scratchInput was sized with Math.Max(1, …) so the span is always valid.
            var inSamplesThisBlock = framesThisBlock * inChan;
            if (inSamplesThisBlock > 0)
                Array.Clear(_scratchInput, 0, inSamplesThisBlock);

            var outRegion = buffer.Slice(framesWritten * outChan, framesThisBlock * outChan);
            _plugin.Process(
                _scratchInput.AsSpan(0, inSamplesThisBlock),
                outRegion,
                framesThisBlock);

            framesWritten += framesThisBlock;
            _tailFramesEmitted += framesThisBlock;

            if (BlockBelowSilenceThreshold(outRegion, thresholdSquared))
                _consecutiveSilentBlocks++;
            else
                _consecutiveSilentBlocks = 0;

            if (_consecutiveSilentBlocks >= TailSilenceBlocks || _tailFramesEmitted >= maxTailFrames)
            {
                _tailDrained = true;
                break;
            }
        }

        return framesWritten * outChan;
    }

    private static bool BlockBelowSilenceThreshold(ReadOnlySpan<float> block, double thresholdSquared)
    {
        if (block.IsEmpty) return true;
        double sumSquares = 0;
        for (var i = 0; i < block.Length; i++)
        {
            double v = block[i];
            sumSquares += v * v;
        }
        var meanSquare = sumSquares / block.Length;
        return meanSquare < thresholdSquared;
    }
}
