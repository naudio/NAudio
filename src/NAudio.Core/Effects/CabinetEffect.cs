using System;
using System.IO;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Guitar or bass speaker-cabinet simulation: convolves the signal with a cabinet
/// impulse response (IR) with no added latency. The IR is resampled to the stream's
/// sample rate, its silent tail trimmed and its level normalised when it is loaded,
/// so IR files can be used as they come. With no IR set the effect is a transparent
/// pass-through.
/// </summary>
/// <remarks>
/// <para>
/// Use this after an amp simulator, such as a Neural Amp Modeler capture, that
/// models the amp alone. Unlike <see cref="ConvolutionReverbEffect"/> it defaults to
/// fully wet and zero latency, which is what a cabinet needs. The first
/// <see cref="PartitionSize"/> samples of the IR are applied directly and the rest by
/// partitioned FFT convolution (see <see cref="PartitionedConvolver"/>).
/// </para>
/// <para>
/// Loading or changing the IR, or any of the preparation settings, rebuilds the
/// convolution engine on the calling thread and then swaps it in, so it is safe to
/// do from a UI thread while audio plays. The new IR starts with empty history.
/// </para>
/// </remarks>
public sealed class CabinetEffect : AudioEffect
{
    // Trailing IR samples quieter than this relative to the peak are dropped.
    private const double TrimThresholdDb = -80.0;

    private float[][]? sourceIrs;
    private int sourceRate;
    // Everything the audio thread needs, built off the audio thread and published with
    // one reference write, so a block never sees convolvers from one configuration and
    // settings from another. Null when there is no IR (pass-through).
    private volatile Engine? engine;
    private int partitionSize = 64;
    private bool normalize = true;
    private bool trimSilence = true;
    private TimeSpan? maxLength;
    private bool monoInput;

    /// <summary>
    /// Loads a cabinet IR from a WAV file. A mono file is applied to every channel; a
    /// file with several channels supplies one IR per channel. Any sample rate is
    /// accepted.
    /// </summary>
    public void LoadImpulseResponse(string path)
    {
        using var stream = File.OpenRead(path);
        LoadImpulseResponse(stream);
    }

    /// <summary>Loads a cabinet IR from a stream containing a WAV file.</summary>
    /// <inheritdoc cref="LoadImpulseResponse(string)" path="/summary"/>
    public void LoadImpulseResponse(Stream wavStream)
    {
        ArgumentNullException.ThrowIfNull(wavStream);
        using var reader = new WaveFileReader(wavStream);
        var provider = reader.ToSampleProvider();
        var channels = provider.WaveFormat.Channels;
        var frames = (int)(reader.SampleCount);
        var interleaved = new float[frames * channels];
        var read = 0;
        while (read < interleaved.Length)
        {
            var n = provider.Read(interleaved.AsSpan(read));
            if (n == 0)
                break;
            read += n;
        }
        frames = read / channels;
        var irs = new float[channels][];
        for (var ch = 0; ch < channels; ch++)
        {
            irs[ch] = new float[frames];
            for (var f = 0; f < frames; f++)
                irs[ch][f] = interleaved[f * channels + ch];
        }
        SetImpulseResponse(irs, provider.WaveFormat.SampleRate);
    }

    /// <summary>Sets a mono IR, applied to every channel, recorded at <paramref name="sampleRate"/>.</summary>
    public void SetImpulseResponse(float[] impulseResponse, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(impulseResponse);
        SetImpulseResponse(new[] { impulseResponse }, sampleRate);
    }

