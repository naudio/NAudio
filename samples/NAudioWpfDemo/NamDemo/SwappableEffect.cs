using System;
using System.Diagnostics;
using System.Threading;
using NAudio.Effects;
using NAudio.Wave;

namespace NAudioWpfDemo.NamDemo;

/// <summary>
/// A chain slot whose effect can be replaced while audio is running: the new effect is
/// configured on the calling (UI) thread and then published with a single reference
/// write, so the audio thread sees either the old effect or the new one. It also times
/// the effect's processing, for the demo's CPU readout.
/// </summary>
internal sealed class SwappableEffect : IAudioEffect
{
    private volatile IAudioEffect current;
    private WaveFormat format;
    private long busyTicks;
    private long processedFrames;

    public IAudioEffect Current => current;

    /// <summary>Replaces the effect (null empties the slot, making it a pass-through).</summary>
    public void Swap(IAudioEffect effect)
    {
        var configuredFor = format;
        if (effect != null && configuredFor != null)
            effect.Configure(configuredFor);
        current = effect;
    }

    public void Configure(WaveFormat waveFormat)
    {
        format = waveFormat;
        current?.Configure(waveFormat);
    }

    public void Process(Span<float> buffer)
    {
        var effect = current;
        var waveFormat = format;
        if (effect == null || waveFormat == null)
            return;
        var start = Stopwatch.GetTimestamp();
        effect.Process(buffer);
        Interlocked.Add(ref busyTicks, Stopwatch.GetTimestamp() - start);
        Interlocked.Add(ref processedFrames, buffer.Length / waveFormat.Channels);
    }

    public void Reset() => current?.Reset();

    public int LatencySamples => current?.LatencySamples ?? 0;

    /// <summary>
    /// Processing time as a fraction of the audio duration processed since the last call
    /// (0.05 = 5% of one core), or null if nothing was processed.
    /// </summary>
    public double? TakeLoad()
    {
        var ticks = Interlocked.Exchange(ref busyTicks, 0);
        var frames = Interlocked.Exchange(ref processedFrames, 0);
        var waveFormat = format;
        if (frames == 0 || waveFormat == null)
            return null;
        return ticks / (double)Stopwatch.Frequency / (frames / (double)waveFormat.SampleRate);
    }
}
