using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudioConsoleTest.Shared;

/// <summary>
/// Generates a sine wave. Implements ISampleSource (primary) and IAudioSource
/// for direct use with WASAPI playback.
/// </summary>
class SineWaveSource : ISampleSource, IAudioSource
{
    private readonly float frequency;
    private readonly float amplitude;
    private readonly int sampleRate;
    private readonly int channels;
    private double phase;

    public WaveFormat WaveFormat { get; }

    public SineWaveSource(float frequency = 440f, float amplitude = 0.25f,
        int sampleRate = 44100, int channels = 2)
    {
        this.frequency = frequency;
        this.amplitude = amplitude;
        this.sampleRate = sampleRate;
        this.channels = channels;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public int Read(Span<float> buffer)
    {
        double phaseIncrement = 2 * Math.PI * frequency / sampleRate;

        for (int i = 0; i < buffer.Length; i += channels)
        {
            float sample = (float)(amplitude * Math.Sin(phase));
            for (int ch = 0; ch < channels && (i + ch) < buffer.Length; ch++)
            {
                buffer[i + ch] = sample;
            }
            phase += phaseIncrement;
            if (phase > 2 * Math.PI)
                phase -= 2 * Math.PI;
        }

        return buffer.Length;
    }

    public int Read(Span<byte> buffer)
    {
        var floatSpan = MemoryMarshal.Cast<byte, float>(buffer);
        int samplesRead = Read(floatSpan);
        return samplesRead * sizeof(float);
    }
}