    /// <summary>
    /// Sets one IR per channel, recorded at <paramref name="sampleRate"/>. If there are
    /// fewer IRs than channels, the last IR is used for the remaining channels.
    /// </summary>
    public void SetImpulseResponse(float[][] perChannelImpulseResponses, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(perChannelImpulseResponses);
        if (perChannelImpulseResponses.Length == 0)
            throw new ArgumentException("At least one impulse response is required.", nameof(perChannelImpulseResponses));
        foreach (var ir in perChannelImpulseResponses)
        {
            if (ir == null || ir.Length == 0)
                throw new ArgumentException("Impulse responses must not be empty.", nameof(perChannelImpulseResponses));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        sourceIrs = perChannelImpulseResponses;
        sourceRate = sampleRate;
        Rebuild();
    }

    /// <summary>Removes the IR, making the effect a pass-through.</summary>
    public void ClearImpulseResponse()
    {
        sourceIrs = null;
        Rebuild();
    }

    /// <summary>
    /// When true (the default), the IR is scaled so white noise passes at an
    /// unchanged RMS level, which keeps different IRs at similar loudness. With
    /// several channels one common gain is used, preserving their balance.
    /// </summary>
    public bool Normalize
    {
        get => normalize;
        set { normalize = value; Rebuild(); }
    }

    /// <summary>
    /// When true (the default), trailing IR samples more than 80 dB below the IR's
    /// peak are dropped. Many cabinet IR files are padded with near-silence, and
    /// trimming it saves CPU without changing the sound.
    /// </summary>
    public bool TrimSilence
    {
        get => trimSilence;
        set { trimSilence = value; Rebuild(); }
    }

    /// <summary>
    /// Optional cap on the IR length, applied with a short fade-out. Cabinet IRs
    /// rarely need more than 50–100 ms; shorter IRs use less CPU. Null (the default)
    /// keeps the whole IR.
    /// </summary>
    public TimeSpan? MaxLength
    {
        get => maxLength;
        set
        {
            if (value is { } v && v <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxLength must be positive.");
            maxLength = value;
            Rebuild();
        }
    }

    /// <summary>
    /// When true, the channels are averaged and convolved once, and the result is
    /// written to every channel (or, with one IR per channel, the mono signal goes
    /// through each channel's IR). Set this when the input is mono in a multi-channel
    /// stream, such as the output of a <c>NamEffect</c>, to halve the CPU cost.
    /// Default false.
    /// </summary>
    public bool MonoInput
    {
        get => monoInput;
        set { monoInput = value; Rebuild(); }
    }

    /// <summary>
    /// The FFT partition size (a power of two). The first this-many IR samples are
    /// applied directly, so larger values cost more per sample there but less in the
    /// FFT part. 64 (the default) suits typical cabinet IRs; latency is zero
    /// regardless.
    /// </summary>
    public int PartitionSize
    {
        get => partitionSize;
        set
        {
            if (value < 1 || (value & (value - 1)) != 0)
                throw new ArgumentException("Partition size must be a power of two.", nameof(value));
            partitionSize = value;
            Rebuild();
        }
    }

    /// <summary>
    /// Length in samples, at the stream's sample rate, of the IR as prepared for
    /// convolution (after resampling, trimming and <see cref="MaxLength"/>). Zero
    /// before the effect is configured or when no IR is set.
    /// </summary>
    public int ImpulseResponseLength { get; private set; }

    /// <inheritdoc />
    public override int LatencySamples => 0;

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format) => Rebuild();

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var active = engine;
        var channels = Channels;
        // No IR, or a rebuild for a new channel count has not been published yet.
        if (active == null || active.Channels != channels)
            return;
        var convolvers = active.Convolvers;

        if (!active.MonoInput || channels == 1)
        {
            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] = convolvers[ch].Process(buffer[i + ch]);
            }
            return;
        }

        var scale = 1f / channels;
        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            var sum = 0f;
            for (var ch = 0; ch < channels; ch++)
                sum += buffer[i + ch];
            var x = sum * scale;
            if (convolvers.Length == 1)
            {
                var y = convolvers[0].Process(x);
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] = y;
            }
            else
            {
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] = convolvers[ch].Process(x);
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        if (engine is { } active)
        {
            foreach (var convolver in active.Convolvers)
                convolver.Reset();
        }
    }

    private void Rebuild()
    {
        var format = WaveFormat;
        var irs = sourceIrs;
        if (format == null || irs == null)
        {
            engine = null;
            ImpulseResponseLength = 0;
            return;
        }

        var prepared = new float[irs.Length][];
        var length = 0;
        for (var i = 0; i < irs.Length; i++)
            prepared[i] = ImpulseResponseTools.Resample(irs[i], sourceRate, format.SampleRate);
        if (trimSilence)
        {
            // Trim all channels to the same length so a stereo IR stays aligned.
            foreach (var ir in prepared)
                length = Math.Max(length, ImpulseResponseTools.TrimmedLength(ir, TrimThresholdDb));
        }
        else
        {
            foreach (var ir in prepared)
                length = Math.Max(length, ir.Length);
        }
        var fadeLength = Math.Max(1, format.SampleRate / 1000); // 1 ms
        if (maxLength is { } cap)
        {
            var capSamples = Math.Max(1, (int)Math.Round(cap.TotalSeconds * format.SampleRate));
            if (capSamples < length)
                length = capSamples;
        }
        for (var i = 0; i < prepared.Length; i++)
            prepared[i] = ImpulseResponseTools.Truncate(prepared[i], length, fadeLength);
        if (normalize)
            ImpulseResponseTools.NormalizeEnergy(prepared);

        var channels = format.Channels;
        var mono = monoInput;
        var count = mono && prepared.Length == 1 ? 1 : channels;
        var built = new PartitionedConvolver[count];
        for (var ch = 0; ch < count; ch++)
            built[ch] = new PartitionedConvolver(prepared[Math.Min(ch, prepared.Length - 1)], partitionSize, zeroLatency: true);

        ImpulseResponseLength = length;
        engine = new Engine(built, mono, channels);
    }

    /// <summary>One prepared configuration: a convolver per channel (or one shared, for a mono IR with <see cref="MonoInput"/>).</summary>
    private sealed class Engine(PartitionedConvolver[] convolvers, bool monoInput, int channels)
    {
        public PartitionedConvolver[] Convolvers { get; } = convolvers;
        public bool MonoInput { get; } = monoInput;
        public int Channels { get; } = channels;
    }
}
