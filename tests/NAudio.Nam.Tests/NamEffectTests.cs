using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Nam.Tests;

[TestFixture]
[Category("UnitTest")]
public class NamEffectTests
{
    [Test]
    public void MonoEffectMatchesProcessor()
    {
        var model = TestData.LoadA2();
        var input = TestData.Input;
        var expected = TestData.Render(model.CreateProcessor(), input, 64);

        var effect = new NamEffect(model);
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
        var buffer = (float[])input.Clone();
        for (var offset = 0; offset < buffer.Length; offset += 480)
            effect.Process(buffer.AsSpan(offset, Math.Min(480, buffer.Length - offset)));

        Assert.That(buffer, Is.EqualTo(expected));
        Assert.That(effect.LatencySamples, Is.Zero);
    }

    [Test]
    public void StereoEffectProcessesTheChannelAverageIntoBothChannels()
    {
        var model = TestData.LoadA2();
        var input = TestData.Input;
        var mono = new float[input.Length];
        var stereo = new float[input.Length * 2];
        for (var i = 0; i < input.Length; i++)
        {
            stereo[2 * i] = input[i];
            stereo[2 * i + 1] = input[i] * 0.5f;
            mono[i] = input[i] * 0.75f;
        }
        var expected = TestData.Render(model.CreateProcessor(0.0), mono, 64);

        var effect = new NamEffect(model, size: 0.0);
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
        for (var offset = 0; offset < stereo.Length; offset += 1024)
            effect.Process(stereo.AsSpan(offset, Math.Min(1024, stereo.Length - offset)));

        for (var i = 0; i < input.Length; i++)
        {
            Assert.That(stereo[2 * i], Is.EqualTo(expected[i]).Within(1e-6f), $"left {i}");
            Assert.That(stereo[2 * i + 1], Is.EqualTo(stereo[2 * i]), $"right {i}");
        }
    }

    [Test]
    public void ResetRestartsFromSilence()
    {
        var model = TestData.LoadA2();
        var input = TestData.Input;
        var effect = new NamEffect(model);
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
        var first = (float[])input.Clone();
        effect.Process(first);

        effect.Reset();
        var second = (float[])input.Clone();
        effect.Process(second);

        Assert.That(TestData.MaxAbsDifference(first, second), Is.LessThan(1e-6f));
    }

    [Test]
    public void WorksInAnEffectChain()
    {
        var chain = new EffectChain(new SignalGeneratorSource())
            .Add(new NamEffect(TestData.LoadA2()))
            .Add(new GainEffect { GainDb = -6f });

        var buffer = new float[4800];
        var read = chain.Read(buffer);

        Assert.That(read, Is.EqualTo(buffer.Length));
        Assert.That(buffer, Has.Some.Not.EqualTo(0f));
    }

    [Test]
    public void ProcessingDoesNotAllocate()
    {
        var effect = new NamEffect(TestData.LoadA2());
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
        var buffer = new float[1024];
        effect.Process(buffer);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
            effect.Process(buffer);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private sealed class SignalGeneratorSource : ISampleProvider
    {
        private int phase;

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        public int Read(Span<float> buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = 0.3f * MathF.Sin(2 * MathF.PI * 110 * phase++ / 48000f);
            return buffer.Length;
        }
    }
}
