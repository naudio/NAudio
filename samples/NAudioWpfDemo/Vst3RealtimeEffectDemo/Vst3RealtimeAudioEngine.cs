using System;
using NAudio.Vst3;
using NAudio.Wave;

namespace NAudioWpfDemo.Vst3RealtimeEffectDemo;

/// <summary>
/// One element of the realtime chain: a live <see cref="Vst3Plugin"/> instance plus a
/// volatile bypass flag. The audio thread reads <see cref="Bypass"/> per block; the UI
/// thread sets it.
/// </summary>
internal sealed class Vst3ChainSlot
{
    public Vst3ChainSlot(Vst3Plugin plugin)
    {
        Plugin = plugin;
    }

    public Vst3Plugin Plugin { get; }

    // No interlock — torn reads of a 1-byte bool don't happen on x86/x64, and the
    // ramping is up to the plug-in. Volatile keeps the JIT honest.
    private volatile bool bypass;
    public bool Bypass { get => bypass; set => bypass = value; }
}

/// <summary>
/// Drives full-duplex ASIO audio through an ordered chain of <see cref="Vst3Plugin"/>
/// instances. The chain is held as an atomically-swappable array so the UI thread can
/// rebuild it while the realtime callback keeps running. Includes feedback protection
/// (starts muted; auto-mutes on sustained near-full-scale output) and a per-block peak
/// meter — the same shape as the managed-effects branch's <c>RealtimeAudioEngine</c>,
/// adapted to VST 3's separate-input/output process model.
/// </summary>
/// <remarks>
/// <para>All plug-ins in the chain must be created with the engine's current
/// <see cref="SampleRate"/> and a <see cref="Vst3Plugin.MaxBlockSize"/> of at least
/// <see cref="FramesPerBuffer"/>. The viewmodel guarantees both by deferring plug-in
/// instantiation until <see cref="Start"/> has been called.</para>
/// <para>Phase 1 of this demo hardcodes stereo I/O end-to-end. A mono ASIO input is
/// duplicated to right; a mono ASIO output gets only the left channel.</para>
/// </remarks>
internal sealed class Vst3RealtimeAudioEngine : IDisposable
{
    private AsioDevice device;
    private float[] bufA = Array.Empty<float>();
    private float[] bufB = Array.Empty<float>();
    private volatile Vst3ChainSlot[] chain = Array.Empty<Vst3ChainSlot>();
    private volatile bool muted = true;
    private volatile bool autoMuted;
    private int inputChannels = 2;
    private int runawaySamples;
    private float outputLevel;
    private float masterGain = 1f; // volatile read/write via Volatile.Read/Write on the float bits

    public bool IsRunning => device != null && device.State == AsioDeviceState.Running;

    public int SampleRate { get; private set; }

    public int FramesPerBuffer { get; private set; }

    /// <summary>When true the output is silenced (feedback-safe default).</summary>
    public bool Muted
    {
        get => muted;
        set => muted = value;
    }

    /// <summary>Most recent output peak (0..1), for the level meter (measured post master-gain).</summary>
    public float OutputLevel => outputLevel;

    /// <summary>
    /// Linear master output gain (1.0 = unity). Applied to the final chain output before
    /// the mute check and peak meter, so the meter reflects what the listener hears.
    /// Safe to set from any thread — float assignment is atomic on x86/x64.
    /// </summary>
    public float MasterGain
    {
        get => masterGain;
        set => masterGain = value < 0f ? 0f : value;
    }

    /// <summary>
    /// Replaces the processing chain. The slots' plug-ins must already have been created
    /// at this engine's <see cref="SampleRate"/> and with a <see cref="Vst3Plugin.MaxBlockSize"/>
    /// of at least <see cref="FramesPerBuffer"/>. The array is published atomically so the
    /// audio thread picks it up on the next block. Caller-side disposal of plug-ins removed
    /// from the chain must wait at least one block — see <see cref="WaitForChainQuiesce"/>.
    /// </summary>
    public void SetChain(Vst3ChainSlot[] newChain)
    {
        chain = newChain ?? Array.Empty<Vst3ChainSlot>();
    }

    /// <summary>
    /// Blocks long enough that the audio callback is guaranteed to have moved past
    /// any previously-published chain (one ASIO buffer-period is enough; we use 50 ms
    /// as a safe upper bound for typical settings). Call before disposing plug-ins
    /// that have been removed from the chain.
    /// </summary>
    public static void WaitForChainQuiesce()
    {
        System.Threading.Thread.Sleep(50);
    }

