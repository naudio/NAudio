using System;
using System.Threading.Tasks;
using NAudio.Dsp;
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests the live-MIDI bridge: events sent from other threads are applied to
    /// the sampler on the audio thread (at the start of each Read) and produce sound.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class LiveMidiInstrumentTests
    {
        private const int SampleRate = 44100;

        private static SingleSampleSampler Sampler()
        {
            var data = new float[64];
            for (int i = 0; i < data.Length; i++) data[i] = 0.5f;
            var instrument = new SingleSampleInstrument(data, SampleRate, rootKey: 60)
            {
                LoopMode = LoopMode.Continuous,
                LoopStart = 0,
                LoopEnd = 64
            };
            return new SingleSampleSampler(instrument, SampleRate);
        }

        private static float Peak(ISampleProvider source, int frames)
        {
            var buffer = new float[frames * 2];
            source.Read(buffer);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        [Test]
        public void QueuedNoteIsAppliedOnNextRead()
        {
            var instrument = new LiveMidiInstrument(Sampler());

            // before any Read, nothing has reached the sampler
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(0));

            instrument.NoteOn(0, 60, 100);
            // still queued — not applied until the audio thread pulls
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(0));

            float peak = Peak(instrument, 256);
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(peak, Is.GreaterThan(0.1f));
        }

        [Test]
        public void NoteOffStopsTheVoice()
        {
            var instrument = new LiveMidiInstrument(Sampler());
            instrument.NoteOn(0, 60, 100);
            Peak(instrument, 64);
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(1));

            instrument.NoteOff(0, 60);
            // pull enough for the amplitude envelope's release to complete
            for (int i = 0; i < 2000 && instrument.Sampler.ActiveVoiceCount > 0; i++)
                Peak(instrument, 256);
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(0));
        }

        [Test]
        public void AcceptsRawMidiEvents()
        {
            var instrument = new LiveMidiInstrument(Sampler());
            instrument.Send(new NoteOnEvent(0, 1, 60, 100, 0));
            Peak(instrument, 64);
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void NullEventIsIgnored()
        {
            var instrument = new LiveMidiInstrument(Sampler());
            Assert.DoesNotThrow(() => instrument.Send(null));
            Peak(instrument, 64);
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(0));
        }

        [Test]
        public void ConcurrentSendsAreNotLost()
        {
            var instrument = new LiveMidiInstrument(Sampler());
            // many notes pushed from several threads, mirroring a MIDI callback
            // racing the UI thread; the lock-free queue must lose none of them
            Parallel.For(0, 32, n => instrument.NoteOn(0, 36 + n, 100));
            for (int i = 0; i < 4; i++) Peak(instrument, 256);
            Assert.That(instrument.Sampler.ActiveVoiceCount, Is.EqualTo(32));
        }
    }
}
