using BenchmarkDotNet.Attributes;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Benchmarks;

/// <summary>
/// End-to-end benchmark of <see cref="MixingSampleProvider.Read"/>. Includes the framework
/// overhead (source iteration, end-of-stream bookkeeping) on top of the mix-add kernel.
/// </summary>
[MemoryDiagnoser]
public class MixingSampleProviderBenchmarks
{
    [Params(2, 8, 32)]
    public int SourceCount;

    [Params(1920, 9600)]
    public int BufferSize;

    private MixingSampleProvider mixer = null!;
    private float[] buffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        var sources = new ISampleProvider[SourceCount];
        var rng = new Random(1337);
        for (int i = 0; i < SourceCount; i++)
        {
            var data = new float[4096];
            for (int k = 0; k < data.Length; k++) data[k] = (float)(rng.NextDouble() * 2 - 1);
            sources[i] = new CyclingSampleProvider(data, 48000, 2);
        }
        mixer = new MixingSampleProvider(sources) { ReadFully = true };
        buffer = new float[BufferSize];
    }

    [Benchmark]
    public int MixRead() => mixer.Read(buffer);

    /// <summary>
    /// Endless sample provider that replays a fixed float buffer. Avoids file I/O so the
    /// benchmark measures the mix kernel, not disk.
    /// </summary>
    private sealed class CyclingSampleProvider : ISampleProvider
    {
        private readonly float[] data;
        private int position;

        public CyclingSampleProvider(float[] data, int sampleRate, int channels)
        {
            this.data = data;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<float> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = data[position];
                position = (position + 1) % data.Length;
            }
            return buffer.Length;
        }
    }
}
