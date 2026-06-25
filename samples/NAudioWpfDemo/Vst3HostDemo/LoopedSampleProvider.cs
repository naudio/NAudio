using System;
using NAudio.Wave;

namespace NAudioWpfDemo.Vst3HostDemo;

/// <summary>
/// Loops a fully-buffered sample forever, padding each repetition with trailing silence so the
/// loop period is at least <c>minLoopPeriod</c>. This stops a short one-shot (e.g. a ~150 ms
/// drum hit) from "machine-gunning" when looped — it fires once per period with silence in
/// between, which also lets an effect's tail (reverb / delay) ring out between hits.
/// </summary>
class LoopedSampleProvider : ISampleProvider
{
    private readonly float[] sample;
    private readonly int loopLengthSamples;
    private int position;

    public LoopedSampleProvider(float[] sample, WaveFormat waveFormat, TimeSpan minLoopPeriod)
    {
        this.sample = sample;
        WaveFormat = waveFormat;
        var minSamples = (int)(minLoopPeriod.TotalSeconds * waveFormat.SampleRate) * waveFormat.Channels;
        loopLengthSamples = Math.Max(sample.Length, minSamples);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = position < sample.Length ? sample[position] : 0f;
            if (++position >= loopLengthSamples)
            {
                position = 0;
            }
        }
        return buffer.Length; // never-ending — the host stops it by disposing the player
    }
}
