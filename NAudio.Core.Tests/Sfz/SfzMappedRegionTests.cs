using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sfz
{
    [TestFixture]
    [Category("UnitTest")]
    public class SfzNoteNameTests
    {
        [TestCase("60", 60)]
        [TestCase("0", 0)]
        [TestCase("c4", 60)]   // SFZ middle C convention
        [TestCase("C4", 60)]
        [TestCase("a4", 69)]
        [TestCase("c#4", 61)]
        [TestCase("db4", 61)]
        [TestCase("c-1", 0)]
        [TestCase("g9", 127)]
        public void ParsesNumbersAndNoteNames(string text, int expected)
        {
            Assert.That(SfzNoteName.TryParse(text, out var note), Is.True);
            Assert.That(note, Is.EqualTo(expected));
        }

        [TestCase("h2")]   // not a note letter
        [TestCase("c10")]  // out of MIDI range
        [TestCase("")]
        [TestCase("xyz")]
        public void RejectsInvalidValues(string text)
        {
            Assert.That(SfzNoteName.TryParse(text, out _), Is.False);
            Assert.That(SfzNoteName.Parse(text, -1), Is.EqualTo(-1));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class SfzMappedRegionTests
    {
        private static SfzMappedRegion MapFirst(string sfz)
        {
            var instrument = SfzParser.Parse(sfz);
            return SfzMappedRegion.Map(instrument.Regions[0]);
        }

        [Test]
        public void DefaultsWhenNoOpcodes()
        {
            var r = MapFirst("<region> sample=a.wav");
            Assert.That(r.LoKey, Is.EqualTo(0));
            Assert.That(r.HiKey, Is.EqualTo(127));
            Assert.That(r.LoVel, Is.EqualTo(0));
            Assert.That(r.HiVel, Is.EqualTo(127));
            Assert.That(r.PitchKeycenter, Is.EqualTo(60));
            Assert.That(r.PitchKeytrack, Is.EqualTo(100));
            Assert.That(r.AmpegSustain, Is.EqualTo(1f));
            Assert.That(r.HasCutoff, Is.False);
            Assert.That(r.LoopMode, Is.EqualTo(SfzLoopMode.NoLoop));
            Assert.That(r.Trigger, Is.EqualTo(SfzTrigger.Attack));
        }

        [Test]
        public void KeyOpcodeSetsRangeAndCentre()
        {
            var r = MapFirst("<region> sample=a.wav key=c4");
            Assert.That(r.LoKey, Is.EqualTo(60));
            Assert.That(r.HiKey, Is.EqualTo(60));
            Assert.That(r.PitchKeycenter, Is.EqualTo(60));
        }

        [Test]
        public void ExplicitKeyRangeWithNoteNames()
        {
            var r = MapFirst("<region> sample=a.wav lokey=c3 hikey=c5 pitch_keycenter=c4");
            Assert.That(r.LoKey, Is.EqualTo(48));
            Assert.That(r.HiKey, Is.EqualTo(72));
            Assert.That(r.PitchKeycenter, Is.EqualTo(60));
        }

        [Test]
        public void TuneAndTransposeCombineToCents()
        {
            var r = MapFirst("<region> sample=a.wav tune=50 transpose=-2");
            Assert.That(r.TuneCents, Is.EqualTo(-150).Within(1e-9));
        }

        [Test]
        public void PanNormalisesToPlusMinusOne()
        {
            Assert.That(MapFirst("<region> sample=a.wav pan=-100").Pan, Is.EqualTo(-1f));
            Assert.That(MapFirst("<region> sample=a.wav pan=50").Pan, Is.EqualTo(0.5f));
            Assert.That(MapFirst("<region> sample=a.wav").Pan, Is.EqualTo(0f));
        }

        [Test]
        public void AmplitudeEnvelopeInSecondsAndSustainFraction()
        {
            var r = MapFirst("<region> sample=a.wav ampeg_attack=0.01 ampeg_decay=0.2 ampeg_sustain=50 ampeg_release=0.3");
            Assert.That(r.AmpegAttack, Is.EqualTo(0.01f));
            Assert.That(r.AmpegDecay, Is.EqualTo(0.2f));
            Assert.That(r.AmpegSustain, Is.EqualTo(0.5f));
            Assert.That(r.AmpegRelease, Is.EqualTo(0.3f));
        }

        [Test]
        public void FilterOpcodes()
        {
            var r = MapFirst("<region> sample=a.wav cutoff=2000 resonance=6 fil_type=hpf_2p");
            Assert.That(r.HasCutoff, Is.True);
            Assert.That(r.CutoffHz, Is.EqualTo(2000f));
            Assert.That(r.ResonanceDb, Is.EqualTo(6f));
            Assert.That(r.FilterType, Is.EqualTo(SfzFilterType.HighPass));
        }

        [TestCase("one_shot", SfzLoopMode.OneShot)]
        [TestCase("loop_continuous", SfzLoopMode.LoopContinuous)]
        [TestCase("loop_sustain", SfzLoopMode.LoopSustain)]
        [TestCase("no_loop", SfzLoopMode.NoLoop)]
        public void LoopModeMapping(string value, SfzLoopMode expected)
        {
            var r = MapFirst($"<region> sample=a.wav loop_mode={value}");
            Assert.That(r.LoopMode, Is.EqualTo(expected));
        }

        [Test]
        public void GroupAndTriggerRouting()
        {
            var r = MapFirst("<region> sample=a.wav group=1 off_by=1 off_mode=normal trigger=release");
            Assert.That(r.Group, Is.EqualTo(1));
            Assert.That(r.OffBy, Is.EqualTo(1));
            Assert.That(r.OffMode, Is.EqualTo(SfzOffMode.Normal));
            Assert.That(r.Trigger, Is.EqualTo(SfzTrigger.Release));
        }

        [Test]
        public void MatchesKeyAndVelocity()
        {
            var r = MapFirst("<region> sample=a.wav lokey=60 hikey=72 lovel=64 hivel=127");
            Assert.That(r.Matches(64, 100), Is.True);
            Assert.That(r.Matches(48, 100), Is.False);
            Assert.That(r.Matches(64, 32), Is.False);
        }

        [Test]
        public void OctaveOffsetShiftsSpecifiedKeys()
        {
            // direct offset
            var instrument = SfzParser.Parse("<region> sample=a.wav key=c4");
            var shifted = SfzMappedRegion.Map(instrument.Regions[0], noteOffset: 0, octaveOffset: 1);
            Assert.That(shifted.PitchKeycenter, Is.EqualTo(72));

            // via <control> offsets through MapRegions
            var withControl = SfzParser.Parse("<control> octave_offset=1\n<region> sample=a.wav key=c4");
            Assert.That(withControl.MapRegions()[0].PitchKeycenter, Is.EqualTo(72));
        }

        [Test]
        public void KeyswitchOpcodesMap()
        {
            var r = MapFirst("<region> sample=a.wav sw_lokey=c0 sw_hikey=d0 sw_last=c0 sw_default=d0");
            Assert.That(r.KeyswitchLow, Is.EqualTo(12));   // c0 = 12
            Assert.That(r.KeyswitchHigh, Is.EqualTo(14));  // d0 = 14
            Assert.That(r.KeyswitchLast, Is.EqualTo(12));
            Assert.That(r.KeyswitchDefault, Is.EqualTo(14));
        }

        [Test]
        public void RoundRobinAndRandomOpcodesMap()
        {
            var r = MapFirst("<region> sample=a.wav seq_length=3 seq_position=2 lorand=0.25 hirand=0.75");
            Assert.That(r.SequenceLength, Is.EqualTo(3));
            Assert.That(r.SequencePosition, Is.EqualTo(2));
            Assert.That(r.LowRandom, Is.EqualTo(0.25f));
            Assert.That(r.HighRandom, Is.EqualTo(0.75f));
        }

        [Test]
        public void CcGatesAreCollectedPerController()
        {
            var r = MapFirst("<region> sample=a.wav locc1=64 hicc1=127 hicc74=100");
            Assert.That(r.CcGates, Has.Count.EqualTo(2));
            Assert.That(r.CcGates, Has.Member((1, 64, 127)));
            Assert.That(r.CcGates, Has.Member((74, 0, 100))); // missing locc defaults to 0
        }

        [Test]
        public void CrossfadeOpcodesMap()
        {
            var r = MapFirst("<region> sample=a.wav xfin_lovel=20 xfin_hivel=80 xfout_lokey=c4 xfout_hikey=c5 xf_velcurve=gain");
            Assert.That(r.VelocityFadeInLow, Is.EqualTo(20));
            Assert.That(r.VelocityFadeInHigh, Is.EqualTo(80));
            Assert.That(r.KeyFadeOutLow, Is.EqualTo(60));  // c4
            Assert.That(r.KeyFadeOutHigh, Is.EqualTo(72)); // c5
            Assert.That(r.VelocityFadeCurve, Is.EqualTo(SfzCrossfadeCurve.Linear)); // "gain"
            Assert.That(r.KeyFadeCurve, Is.EqualTo(SfzCrossfadeCurve.Power));        // default
        }

        [Test]
        public void RandomDefaultsToFullRange()
        {
            var r = MapFirst("<region> sample=a.wav");
            Assert.That(r.LowRandom, Is.EqualTo(0f));
            Assert.That(r.HighRandom, Is.EqualTo(1f));
            Assert.That(r.SequenceLength, Is.EqualTo(1));
        }
    }
}
