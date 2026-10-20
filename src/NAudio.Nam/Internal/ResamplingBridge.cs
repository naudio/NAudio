using System;
using NAudio.Dsp;

namespace NAudio.Nam.Internal;

/// <summary>
/// Runs a mono process at a different sample rate from the stream it sits in: each block
/// is resampled to the process rate, processed, and resampled back, with 64-tap sinc
/// interpolation both ways. The output has a fixed latency, which a FIFO pre-filled with
/// silence makes up: the resamplers need input from slightly ahead of each output sample,
/// so they hand back a few samples less than they are given until the stream moves on.
/// Allocation-free after construction.
/// </summary>
internal sealed class ResamplingBridge
{
    /// <summary>The largest block processed at once; longer spans are split.</summary>
    public const int MaxChunkFrames = 256;

    private const int SincSize = 64;

    private readonly WdlResampler up;
    private readonly WdlResampler down;
    private readonly float[] processBuffer;  // the block at the process rate
    private readonly float[] returnBuffer;   // the block back at the stream rate
    private readonly float[] fifo;
    private readonly int prefill;
    private int fifoStart;
    private int fifoCount;

    public delegate void BlockProcessor(Span<float> samples);

    public ResamplingBridge(int streamRate, int processRate)
    {
        if (streamRate <= 0 || processRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(streamRate), "Sample rates must be positive.");
        StreamRate = streamRate;
        ProcessRate = processRate;
        up = CreateResampler(streamRate, processRate);
        down = CreateResampler(processRate, streamRate);

        var ratio = (double)processRate / streamRate;
        processBuffer = new float[(int)Math.Ceiling(MaxChunkFrames * ratio) + 2 * SincSize];
        returnBuffer = new float[(int)Math.Ceiling(processBuffer.Length / ratio) + 2 * SincSize];

        // How far the returned audio can fall behind what has been taken in depends on the
        // rates and the block sizes, so measure it: run the bridge with no processing over a
        // spread of block sizes, including single frames and full chunks.
        fifo = new float[returnBuffer.Length + 4 * MaxChunkFrames];
        prefill = MeasureShortfall() + 2;
        if (prefill + MaxChunkFrames + returnBuffer.Length > fifo.Length)
            fifo = new float[prefill + MaxChunkFrames + returnBuffer.Length];

        // The latency is where an impulse comes out, with no processing in between.
        LatencySamples = MeasureLatency();

        // Grow the resamplers' input buffers to their high-water marks now, so that processing
        // never has to.
        up.ResamplePrepare(MaxChunkFrames + 2 * SincSize, 1, out _);
        down.ResamplePrepare(processBuffer.Length + 2 * SincSize, 1, out _);
        Reset();
    }

    public int StreamRate { get; }
    public int ProcessRate { get; }

    /// <summary>The fixed delay the bridge adds, in samples at the stream rate.</summary>
    public int LatencySamples { get; }

    /// <summary>Samples of silence output because the bridge ran dry. Zero in correct operation.</summary>
    public long UnderrunSamples { get; private set; }

    /// <summary>Processes <paramref name="samples"/> in place, through <paramref name="process"/> at the process rate.</summary>
    public void Process(Span<float> samples, BlockProcessor process)
    {
        for (var offset = 0; offset < samples.Length; offset += MaxChunkFrames)
        {
            var block = samples.Slice(offset, Math.Min(MaxChunkFrames, samples.Length - offset));
            var processed = Resample(up, block, processBuffer);
            var atProcessRate = processBuffer.AsSpan(0, processed);
            process(atProcessRate);
            Push(returnBuffer.AsSpan(0, Resample(down, atProcessRate, returnBuffer)));
            Pop(block);
        }
    }

    /// <summary>Clears the resampler histories and refills the FIFO with its silent lead-in.</summary>
    public void Reset()
    {
        up.Reset();
        down.Reset();
        Array.Clear(fifo);
        fifoStart = 0;
        fifoCount = prefill;
        UnderrunSamples = 0;
    }

    private static WdlResampler CreateResampler(int from, int to)
    {
        var resampler = new WdlResampler();
        resampler.SetMode(false, 0, true, SincSize, 32);
        resampler.SetFeedMode(true); // input-driven: every input sample is consumed each call
        resampler.SetRates(from, to);
        return resampler;
    }

    private static int Resample(WdlResampler resampler, ReadOnlySpan<float> input, float[] output)
    {
        var prepared = resampler.ResamplePrepare(input.Length, 1, out var inputBuffer);
        input[..prepared].CopyTo(inputBuffer);
        return resampler.ResampleOut(output, prepared, output.Length, 1);
    }

    private void Push(ReadOnlySpan<float> samples)
    {
        var end = (fifoStart + fifoCount) % fifo.Length;
        var first = Math.Min(samples.Length, fifo.Length - end);
        samples[..first].CopyTo(fifo.AsSpan(end));
        samples[first..].CopyTo(fifo);
        fifoCount += samples.Length;
    }

    private void Pop(Span<float> destination)
    {
        var available = Math.Min(destination.Length, fifoCount);
        var first = Math.Min(available, fifo.Length - fifoStart);
        fifo.AsSpan(fifoStart, first).CopyTo(destination);
        fifo.AsSpan(0, available - first).CopyTo(destination[first..]);
        fifoStart = (fifoStart + available) % fifo.Length;
        fifoCount -= available;
        if (available < destination.Length)
        {
            destination[available..].Clear();
            UnderrunSamples += destination.Length - available;
        }
    }

    private int MeasureShortfall()
    {
        var random = new Random(1);
        var block = new float[MaxChunkFrames];
        long taken = 0, returned = 0, worst = 0;
        for (var i = 0; i < 400; i++)
        {
            var frames = (i % 4) switch
            {
                0 => MaxChunkFrames,
                1 => 1,
                _ => 1 + random.Next(MaxChunkFrames),
            };
            var span = block.AsSpan(0, frames);
            var processed = Resample(up, span, processBuffer);
            returned += Resample(down, processBuffer.AsSpan(0, processed), returnBuffer);
            taken += frames;
            worst = Math.Max(worst, taken - returned);
        }
        up.Reset();
        down.Reset();
        return (int)worst;
    }

    private int MeasureLatency()
    {
        Reset();
        var length = 4 * (prefill + 2 * SincSize);
        var signal = new float[length];
        signal[0] = 1f;
        Process(signal, static _ => { });
        var peak = 0;
        for (var i = 1; i < signal.Length; i++)
        {
            if (Math.Abs(signal[i]) > Math.Abs(signal[peak]))
                peak = i;
        }
        return peak;
    }
}
