using System.IO;
using System.Linq;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Core.Tests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class SoundFontInstrumentResolverTests
    {
        [Test]
        public void DefaultsAreSoundFontSpecValues()
        {
            var gens = SoundFontGenerators.CreateWithDefaults();
            Assert.That(gens[GeneratorEnum.InitialFilterCutoffFrequency], Is.EqualTo(13500));
            Assert.That(gens[GeneratorEnum.DelayVolumeEnvelope], Is.EqualTo(-12000));
            Assert.That(gens[GeneratorEnum.AttackVolumeEnvelope], Is.EqualTo(-12000));
            Assert.That(gens[GeneratorEnum.ScaleTuning], Is.EqualTo(100));
            Assert.That(gens[GeneratorEnum.OverridingRootKey], Is.EqualTo(-1));
            Assert.That(gens[GeneratorEnum.KeyNumber], Is.EqualTo(-1));
            Assert.That(gens[GeneratorEnum.Velocity], Is.EqualTo(-1));
            // unspecified generator defaults to zero
            Assert.That(gens[GeneratorEnum.Pan], Is.EqualTo(0));
        }

        [Test]
        public void ZeroedSetHasAllZeroValues()
        {
            var gens = SoundFontGenerators.CreateZeroed();
            Assert.That(gens[GeneratorEnum.InitialFilterCutoffFrequency], Is.EqualTo(0));
            Assert.That(gens[GeneratorEnum.AttackVolumeEnvelope], Is.EqualTo(0));
        }

        [Test]
        public void GeneratorsIndexerRoundTrips()
        {
            var gens = SoundFontGenerators.CreateZeroed();
            gens[GeneratorEnum.Pan] = -250;
            Assert.That(gens[GeneratorEnum.Pan], Is.EqualTo(-250));
        }

        [Test]
        public void RichSoundFontResolvesToTwoRegions()
        {
            var sf = LoadRich();

            var pianoRegions = sf.Presets.Single(p => p.Name == "Piano").ResolveRegions();
            Assert.That(pianoRegions.Count, Is.EqualTo(1));
            Assert.That(pianoRegions[0].Sample.SampleName, Is.EqualTo("PianoSample"));
            Assert.That(pianoRegions[0].LowKey, Is.EqualTo(0));
            Assert.That(pianoRegions[0].HighKey, Is.EqualTo(127));

            var stringRegions = sf.Presets.Single(p => p.Name == "Strings").ResolveRegions();
            Assert.That(stringRegions.Count, Is.EqualTo(1));
            Assert.That(stringRegions[0].Sample.SampleName, Is.EqualTo("StringSample"));
        }

        [Test]
        public void ResolvedRegionStartsFromDefaults()
        {
            var sf = LoadRich();
            var region = sf.Presets.Single(p => p.Name == "Piano").ResolveRegions()[0];
            // no envelope generators set in the test font, so defaults survive
            Assert.That(region.Generators[GeneratorEnum.InitialFilterCutoffFrequency], Is.EqualTo(13500));
            Assert.That(region.Generators[GeneratorEnum.AttackVolumeEnvelope], Is.EqualTo(-12000));
        }

        [Test]
        public void MatchesRespectsKeyAndVelocity()
        {
            var sf = LoadRich();
            var region = sf.Presets.Single(p => p.Name == "Piano").ResolveRegions()[0];
            Assert.That(region.Matches(60, 100), Is.True);
            Assert.That(region.Matches(60, 0), Is.True);   // full velocity range by default
            Assert.That(region.Matches(128, 100), Is.False); // out of key range
        }

        [Test]
        public void InstrumentGeneratorIsAbsoluteOverDefault()
        {
            // instrument zone sets Pan to a specific value; result should equal it
            var sf = LoadCustom(instrumentPan: 250, presetPan: 0);
            var region = sf.Presets[0].ResolveRegions()[0];
            Assert.That(region.Generators[GeneratorEnum.Pan], Is.EqualTo(250));
        }

        [Test]
        public void PresetGeneratorIsAdditiveOverInstrument()
        {
            // instrument Pan = 100, preset Pan offset = 150 -> 250
            var sf = LoadCustom(instrumentPan: 100, presetPan: 150);
            var region = sf.Presets[0].ResolveRegions()[0];
            Assert.That(region.Generators[GeneratorEnum.Pan], Is.EqualTo(250));
        }

        [Test]
        public void AddressOffsetsCombineCoarseAndFine()
        {
            var gens = SoundFontGenerators.CreateWithDefaults();
            gens[GeneratorEnum.StartAddressOffset] = 100;
            gens[GeneratorEnum.StartAddressCoarseOffset] = 2; // 2 * 32768
            Assert.That(gens.StartAddressOffset, Is.EqualTo(100 + 2 * 32768));
        }

        [Test]
        public void SampleModesReadsLowTwoBits()
        {
            var gens = SoundFontGenerators.CreateZeroed();
            gens[GeneratorEnum.SampleModes] = 3;
            Assert.That(gens.SampleModes, Is.EqualTo(SampleMode.LoopAndContinue));
            gens[GeneratorEnum.SampleModes] = 1;
            Assert.That(gens.SampleModes, Is.EqualTo(SampleMode.LoopContinuously));
        }

        private static NAudio.SoundFont.SoundFont LoadRich()
        {
            return new NAudio.SoundFont.SoundFont(
                new MemoryStream(SoundFontTestHelper.BuildRichSoundFont()));
        }

        /// <summary>
        /// Builds a single-preset/single-instrument/single-sample SF2 with a Pan
        /// generator at the instrument level and an additive Pan at the preset
        /// level, to exercise the absolute-vs-additive accumulation rule.
        /// </summary>
        private static NAudio.SoundFont.SoundFont LoadCustom(short instrumentPan, short presetPan)
        {
            const ushort PanGen = 17;
            const ushort InstrumentGen = 41;
            const ushort SampleIdGen = 53;

            var phdr = SoundFontTestHelper.Chunk("phdr", SoundFontTestHelper.Concat(
                SoundFontTestHelper.PresetHeaderRecord("P", 0, 0, 0),
                SoundFontTestHelper.PresetHeaderRecord("EOP", 0, 0, 1)));
            var pbag = SoundFontTestHelper.Chunk("pbag", SoundFontTestHelper.Concat(
                SoundFontTestHelper.BagRecord(0, 0),
                SoundFontTestHelper.BagRecord(2, 0)));
            var pmod = SoundFontTestHelper.Chunk("pmod", SoundFontTestHelper.ModulatorRecord());
            // preset zone: Pan offset then Instrument index
            var pgen = SoundFontTestHelper.Chunk("pgen", SoundFontTestHelper.Concat(
                SoundFontTestHelper.GeneratorRecord(PanGen, unchecked((ushort)presetPan)),
                SoundFontTestHelper.GeneratorRecord(InstrumentGen, 0)));
            var inst = SoundFontTestHelper.Chunk("inst", SoundFontTestHelper.Concat(
                SoundFontTestHelper.InstrumentRecord("I", 0),
                SoundFontTestHelper.InstrumentRecord("EOI", 1)));
            var ibag = SoundFontTestHelper.Chunk("ibag", SoundFontTestHelper.Concat(
                SoundFontTestHelper.BagRecord(0, 0),
                SoundFontTestHelper.BagRecord(2, 0)));
            var imod = SoundFontTestHelper.Chunk("imod", SoundFontTestHelper.ModulatorRecord());
            // instrument zone: Pan absolute then SampleID index
            var igen = SoundFontTestHelper.Chunk("igen", SoundFontTestHelper.Concat(
                SoundFontTestHelper.GeneratorRecord(PanGen, unchecked((ushort)instrumentPan)),
                SoundFontTestHelper.GeneratorRecord(SampleIdGen, 0)));
            var shdr = SoundFontTestHelper.Chunk("shdr", SoundFontTestHelper.Concat(
                SoundFontTestHelper.SampleHeaderRecord("S", 0, 3, 0, 3, 44100, 60, 0, 0, 1),
                new byte[46]));

            var pdta = SoundFontTestHelper.ListChunk("pdta", phdr, pbag, pmod, pgen, inst, ibag, imod, igen, shdr);
            var sf2 = SoundFontTestHelper.BuildSoundFont(
                SoundFontTestHelper.BuildInfoList(),
                SoundFontTestHelper.BuildSdtaList(new byte[8]),
                pdta);
            return new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));
        }
    }
}
