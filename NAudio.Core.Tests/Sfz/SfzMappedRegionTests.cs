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

        [TestCase("fast", SfzOffMode.Fast)]
        [TestCase("normal", SfzOffMode.Normal)]
        [TestCase("time", SfzOffMode.Fast)] // ARIA off_mode=time is unsupported and treated as fast
        public void OffModeMapping(string value, SfzOffMode expected)
        {
            var r = MapFirst($"<region> sample=a.wav off_mode={value}");
            Assert.That(r.OffMode, Is.EqualTo(expected));
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
        public void OctaveOffsetShiftsSpecifiedKeysDown()
        {
            // note_offset/octave_offset transpose *incoming MIDI notes* upward
            // (octave_offset=1: played key 48 behaves as 60, the instrument
            // sounds an octave higher). The mapping-side equivalent is shifting
            // the specified key opcodes DOWN by the offset — a previous version
            // of this test wrongly expected them shifted up.
            var instrument = SfzParser.Parse("<region> sample=a.wav key=c4");
            var shifted = SfzMappedRegion.Map(instrument.Regions[0], noteOffset: 0, octaveOffset: 1);
            Assert.That(shifted.PitchKeycenter, Is.EqualTo(48));
            Assert.That(shifted.LoKey, Is.EqualTo(48));
            Assert.That(shifted.HiKey, Is.EqualTo(48));

            // via <control> offsets through MapRegions
            var withControl = SfzParser.Parse("<control> octave_offset=1\n<region> sample=a.wav key=c4");
            Assert.That(withControl.MapRegions()[0].PitchKeycenter, Is.EqualTo(48));
        }

        [Test]
        public void NoteOffsetShiftsKeyValuedOpcodesDown()
        {
            // same incoming-note-transposition semantic as octave_offset: every
            // key-valued opcode (range, keyswitches, key crossfades) moves down
            var instrument = SfzParser.Parse(
                "<region> sample=a.wav lokey=c4 hikey=c5 pitch_keycenter=c4 sw_last=c2 xfout_lokey=c4 xfout_hikey=c5");
            var r = SfzMappedRegion.Map(instrument.Regions[0], noteOffset: 2);
            Assert.That(r.LoKey, Is.EqualTo(58));
            Assert.That(r.HiKey, Is.EqualTo(70));
            Assert.That(r.PitchKeycenter, Is.EqualTo(58));
            Assert.That(r.KeyswitchLast, Is.EqualTo(34)); // c2 = 36
            Assert.That(r.KeyFadeOutLow, Is.EqualTo(58));
            Assert.That(r.KeyFadeOutHigh, Is.EqualTo(70));
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
        public void SampleBoundsOpcodePresenceIsTracked()
        {
            // absent end/loop opcodes must be distinguishable from explicit
            // values: an absent loop_mode defaults from the sample file's
            // embedded loop, and end=-1 explicitly disables the region
            var absent = MapFirst("<region> sample=a.wav");
            Assert.That(absent.HasEnd, Is.False);
            Assert.That(absent.HasLoopMode, Is.False);
            Assert.That(absent.HasLoopStart, Is.False);
            Assert.That(absent.HasLoopEnd, Is.False);

            var set = MapFirst("<region> sample=a.wav end=-1 loop_mode=no_loop loop_start=2 loop_end=5");
            Assert.That(set.HasEnd, Is.True);
            Assert.That(set.End, Is.EqualTo(-1));
            Assert.That(set.HasLoopMode, Is.True);
            Assert.That(set.LoopMode, Is.EqualTo(SfzLoopMode.NoLoop));
            Assert.That(set.HasLoopStart, Is.True);
            Assert.That(set.LoopStart, Is.EqualTo(2));
            Assert.That(set.HasLoopEnd, Is.True);
            Assert.That(set.LoopEnd, Is.EqualTo(5));
        }

        [Test]
        public void RandomDefaultsToFullRange()
        {
            var r = MapFirst("<region> sample=a.wav");
            Assert.That(r.LowRandom, Is.EqualTo(0f));
            Assert.That(r.HighRandom, Is.EqualTo(1f));
            Assert.That(r.SequenceLength, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseDecayAndEffectSendsMap()
        {
            var r = MapFirst("<region> sample=a.wav rt_decay=18 effect1=50 effect2=25");
            Assert.That(r.ReleaseDecayDbPerSecond, Is.EqualTo(18f));
            Assert.That(r.Effect1Percent, Is.EqualTo(50f));
            Assert.That(r.Effect2Percent, Is.EqualTo(25f));

            var defaults = MapFirst("<region> sample=a.wav");
            Assert.That(defaults.ReleaseDecayDbPerSecond, Is.EqualTo(0f));
            Assert.That(defaults.Effect1Percent, Is.EqualTo(0f));
            Assert.That(defaults.Effect2Percent, Is.EqualTo(0f));
        }

        [Test]
        public void OnCcTriggersAreCollectedPerController()
        {
            var r = MapFirst("<region> sample=a.wav on_locc20=64 on_hicc20=127 on_hicc64=100");
            Assert.That(r.OnCcTriggers, Has.Count.EqualTo(2));
            Assert.That(r.OnCcTriggers, Has.Member((20, 64, 127)));
            Assert.That(r.OnCcTriggers, Has.Member((64, 0, 100))); // missing on_locc defaults to 0

            Assert.That(MapFirst("<region> sample=a.wav").OnCcTriggers, Is.Null);
        }

        [Test]
        public void EqBandsCollectSpecifiedBandsWithDefaults()
        {
            var r = MapFirst("<region> sample=a.wav eq1_freq=800 eq1_gain=6 eq2_gain=-3 eq3_bw=2");
            Assert.That(r.EqBands, Has.Count.EqualTo(3));
            Assert.That(r.EqBands[0].FrequencyHz, Is.EqualTo(800f));
            Assert.That(r.EqBands[0].BandwidthOctaves, Is.EqualTo(1f)); // default bandwidth
            Assert.That(r.EqBands[0].GainDb, Is.EqualTo(6f));
            Assert.That(r.EqBands[1].FrequencyHz, Is.EqualTo(500f));    // band-2 default centre
            Assert.That(r.EqBands[1].GainDb, Is.EqualTo(-3f));
            Assert.That(r.EqBands[2].FrequencyHz, Is.EqualTo(5000f));   // band-3 default centre
            Assert.That(r.EqBands[2].BandwidthOctaves, Is.EqualTo(2f));
            Assert.That(r.EqBands[2].GainDb, Is.EqualTo(0f), "no gain opcode -> a flat band");

            Assert.That(MapFirst("<region> sample=a.wav").EqBands, Is.Null);
        }

        [Test]
        public void LfoOpcodesMapToTypedSettings()
        {
            var r = MapFirst("<region> sample=a.wav amplfo_freq=4 amplfo_depth=6 amplfo_delay=0.5 pitchlfo_freq=5 pitchlfo_depth=50");
            Assert.That(r.AmpLfo.FrequencyHz, Is.EqualTo(4f));
            Assert.That(r.AmpLfo.Depth, Is.EqualTo(6f));
            Assert.That(r.AmpLfo.DelaySeconds, Is.EqualTo(0.5f));
            Assert.That(r.AmpLfo.IsActive, Is.True);
            Assert.That(r.PitchLfo.FrequencyHz, Is.EqualTo(5f));
            Assert.That(r.PitchLfo.Depth, Is.EqualTo(50f));
            Assert.That(r.PitchLfo.DelaySeconds, Is.EqualTo(0f));
            Assert.That(r.PitchLfo.IsActive, Is.True);
            Assert.That(r.FilterLfo.IsActive, Is.False, "no fillfo opcodes -> inactive");

            // a depth with no rate modulates nothing (and vice versa)
            var partial = MapFirst("<region> sample=a.wav fillfo_depth=1200");
            Assert.That(partial.FilterLfo.Depth, Is.EqualTo(1200f));
            Assert.That(partial.FilterLfo.IsActive, Is.False);
        }

        [Test]
        public void VelocityCurvePointsAreCollectedSortedAndValidated()
        {
            var r = MapFirst("<region> sample=a.wav amp_velcurve_64=0.7 amp_velcurve_32=0.4");
            Assert.That(r.VelocityCurvePoints, Is.EqualTo(new[] { (32, 0.4f), (64, 0.7f) }));

            Assert.That(MapFirst("<region> sample=a.wav").VelocityCurvePoints, Is.Null);

            // N outside 1..127, a malformed N and a malformed value are all ignored
            var malformed = MapFirst(
                "<region> sample=a.wav amp_velcurve_0=0.5 amp_velcurve_128=0.5 amp_velcurve_abc=0.5 amp_velcurve_64=xyz");
            Assert.That(malformed.VelocityCurvePoints, Is.Null);

            // levels are clamped to the spec's 0..1
            var clamped = MapFirst("<region> sample=a.wav amp_velcurve_64=1.5 amp_velcurve_32=-0.5");
            Assert.That(clamped.VelocityCurvePoints, Is.EqualTo(new[] { (32, 0f), (64, 1f) }));
        }

        [Test]
        public void GroupLevelVelocityCurvePointsMergeIntoTheRegion()
        {
            // indexed opcodes inherit through <group> -> <region> merging like any
            // other opcode key
            var instrument = SfzParser.Parse("<group> amp_velcurve_31=0.5\n<region> sample=a.wav");
            var r = SfzMappedRegion.Map(instrument.Regions[0]);
            Assert.That(r.VelocityCurvePoints, Is.EqualTo(new[] { (31, 0.5f) }));
        }

        [Test]
        public void BuildVelocityCurveInterpolatesBetweenDefinedPoints()
        {
            var curve = MapFirst("<region> sample=a.wav amp_velcurve_32=0.5 amp_velcurve_96=0.6").BuildVelocityCurve();
            Assert.That(curve, Has.Length.EqualTo(128));
            Assert.That(curve[32], Is.EqualTo(0.5f));
            Assert.That(curve[96], Is.EqualTo(0.6f));
            Assert.That(curve[64], Is.EqualTo(0.55f).Within(1e-3f), "midway between the defined points");
            Assert.That(curve[0], Is.EqualTo(0f), "below the lowest point interpolates from (0, 0)");
            Assert.That(curve[16], Is.EqualTo(0.25f).Within(1e-3f));
            Assert.That(curve[127], Is.EqualTo(1f), "above the highest point interpolates to (127, 1)");
            Assert.That(curve[111], Is.EqualTo(0.7935f).Within(1e-3f), "midway up the (96, 0.6) -> (127, 1) tail");
        }

        [Test]
        public void BuildVelocityCurveHonoursADefined127AndReturnsNullWithoutPoints()
        {
            Assert.That(MapFirst("<region> sample=a.wav").BuildVelocityCurve(), Is.Null);

            var capped = MapFirst("<region> sample=a.wav amp_velcurve_127=0.5").BuildVelocityCurve();
            Assert.That(capped[127], Is.EqualTo(0.5f), "a defined 127 overrides the implicit (127, 1)");
            Assert.That(capped[0], Is.EqualTo(0f));
            Assert.That(capped[64], Is.EqualTo(0.5f * 64 / 127).Within(1e-3f), "rises linearly from (0, 0)");
        }

        [Test]
        public void ModulationEnvelopeOpcodesMapToTypedSettings()
        {
            var r = MapFirst("<region> sample=a.wav fileg_depth=1200 fileg_attack=0.1 fileg_sustain=50 pitcheg_depth=200");
            Assert.That(r.FilterEg.DepthCents, Is.EqualTo(1200f));
            Assert.That(r.FilterEg.AttackSeconds, Is.EqualTo(0.1f));
            Assert.That(r.FilterEg.SustainPercent, Is.EqualTo(50f));
            Assert.That(r.FilterEg.IsActive, Is.True);
            Assert.That(r.PitchEg.DepthCents, Is.EqualTo(200f));
            Assert.That(r.PitchEg.SustainPercent, Is.EqualTo(100f), "sustain defaults to 100%");
            Assert.That(r.PitchEg.IsActive, Is.True);

            var depthless = MapFirst("<region> sample=a.wav fileg_attack=0.2");
            Assert.That(depthless.FilterEg.AttackSeconds, Is.EqualTo(0.2f));
            Assert.That(depthless.FilterEg.IsActive, Is.False, "an EG without depth modulates nothing");
        }
    }
}
