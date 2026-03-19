using NAudio.Wasapi;
using NAudio.Wave;

namespace NAudioConsoleTest.Shared;

/// <summary>
/// IAudioSource that generates a sine wave. Zero-copy — writes directly into the Span.
/// </summary>
class SineWaveSource : IAudioSource
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

    public int Read(Span<byte> buffer)
    {
        int samplesRequested = buffer.Length / 4; // 4 bytes per float sample
        var floatSpan = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(buffer);

        double phaseIncrement = 2 * Math.PI * frequency / sampleRate;

        for (int i = 0; i < floatSpan.Length; i += channels)
        {
            float sample = (float)(amplitude * Math.Sin(phase));
            for (int ch = 0; ch < channels && (i + ch) < floatSpan.Length; ch++)
            {
                floatSpan[i + ch] = sample;
            }
            phase += phaseIncrement;
            if (phase > 2 * Math.PI)
                phase -= 2 * Math.PI;
        }

        return buffer.Length;
    }
}
