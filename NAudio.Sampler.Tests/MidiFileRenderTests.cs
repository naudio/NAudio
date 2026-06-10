using System;
using System.IO;
using NAudio.Dsp;
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for loading a MIDI file onto the sequencing timeline and rendering it
    /// through the sampler offline (MidiFileSequence + OfflineMidiRenderer).
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class MidiFileRenderTests
    {
        private const int SampleRate = 44100;

        // writes a temp .mid and returns its path; caller deletes it
        private static string WriteMidiFile(Action<MidiEventCollection> build, int ppq = 480)
        {
            var col = new MidiEventCollection(0, ppq);
            col.AddTrack();
            build(col);
            col.PrepareForExport(); // sorts and appends the end-of-track marker
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mid");
            MidiFile.Export(path, col);
            return path;
        }

        [Test]
        public void LoadsChannelEventsAtCanonicalTicks()
        {
            // ppq 480: a file tick of 480 (one quarter) rescales to the canonical 960
            string path = WriteMidiFile(col =>
            {
                var t = col[0];
                t.Add(new TempoEvent(500000, 0)); // 120 bpm
                t.Add(new NoteOnEvent(480, 1, 60, 100, 0));
                t.Add(new NoteEvent(960, 1, MidiCommandCode.NoteOff, 60, 0));
            });
            try
            {
                var seq = MidiFileSequence.FromFile(path);
                Assert.That(seq.Timeline.Count, Is.EqualTo(2));      // note on + note off
                Assert.That(seq.Timeline.FirstTick, Is.EqualTo(960)); // 480 * 960/480
                Assert.That(seq.EndTick, Is.EqualTo(1920));
                Assert.That(seq.TempoMap.BpmAtTicks(0), Is.EqualTo(120).Within(1e-6));
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void TempoChangesBuildAMultiSegmentMap()
        {
            string path = WriteMidiFile(col =>
            {
                var t = col[0];
                t.Add(new TempoEvent(500000, 0));    // 120 bpm at tick 0
                t.Add(new TempoEvent(250000, 480));  // 240 bpm at file tick 480 -> canonical 960
                t.Add(new NoteOnEvent(0, 1, 60, 100, 0));
                t.Add(new NoteEvent(480, 1, MidiCommandCode.NoteOff, 60, 0));
            });
            try
            {
                var seq = MidiFileSequence.FromFile(path);
                Assert.That(seq.TempoMap.BpmAtTicks(0), Is.EqualTo(120).Within(1e-6));
                Assert.That(seq.TempoMap.BpmAtTicks(960), Is.EqualTo(240).Within(1e-6));
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void RendersTheNoteAtTheRightTime()
        {
            // 120 bpm: canonical 960 ticks = one quarter = 0.5 s.
            // note on at file tick 480 (0.5 s), off at file tick 1440 (1.5 s)
            string path = WriteMidiFile(col =>
            {
                var t = col[0];
                t.Add(new TempoEvent(500000, 0));
                t.Add(new NoteOnEvent(480, 1, 60, 110, 0));
                t.Add(new NoteEvent(1440, 1, MidiCommandCode.NoteOff, 60, 0));
            });
            try
            {
                var seq = MidiFileSequence.FromFile(path);
                var instrument = new SingleSampleInstrument(Constant(0.5f, 64), SampleRate, rootKey: 60)
                {
                    LoopMode = LoopMode.Continuous,
                    LoopEnd = 64
                };
                var sampler = new SingleSampleSampler(instrument, SampleRate);

                float[] audio = OfflineMidiRenderer.Render(seq, sampler, tailSeconds: 0.5);

                // before 0.5 s (frame 22050) -> silence; during the note (0.6-1.4 s) -> sound
                Assert.That(Energy(audio, 0, 21000), Is.LessThan(1e-3f), "silent before the note");
                Assert.That(Energy(audio, 27000, 60000), Is.GreaterThan(1f), "sounding during the note");
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void RenderToWaveFileWritesAFloatWav()
        {
            string midi = WriteMidiFile(col =>
            {
                var t = col[0];
                t.Add(new TempoEvent(500000, 0));
                t.Add(new NoteOnEvent(0, 1, 60, 100, 0));
                t.Add(new NoteEvent(480, 1, MidiCommandCode.NoteOff, 60, 0));
            });
            string wav = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
            try
            {
                var seq = MidiFileSequence.FromFile(midi);
                var instrument = new SingleSampleInstrument(Constant(0.5f, 64), SampleRate, rootKey: 60)
                {
                    LoopMode = LoopMode.Continuous,
                    LoopEnd = 64
                };
                OfflineMidiRenderer.RenderToWaveFile(seq, new SingleSampleSampler(instrument, SampleRate),
                    wav, tailSeconds: 0.2);

                using var reader = new NAudio.Wave.WaveFileReader(wav);
                Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(SampleRate));
                Assert.That(reader.TotalTime.TotalSeconds, Is.GreaterThan(0.2));
            }
            finally
            {
                File.Delete(midi);
                if (File.Exists(wav)) File.Delete(wav);
            }
        }

        [Test]
        public void SteadyStateSequencedPlaybackReadDoesNotAllocate()
        {
            // The whole sequenced-playback hot path end-to-end: a one-bar MIDI pattern looped by
            // the transport, so every SequencedMidiPlayer.Read queries the timeline (through the
            // lock-free snapshot), dispatches note events and renders the sampler. After warm-up
            // (JIT, voice pool, pending-list capacity) it must allocate nothing per Read.
            string path = WriteMidiFile(col =>
            {
                var t = col[0];
                t.Add(new TempoEvent(500000, 0)); // 120 bpm
                for (int i = 0; i < 8; i++)       // eighth notes filling the bar (ppq 480)
                {
                    t.Add(new NoteOnEvent(i * 240, 1, 60 + i % 4, 100, 0));
                    t.Add(new NoteEvent(i * 240 + 120, 1, MidiCommandCode.NoteOff, 60 + i % 4, 0));
                }
            });
            try
            {
                var seq = MidiFileSequence.FromFile(path);
                var instrument = new SingleSampleInstrument(Constant(0.5f, 64), SampleRate, rootKey: 60)
                {
                    LoopMode = LoopMode.Continuous,
                    LoopEnd = 64
                };
                var sampler = new SingleSampleSampler(instrument, SampleRate);
                var transport = new Transport(seq.TempoMap, SampleRate)
                {
                    Loop = new LoopRegion(0, MusicalTime.CanonicalPpq * 4L) // loop the bar forever
                };
                var player = new SequencedMidiPlayer(transport, seq.Timeline, sampler);
                transport.Play();
                var buffer = new float[1024 * 2];

                for (int i = 0; i < 400; i++) player.Read(buffer); // warm-up: ~4.6 loop iterations

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 400; i++) player.Read(buffer);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

                Assert.That(allocated, Is.Zero, "steady-state sequenced playback Read must be allocation-free");
            }
            finally { File.Delete(path); }
        }

        private static float[] Constant(float value, int length)
        {
            var a = new float[length];
            for (int i = 0; i < length; i++) a[i] = value;
            return a;
        }

        private static float Energy(float[] interleavedStereo, int frameStart, int frameEnd)
        {
            float e = 0;
            int end = Math.Min(frameEnd, interleavedStereo.Length / 2);
            for (int f = frameStart; f < end; f++)
                e += Math.Abs(interleavedStereo[f * 2]) + Math.Abs(interleavedStereo[f * 2 + 1]);
            return e;
        }
    }
}
