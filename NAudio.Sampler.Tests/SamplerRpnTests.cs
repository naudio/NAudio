using System;
using NAudio.Midi;
using NAudio.Sampler;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for the per-channel RPN data-entry decode (MIDI 1.0; RPN 0 =
    /// pitch-bend sensitivity): CC101/CC100 select the parameter, CC6/CC38 write
    /// semitones + cents to it, RPN null (127,127) and an NRPN selection
    /// deselect it, and Reset All Controllers leaves the bend range alone
    /// (RP-015).
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SamplerRpnTests
    {
        private const int SampleRate = 44100;

        private static SingleSampleSampler NewEngine(int sampleLength = 64)
        {
            var data = new float[sampleLength];
            for (int i = 0; i < data.Length; i++) data[i] = 0.5f;
            return new SingleSampleSampler(
                new SingleSampleInstrument(data, SampleRate, rootKey: 60), SampleRate);
        }

        private static void Cc(SamplerEngine engine, int cc, int value) =>
            engine.ProcessMidiEvent(new ControlChangeEvent(0, 1, (MidiController)cc, value));

        private static double BendRange(SamplerEngine engine) =>
            engine.Channels[0].PitchBendRangeSemitones;

        [Test]
        public void Rpn0DataEntrySetsThePitchBendRange()
        {
            var engine = NewEngine();
            Cc(engine, 101, 0); // RPN MSB
            Cc(engine, 100, 0); // RPN LSB -> RPN (0,0) = pitch-bend range
            Cc(engine, 6, 12);  // Data Entry MSB: 12 semitones
            Assert.That(BendRange(engine), Is.EqualTo(12.0));
        }

        [Test]
        public void DataEntryLsbAddsCents()
        {
            var engine = NewEngine();
            Cc(engine, 101, 0);
            Cc(engine, 100, 0);
            Cc(engine, 6, 12);
            Cc(engine, 38, 50); // Data Entry LSB: +50 cents
            Assert.That(BendRange(engine), Is.EqualTo(12.5));
        }

        [Test]
        public void ANewDataEntryMsbResetsTheCents()
        {
            var engine = NewEngine();
            Cc(engine, 101, 0);
            Cc(engine, 100, 0);
            Cc(engine, 6, 12);
            Cc(engine, 38, 50); // 12.5
            Cc(engine, 6, 4);   // a fresh MSB clears the cents part
            Assert.That(BendRange(engine), Is.EqualTo(4.0));
        }

        [Test]
        public void DataEntryWithoutAnRpnSelectionIsIgnored()
        {
            var engine = NewEngine();
            Cc(engine, 6, 12); // nothing selected: must not touch the bend range
            Cc(engine, 38, 50);
            Assert.That(BendRange(engine), Is.EqualTo(2.0));
        }

        [Test]
        public void RpnNullBlocksSubsequentDataEntry()
        {
            var engine = NewEngine();
            Cc(engine, 101, 0);
            Cc(engine, 100, 0);
            Cc(engine, 6, 12);
            Cc(engine, 101, 127); // RPN null (127,127) deselects
            Cc(engine, 100, 127);
            Cc(engine, 6, 5);
            Assert.That(BendRange(engine), Is.EqualTo(12.0),
                "data entry after RPN null must be ignored");
        }

        [Test]
        public void AnNrpnSelectionBlocksDataEntryFromTheBendRange()
        {
            var engine = NewEngine();
            Cc(engine, 101, 0); // RPN 0 selected...
            Cc(engine, 100, 0);
            Cc(engine, 99, 1);  // ...then an NRPN selected (CC99/98)
            Cc(engine, 98, 2);
            Cc(engine, 6, 5);   // belongs to the NRPN, not the bend range
            Assert.That(BendRange(engine), Is.EqualTo(2.0));

            Cc(engine, 101, 0); // reselecting the RPN re-arms data entry
            Cc(engine, 100, 0);
            Cc(engine, 6, 5);
            Assert.That(BendRange(engine), Is.EqualTo(5.0));
        }

        [Test]
        public void ResetAllControllersKeepsTheBendRange()
        {
            // RP-015: Reset All Controllers must not reset pitch-bend sensitivity
            var engine = NewEngine();
            Cc(engine, 101, 0);
            Cc(engine, 100, 0);
            Cc(engine, 6, 12);
            Cc(engine, 121, 0); // Reset All Controllers
            Assert.That(BendRange(engine), Is.EqualTo(12.0));
        }

        [Test]
        public void BendDepthFollowsTheRpnRangeAudibly()
        {
            // End-to-end through the render path: a full upward bend doubles the
            // playback rate with a 12-semitone range, so a one-shot finishes in
            // about half the frames it takes at the default +/-2 range.
            int FramesUntilSilent(bool widenRangeTo12)
            {
                var engine = NewEngine(sampleLength: 4410); // ~100 ms one-shot
                if (widenRangeTo12)
                {
                    Cc(engine, 101, 0);
                    Cc(engine, 100, 0);
                    Cc(engine, 6, 12);
                }
                engine.ProcessMidiEvent(new PitchWheelChangeEvent(0, 1, 16383)); // full bend up
                engine.NoteOn(0, 60, 127);
                int frames = 0;
                var buf = new float[2];
                while (engine.ActiveVoiceCount > 0 && frames < 10000)
                {
                    engine.Read(buf);
                    frames++;
                }
                return frames;
            }

            int wide = FramesUntilSilent(true);
            int narrow = FramesUntilSilent(false);
            Assert.That(wide, Is.LessThan((int)(narrow * 0.75)),
                "a 12-semitone bend range must bend further (faster playback) than the default +/-2");
        }
    }
}
