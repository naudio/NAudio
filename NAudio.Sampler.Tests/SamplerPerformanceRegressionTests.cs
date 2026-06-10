using System;
using NAudio.Sampler;
using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// CI-runnable guards for the sampler's performance contracts: steady-state
    /// note cycling must not allocate, preset resolution is pre-warmed at
    /// construction (not run inside Read on the first note per program), and the
    /// region-lookup index preserves dispatch ordering. These complement the
    /// manual <see cref="SamplerBenchmarks"/> (which measure, but don't gate).
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SamplerPerformanceRegressionTests
    {
        private const int SampleRate = 44100;

        // a looped sine with an engaged filter, both LFOs and a reverb send — a
        // deliberately busy voice so the no-allocation guarantee covers the whole
        // render chain, not just a trivial voice
        private static NAudio.SoundFont.SoundFont BuildBusyFont()
        {
            const int points = 256;
            var data = new byte[points * 2];
            for (int i = 0; i < points; i++)
            {
                short val = (short)(Math.Sin(2 * Math.PI * i / points) * 12000);
                data[i * 2] = (byte)(val & 0xFF);
                data[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(8, 6000),                              // initialFilterFc: filter engaged
                SoundFontTestBuilder.Gen(6, 25),                                // vibLfoToPitch
                SoundFontTestBuilder.Gen(24, unchecked((ushort)(short)-1200)),  // freqVibLfo ~4 Hz
                SoundFontTestBuilder.Gen(10, 1200),                             // modLfoToFilterFc
                SoundFontTestBuilder.Gen(91, 200),                              // reverb send 20%
                SoundFontTestBuilder.Gen(54, 1),                                // loop
                SoundFontTestBuilder.Gen(58, 60),                               // root key
                SoundFontTestBuilder.Gen(53, 0)));                              // sampleID
            return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, points, 0, points, SampleRate, 60);
        }

        [Test]
        public void SteadyStateNoteCyclesDoNotAllocate()
        {
            var sampler = new SoundFontSampler(BuildBusyFont(), SampleRate, maxVoices: 32)
            {
                PercussionChannel = -1
            };
            var buffer = new float[512 * 2];

            void Cycle(int i)
            {
                int ch = i % 16, note = 40 + i % 40;
                sampler.NoteOn(ch, note, 100);
                sampler.Read(buffer);
                sampler.NoteOff(ch, note);
            }

            // warm-up: primes JIT, the voice pool's lazily created readers, the
            // region sample-source caches, the region index and the held-note
            // dictionary capacities (the (channel, note) combos repeat with
            // period 80, so 200 iterations cover them all)
            for (int i = 0; i < 200; i++) Cycle(i);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 200; i < 400; i++) Cycle(i);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero,
                "steady-state note-on/render/note-off cycling must be allocation-free");
        }

        [Test]
        public void PresetResolutionIsPrewarmedAtConstruction()
        {
            // both presets (bank 0 and bank 8, program 0) must be projected by the
            // constructor, so a program/bank change followed by a note-on resolves
            // from the cache instead of doing resolution work on the audio thread
            var data = new byte[8];
            for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
            var sf = SoundFontTestBuilder.BuildTwoBankFont(data, SampleRate, bank8AttenuationCb: 200);
            var sampler = new SoundFontSampler(sf, SampleRate, 8);

            Assert.That(sampler.CachedPresetCount, Is.EqualTo(2), "every preset is cached at construction");

            int cached = sampler.CachedPresetCount;
            sampler.ProcessMidiEvent(new NAudio.Midi.ControlChangeEvent(0, 1, NAudio.Midi.MidiController.BankSelect, 8));
            sampler.NoteOn(0, 60, 127);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(sampler.CachedPresetCount, Is.EqualTo(cached),
                "a note-on for a present (bank, program) must not grow the cache");
        }

        [Test]
        public void PercussionPresetIsPrewarmedUnderTheForcedPercussionKey()
        {
            var data = new byte[8];
            for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(54, 1),
                SoundFontTestBuilder.Gen(58, 60),
                SoundFontTestBuilder.Gen(53, 0)));
            var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60, bank: 128);
            var sampler = new SoundFontSampler(sf, SampleRate, 8);

            int cached = sampler.CachedPresetCount;
            Assert.That(cached, Is.EqualTo(1), "the kit is cached at construction");

            sampler.NoteOn(9, 60, 127); // percussion channel resolves the prewarmed kit
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(sampler.CachedPresetCount, Is.EqualTo(cached),
                "a percussion note-on for a present kit must not grow the cache");
        }

        [Test]
        public void SegmentedRenderIsBitIdenticalToMonolithic()
        {
            // The voice carries its control-rate phase across Mix calls, so
            // rendering the same MIDI scene in odd-sized chunks (event-dense
            // sequenced playback) must be bit-identical to rendering it in large
            // uniform blocks. Events are applied at the same absolute frame in
            // both engines (chunks are clamped to the event boundary).
            SoundFontSampler Make()
            {
                var s = new SoundFontSampler(BuildBusyFont(), SampleRate, 16) { PercussionChannel = -1 };
                s.NoteOn(0, 60, 100);
                s.NoteOn(1, 67, 80); // stays held: keeps the sends active throughout
                return s;
            }
            var segmented = Make();
            var monolithic = Make();

            const int total = SampleRate;          // 1 s
            const int noteOffAt = SampleRate / 2;  // a mid-stream event both engines see at the same frame
            var bufA = new float[total * 2];
            var bufB = new float[total * 2];

            int[] chunks = { 1, 7, 64, 113, 1000 };
            int posA = 0, rotation = 0;
            void RenderSegmentedTo(int target)
            {
                while (posA < target)
                {
                    int frames = Math.Min(chunks[rotation++ % chunks.Length], target - posA);
                    segmented.Read(bufA.AsSpan(posA * 2, frames * 2));
                    posA += frames;
                }
            }
            int posB = 0;
            void RenderMonolithicTo(int target)
            {
                while (posB < target)
                {
                    int frames = Math.Min(4096, target - posB);
                    monolithic.Read(bufB.AsSpan(posB * 2, frames * 2));
                    posB += frames;
                }
            }

            RenderSegmentedTo(noteOffAt);
            RenderMonolithicTo(noteOffAt);
            segmented.NoteOff(0, 60);
            monolithic.NoteOff(0, 60);
            RenderSegmentedTo(total);
            RenderMonolithicTo(total);

            Assert.That(bufA, Is.EqualTo(bufB),
                "segmented and monolithic renders of the same scene must be bit-identical");
        }

        private sealed class ConstantLoader : ISfzSampleLoader
        {
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
                out SampleLoop? embeddedLoop)
            {
                left = new float[64];
                for (int i = 0; i < left.Length; i++) left[i] = 0.5f;
                right = null;
                sampleRate = SampleRate;
                embeddedLoop = null;
                return true;
            }
        }

        [Test]
        public void LayeredRegionsOnTheSameKeyStillStartInListOrder()
        {
            // ordering guard for the key-bucketed region index: with a one-voice
            // pool, the later layer steals the voice the earlier layer just
            // started, so the note that survives is the LAST region in list
            // order. The quiet layer is listed first; the surviving voice must be
            // the loud second layer. If the index ever reordered a bucket, the
            // -40 dB layer would survive instead.
            var sfz =
                "<region> sample=a.wav key=60 loop_mode=loop_continuous volume=-40\n" +
                "<region> sample=a.wav key=60 loop_mode=loop_continuous";
            var sampler = new SfzSampler(SfzParser.Parse(sfz), new ConstantLoader(), SampleRate, maxVoices: 1);

            sampler.NoteOn(0, 60, 127);
            var buffer = new float[4096 * 2];
            sampler.Read(buffer); // past the attack and the steal fade

            float tail = Math.Abs(buffer[buffer.Length - 2]);
            Assert.That(tail, Is.GreaterThan(0.2f),
                "the second (loud) layer must own the voice after in-order dispatch");
        }
    }
}