    /// <summary>Returns true once if a feedback auto-mute fired since the last call.</summary>
    public bool ConsumeAutoMuted()
    {
        if (!autoMuted)
            return false;
        autoMuted = false;
        return true;
    }

    public void Start(string driverName, int inputChannelCount, int inputChannelOffset = 0)
    {
        Stop();
        inputChannels = inputChannelCount <= 1 ? 1 : 2;
        device = AsioDevice.Open(driverName);
        var capabilities = device.Capabilities;
        var offset = inputChannelOffset < 0 ? 0 : inputChannelOffset;
        if (offset + inputChannels > capabilities.NbInputChannels)
        {
            var asked = inputChannels == 2 ? $"{offset + 1}+{offset + 2}" : $"{offset + 1}";
            device.Dispose();
            device = null;
            throw new ArgumentException(
                $"Input channel {asked} not available — the driver exposes " +
                $"{capabilities.NbInputChannels} input channel(s).");
        }
        var inputs = inputChannels == 1
            ? new[] { offset }
            : new[] { offset, offset + 1 };
        var outputs = capabilities.NbOutputChannels >= 2 ? new[] { 0, 1 } : new[] { 0 };

        device.InitDuplex(new AsioDuplexOptions
        {
            InputChannels = inputs,
            OutputChannels = outputs,
            Processor = OnBuffer
        });

        SampleRate = device.CurrentSampleRate;
        FramesPerBuffer = device.FramesPerBuffer;
        // Two stereo scratch buffers for the ping-pong chain walk.
        bufA = new float[FramesPerBuffer * 2];
        bufB = new float[FramesPerBuffer * 2];
        muted = true;
        autoMuted = false;
        runawaySamples = 0;
        outputLevel = 0f;
        device.Start();
    }

    public void Stop()
    {
        if (device != null)
        {
            try { device.Stop(); }
            catch { /* driver may already be stopped */ }
            device.Dispose();
            device = null;
        }
        chain = Array.Empty<Vst3ChainSlot>();
        SampleRate = 0;
        FramesPerBuffer = 0;
        outputLevel = 0f;
    }

    private void OnBuffer(in AsioProcessBuffers buffers)
    {
        var frames = buffers.Frames;
        var samples = frames * 2;

        // Deinterleave input into bufA.
        var inputLeft = buffers.GetInput(0);
        var inputRight = inputChannels == 2 && buffers.InputChannelCount > 1
            ? buffers.GetInput(1)
            : inputLeft;
        var a = bufA;
        for (var i = 0; i < frames; i++)
        {
            a[i * 2] = inputLeft[i];
            a[i * 2 + 1] = inputRight[i];
        }

        // Run the chain: alternate buffers so the plug-in's input and output never alias.
        var slots = chain;
        float[] src = a, dst = bufB;
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.Bypass)
            {
                continue; // src already holds the dry signal
            }
            slot.Plugin.Process(src.AsSpan(0, samples), dst.AsSpan(0, samples), frames);
            (src, dst) = (dst, src);
        }

        // Master gain on the final buffer, then mute / peak meter / feedback protection.
        var gain = masterGain;
        if (gain != 1f)
        {
            for (var i = 0; i < samples; i++) src[i] *= gain;
        }

        var peak = 0f;
        if (muted)
        {
            Array.Clear(src, 0, samples);
        }
        else
        {
            for (var i = 0; i < samples; i++)
            {
                var v = src[i];
                var av = v < 0f ? -v : v;
                if (av > peak) peak = av;
            }
        }
        outputLevel = peak;

        // Interleaved → per-channel ASIO output.
        var outputLeftSpan = buffers.GetOutput(0);
        for (var i = 0; i < frames; i++)
            outputLeftSpan[i] = src[i * 2];
        if (buffers.OutputChannelCount > 1)
        {
            var outputRightSpan = buffers.GetOutput(1);
            for (var i = 0; i < frames; i++)
                outputRightSpan[i] = src[i * 2 + 1];
        }

        // ~1 s of near-full-scale output → auto-mute.
        if (!muted && peak > 0.98f)
        {
            runawaySamples += frames;
            if (runawaySamples > SampleRate)
            {
                muted = true;
                autoMuted = true;
                runawaySamples = 0;
            }
        }
        else
        {
            runawaySamples = 0;
        }
    }

    public void Dispose() => Stop();
}
