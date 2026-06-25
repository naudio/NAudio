using System;
using NAudio.Effects;
using NAudio.Wave;

namespace NAudioWpfDemo.RealtimeEffectsDemo;

/// <summary>
/// Drives full-duplex ASIO audio through an effect chain. The chain is held as an
/// atomically-swappable array so the UI thread can rebuild it while the real-time
/// callback keeps running. Includes feedback protection (starts muted; auto-mutes on
/// sustained near-full-scale output).
/// </summary>
internal class RealtimeAudioEngine : IDisposable
{
    private AsioDevice device;
    private float[] scratch = Array.Empty<float>();
    private float[] prewarm = Array.Empty<float>();
    private IAudioEffect[] attached = Array.Empty<IAudioEffect>();
    private volatile IAudioEffect[] effects = Array.Empty<IAudioEffect>();
    private volatile bool muted = true;
    private volatile bool autoMuted;
    private int inputChannels = 2;
    private int runawaySamples;
    private float outputLevel;

    public bool IsRunning => device != null && device.State == AsioDeviceState.Running;

    public int SampleRate { get; private set; }

    /// <summary>
    /// Carries parameter edits from the UI thread to the audio thread. Drained
    /// at the top of every ASIO block so setters run where there is no
    /// concurrent reader.
    /// </summary>
    public ParameterDispatchQueue Parameters { get; } = new ParameterDispatchQueue();

    /// <summary>When true the output is silenced (feedback-safe default).</summary>
    public bool Muted
    {
        get => muted;
        set => muted = value;
    }

    /// <summary>Most recent output peak (0..1), for the level meter.</summary>
    public float OutputLevel => outputLevel;

    /// <summary>
    /// Replaces the processing chain. The effects must already be configured for the
    /// engine's sample rate and stereo. Thread-safe against the audio callback:
    /// newly added effects are warmed (one silent block, off the audio thread) so
    /// their one-time buffer sizing never lands on the ASIO callback, and their
    /// parameters are routed through <see cref="Parameters"/> before the chain is
    /// published so a UI edit can never race the audio thread inline.
    /// </summary>
    public void SetEffects(IAudioEffect[] chain)
    {
        chain ??= Array.Empty<IAudioEffect>();

        if (IsRunning)
        {
            foreach (var e in chain)
                if (!Contains(attached, e))
                    Prewarm(e);
            foreach (var e in chain)
                if (e is IParameterized p)
                    Parameters.Attach(p);

            effects = chain; // atomic publish to the audio thread

            foreach (var e in attached)
                if (!Contains(chain, e) && e is IParameterized p)
                    Parameters.Detach(p);
            attached = chain;
        }
        else
        {
            effects = chain;
            Unbind();
        }
    }

    private void Prewarm(IAudioEffect effect)
    {
        if (prewarm.Length == 0)
            return;
        Array.Clear(prewarm, 0, prewarm.Length);
        effect.Process(prewarm.AsSpan());
    }

    private void Unbind()
    {
        foreach (var e in attached)
            if (e is IParameterized p)
                Parameters.Detach(p);
        attached = Array.Empty<IAudioEffect>();
    }

    private static bool Contains(IAudioEffect[] array, IAudioEffect effect)
    {
        foreach (var e in array)
            if (ReferenceEquals(e, effect))
                return true;
        return false;
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
        scratch = new float[device.FramesPerBuffer * 2];
        prewarm = new float[device.FramesPerBuffer * 2];
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
        Unbind();
        outputLevel = 0f;
    }

    private void OnBuffer(in AsioProcessBuffers buffers)
    {
        Parameters.Drain(); // apply pending UI edits on the audio thread, pre-block

        var frames = buffers.Frames;
        var samples = frames * 2;
        var buffer = scratch;

        var inputLeft = buffers.GetInput(0);
        var inputRight = inputChannels == 2 && buffers.InputChannelCount > 1
            ? buffers.GetInput(1)
            : inputLeft;

        for (var i = 0; i < frames; i++)
        {
            buffer[i * 2] = inputLeft[i];
            buffer[i * 2 + 1] = inputRight[i];
        }

        var chain = effects;
        for (var e = 0; e < chain.Length; e++)
            chain[e].Process(buffer.AsSpan(0, samples));

        var peak = 0f;
        if (muted)
        {
            Array.Clear(buffer, 0, samples);
        }
        else
        {
            for (var i = 0; i < samples; i++)
            {
                var a = buffer[i] < 0f ? -buffer[i] : buffer[i];
                if (a > peak)
                    peak = a;
            }
        }
        outputLevel = peak;

        var outputLeftSpan = buffers.GetOutput(0);
        for (var i = 0; i < frames; i++)
            outputLeftSpan[i] = buffer[i * 2];
        if (buffers.OutputChannelCount > 1)
        {
            var outputRightSpan = buffers.GetOutput(1);
            for (var i = 0; i < frames; i++)
                outputRightSpan[i] = buffer[i * 2 + 1];
        }

        // Feedback protection: ~1 s of near-full-scale output → auto-mute.
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
