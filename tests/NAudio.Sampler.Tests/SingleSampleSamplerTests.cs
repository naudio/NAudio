using System;
using System.IO;
using NAudio.Dsp;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Sampler.Tests;

[TestFixture]
[Category("UnitTest")]
public class SingleSampleSamplerTests
{
    private const int SampleRate = 44100;

    private static float[] Constant(float value, int length)
    {
        var a = new float[length];
        for (int i = 0; i < length; i++) a[i] = value;
        return a;
    }

    private static SingleSampleSampler Looping(out SingleSampleInstrument instrument, float value = 0.5f)
    {
        instrument = new SingleSampleInstrument(Constant(value, 64), SampleRate, rootKey: 60)
        {
            LoopMode = LoopMode.Continuous,
            LoopStart = 0,
            LoopEnd = 64
        };
        return new SingleSampleSampler(instrument, SampleRate);
    }

    private static float Peak(SamplerEngine sampler, int frames)
    {
        var buffer = new float[frames * 2];
        sampler.Read(buffer);
        float peak = 0;
        foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
        return peak;
    }

    [Test]
    public void OneShotEndRampsOutInsteadOfClicking()
    {
        // A one-shot whose End falls on a non-zero sample (e.g. an edited End
        // marker mid-waveform) must fade out over a few ms, not hard-cut — a
        // hard cut steps from the steady level straight to zero in one sample.
        var instrument = new SingleSampleInstrument(Constant(0.5f, 1500), SampleRate, rootKey: 60)
        {
            LoopMode = LoopMode.None,
            Start = 0,
            End = 1000 // truncate before the data ends, on a non-zero sample
        };
        var sampler = new SingleSampleSampler(instrument, SampleRate);
        sampler.NoteOn(0, 60, 127);

        var buffer = new float[2000 * 2];
        sampler.Read(buffer);

        float steady = Math.Abs(buffer[300 * 2]); // left channel, well past the ~1 ms attack
        Assert.That(steady, Is.GreaterThan(0.05f), "expected audible steady level before End");

        // largest single-sample downward step on the left channel: a hard cut
        // would drop by ~steady in one sample; the de-click ramp drops gradually
        float maxDrop = 0f;
        for (int i = 1; i < 2000; i++)
        {
            float step = Math.Abs(buffer[(i - 1) * 2]) - Math.Abs(buffer[i * 2]);
            if (step > maxDrop) maxDrop = step;
        }
        Assert.That(maxDrop, Is.LessThan(steady * 0.5f), "output should ramp out, not cliff-cut at End");
    }

    [Test]
    public void AutoMapsAcrossTheWholeKeyboard()
    {
        var sampler = Looping(out _);
        sampler.NoteOn(0, 24, 100);
        sampler.NoteOn(0, 60, 100);
        sampler.NoteOn(0, 96, 100);
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(3));
        Assert.That(Peak(sampler, 256), Is.GreaterThan(0.1f));
    }

    [Test]
    public void RespectsAnEditedKeyRange()
    {
        var sampler = Looping(out var instrument);
        instrument.LoKey = 48;
        instrument.HiKey = 72;
        sampler.NoteOn(0, 36, 100); // below range
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
        sampler.NoteOn(0, 60, 100); // in range
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
    }

    [Test]
    public void LiveVolumeEditAffectsTheNextNote()
    {
        var sampler = Looping(out var instrument);

        sampler.NoteOn(0, 60, 127);
        float loud = Peak(sampler, 256);

        sampler.NoteOff(0, 60);
        Peak(sampler, 2048); // let the release finish so the voice frees
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

        instrument.VolumeDb = -40f; // ~ -40 dB on the next note
        sampler.NoteOn(0, 60, 127);
        float quiet = Peak(sampler, 256);

        Assert.That(quiet, Is.LessThan(loud * 0.2f));
    }

    [Test]
    public void PositiveVolumeDbBoostsTheNextNote()
    {
        // a +6 dB VolumeDb must raise the peak ~2x — it is carried as linear
        // gain because the voice clamps SF2 attenuation at >= 0
        var sampler = Looping(out var instrument, value: 0.25f);

        sampler.NoteOn(0, 60, 127);
        float baseline = Peak(sampler, 256);
        sampler.NoteOff(0, 60);
        Peak(sampler, 2048); // let the release finish so the voice frees
        Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

        instrument.VolumeDb = 6f;
        sampler.NoteOn(0, 60, 127);
        float boosted = Peak(sampler, 256);

        Assert.That(boosted, Is.EqualTo(baseline * 1.995f).Within(baseline * 0.05f));
    }

    [Test]
    public void HigherKeyPlaysBackFaster()
    {
        // a one-shot: a note an octave above the root consumes the sample
        // twice as fast, so it finishes sooner
        SingleSampleInstrument MakeOneShot() =>
            new(Constant(0.5f, 4410), SampleRate, rootKey: 60); // ~100ms one-shot

        var atRoot = new SingleSampleSampler(MakeOneShot(), SampleRate);
        var octaveUp = new SingleSampleSampler(MakeOneShot(), SampleRate);
        atRoot.NoteOn(0, 60, 127);
        octaveUp.NoteOn(0, 72, 127);

        // render ~60 ms; the octave-up voice (~50 ms) should have ended, the root (~100 ms) not
        int frames = SampleRate * 60 / 1000;
        Peak(atRoot, frames);
        Peak(octaveUp, frames);
        Assert.That(octaveUp.ActiveVoiceCount, Is.EqualTo(0));
        Assert.That(atRoot.ActiveVoiceCount, Is.EqualTo(1));
    }

    [Test]
    public void FromWaveFileLoadsAndPlays()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            using (var writer = new WaveFileWriter(path, new WaveFormat(SampleRate, 16, 1)))
            {
                var block = new short[SampleRate / 10]; // 100 ms
                for (int i = 0; i < block.Length; i++) block[i] = 16384; // ~0.5 full scale
                writer.WriteSamples(block, 0, block.Length);
            }

            var sampler = SingleSampleSampler.FromWaveFile(path, rootKey: 60);
            sampler.Instrument.LoopMode = LoopMode.Continuous;
            sampler.Instrument.LoopEnd = sampler.Instrument.Length;
            sampler.NoteOn(0, 60, 127);
            Assert.That(Peak(sampler, 512), Is.GreaterThan(0.1f));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void WaveSampleLoaderSplitsStereoChannels()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            using (var writer = new WaveFileWriter(path, new WaveFormat(SampleRate, 16, 2)))
            {
                var block = new short[200]; // 100 frames: left ~0.5, right 0
                for (int i = 0; i < block.Length; i += 2) { block[i] = 16384; block[i + 1] = 0; }
                writer.WriteSamples(block, 0, block.Length);
            }

            Assert.That(WaveSampleLoader.TryLoad(path, out var left, out var right, out var rate), Is.True);
            Assert.That(rate, Is.EqualTo(SampleRate));
            Assert.That(left.Length, Is.EqualTo(100));
            Assert.That(right, Is.Not.Null);
            Assert.That(left[0], Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(right[0], Is.EqualTo(0f).Within(0.01f));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
