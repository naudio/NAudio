using System;
using System.IO;
using NAudio.Dsp;
using NAudio.Sampler;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
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
        public void HigherKeyPlaysBackFaster()
        {
            // a one-shot: a note an octave above the root consumes the sample
            // twice as fast, so it finishes sooner
            SingleSampleInstrument MakeOneShot() =>
                new SingleSampleInstrument(Constant(0.5f, 4410), SampleRate, rootKey: 60); // ~100ms one-shot

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
        public void WaveSampleLoaderDownmixesStereoToMono()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
            try
            {
                using (var writer = new WaveFileWriter(path, new WaveFormat(SampleRate, 16, 2)))
                {
                    var block = new short[200]; // 100 frames L/R
                    for (int i = 0; i < block.Length; i += 2) { block[i] = 16384; block[i + 1] = 0; }
                    writer.WriteSamples(block, 0, block.Length);
                }

                Assert.That(WaveSampleLoader.TryLoad(path, out var data, out var rate), Is.True);
                Assert.That(rate, Is.EqualTo(SampleRate));
                Assert.That(data.Length, Is.EqualTo(100));
                // average of ~0.5 (left) and 0 (right) ≈ 0.25
                Assert.That(data[0], Is.EqualTo(0.25f).Within(0.01f));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
