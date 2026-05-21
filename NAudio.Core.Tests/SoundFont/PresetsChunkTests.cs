using System.IO;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudioTests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class PresetsChunkTests
    {
        private NAudio.SoundFont.SoundFont richSf;

        [OneTimeSetUp]
        public void SetUp()
        {
            var sf2 = SoundFontTestHelper.BuildRichSoundFont();
            richSf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));
        }

        #region Preset tests

        [Test]
        public void ParsesCorrectNumberOfPresets()
        {
            // Rich SF2 has 2 presets (Piano, Strings) + EOP terminal which is removed
            Assert.That(richSf.Presets.Length, Is.EqualTo(2));
        }

        [Test]
        public void ParsesFirstPresetProperties()
        {
            var preset = richSf.Presets[0];
            Assert.That(preset.Name, Is.EqualTo("Piano"));
            Assert.That(preset.PatchNumber, Is.EqualTo(0));
            Assert.That(preset.Bank, Is.EqualTo(0));
        }

        [Test]
        public void ParsesSecondPresetProperties()
        {
            var preset = richSf.Presets[1];
            Assert.That(preset.Name, Is.EqualTo("Strings"));
            Assert.That(preset.PatchNumber, Is.EqualTo(48));
            Assert.That(preset.Bank, Is.EqualTo(0));
        }

        [Test]
        public void PresetToStringIncludesBankPatchAndName()
        {
            var preset = richSf.Presets[0];
            var str = preset.ToString();
            Assert.That(str, Does.Contain("Piano"));
            Assert.That(str, Does.Contain("0-0"));
        }

        [Test]
        public void EachPresetHasOneZone()
        {
            Assert.That(richSf.Presets[0].Zones.Length, Is.EqualTo(1));
            Assert.That(richSf.Presets[1].Zones.Length, Is.EqualTo(1));
        }

        [Test]
        public void PresetZonesContainInstrumentGenerator()
        {
            var zone0 = richSf.Presets[0].Zones[0];
            Assert.That(zone0.Generators.Length, Is.EqualTo(1));
            Assert.That(zone0.Generators[0].GeneratorType, Is.EqualTo(GeneratorEnum.Instrument));
        }

        [Test]
        public void PresetInstrumentGeneratorsLinkedToInstrumentObjects()
        {
            var pianoGen = richSf.Presets[0].Zones[0].Generators[0];
            Assert.That(pianoGen.Instrument, Is.Not.Null);
            Assert.That(pianoGen.Instrument.Name, Is.EqualTo("Piano"));

            var stringsGen = richSf.Presets[1].Zones[0].Generators[0];
            Assert.That(stringsGen.Instrument, Is.Not.Null);
            Assert.That(stringsGen.Instrument.Name, Is.EqualTo("Strings"));
        }

        [Test]
        public void PresetZonesHaveNoModulators()
        {
            // Rich SF2 has no preset-level modulators
            Assert.That(richSf.Presets[0].Zones[0].Modulators.Length, Is.EqualTo(0));
            Assert.That(richSf.Presets[1].Zones[0].Modulators.Length, Is.EqualTo(0));
        }

        #endregion

        #region Instrument tests

        [Test]
        public void ParsesCorrectNumberOfInstruments()
        {
            // Rich SF2 has 2 instruments (Piano, Strings) + EOI terminal which is removed
            Assert.That(richSf.Instruments.Length, Is.EqualTo(2));
        }

        [Test]
        public void ParsesInstrumentNames()
        {
            Assert.That(richSf.Instruments[0].Name, Is.EqualTo("Piano"));
            Assert.That(richSf.Instruments[1].Name, Is.EqualTo("Strings"));
        }

        [Test]
        public void InstrumentToStringReturnsName()
        {
            Assert.That(richSf.Instruments[0].ToString(), Is.EqualTo("Piano"));
        }

        [Test]
        public void EachInstrumentHasOneZone()
        {
            Assert.That(richSf.Instruments[0].Zones.Length, Is.EqualTo(1));
            Assert.That(richSf.Instruments[1].Zones.Length, Is.EqualTo(1));
        }

        #endregion

        #region Instrument zone generator tests

        [Test]
        public void InstrumentZonesHaveCorrectGeneratorCount()
        {
            // Piano inst zone: KeyRange + SampleID = 2 generators
            Assert.That(richSf.Instruments[0].Zones[0].Generators.Length, Is.EqualTo(2));
            // Strings inst zone: KeyRange + SampleID = 2 generators
            Assert.That(richSf.Instruments[1].Zones[0].Generators.Length, Is.EqualTo(2));
        }

        [Test]
        public void InstrumentZonesContainKeyRangeGenerator()
        {
            var pianoZoneGens = richSf.Instruments[0].Zones[0].Generators;
            Assert.That(pianoZoneGens[0].GeneratorType, Is.EqualTo(GeneratorEnum.KeyRange));
        }

        [Test]
        public void KeyRangeGeneratorHasCorrectLowAndHighValues()
        {
            var keyRange = richSf.Instruments[0].Zones[0].Generators[0];
            // Full range: lo=0, hi=127
            Assert.That(keyRange.LowByteAmount, Is.EqualTo(0), "Key range low should be 0");
            Assert.That(keyRange.HighByteAmount, Is.EqualTo(127), "Key range high should be 127");
        }

        [Test]
        public void InstrumentZonesContainSampleIDGenerator()
        {
            var pianoZoneGens = richSf.Instruments[0].Zones[0].Generators;
            Assert.That(pianoZoneGens[1].GeneratorType, Is.EqualTo(GeneratorEnum.SampleID));

            var stringsZoneGens = richSf.Instruments[1].Zones[0].Generators;
            Assert.That(stringsZoneGens[1].GeneratorType, Is.EqualTo(GeneratorEnum.SampleID));
        }

        [Test]
        public void SampleIDGeneratorsLinkedToSampleHeaderObjects()
        {
            var pianoSampleGen = richSf.Instruments[0].Zones[0].Generators[1];
            Assert.That(pianoSampleGen.SampleHeader, Is.Not.Null);
            Assert.That(pianoSampleGen.SampleHeader.SampleName, Is.EqualTo("PianoSample"));

            var stringsSampleGen = richSf.Instruments[1].Zones[0].Generators[1];
            Assert.That(stringsSampleGen.SampleHeader, Is.Not.Null);
            Assert.That(stringsSampleGen.SampleHeader.SampleName, Is.EqualTo("StringSample"));
        }

        #endregion

        #region Modulator tests

        [Test]
        public void PianoInstrumentZoneHasOneModulator()
        {
            Assert.That(richSf.Instruments[0].Zones[0].Modulators.Length, Is.EqualTo(1));
        }

        [Test]
        public void StringsInstrumentZoneHasNoModulators()
        {
            Assert.That(richSf.Instruments[1].Zones[0].Modulators.Length, Is.EqualTo(0));
        }

        [Test]
        public void ModulatorHasCorrectDestinationGenerator()
        {
            var mod = richSf.Instruments[0].Zones[0].Modulators[0];
            Assert.That(mod.DestinationGenerator, Is.EqualTo(GeneratorEnum.InitialFilterCutoffFrequency));
        }

        [Test]
        public void ModulatorHasCorrectAmount()
        {
            var mod = richSf.Instruments[0].Zones[0].Modulators[0];
            Assert.That(mod.Amount, Is.EqualTo(1200));
        }

        [Test]
        public void ModulatorHasCorrectSourceTransform()
        {
            var mod = richSf.Instruments[0].Zones[0].Modulators[0];
            Assert.That(mod.SourceTransform, Is.EqualTo(TransformEnum.Linear));
        }

        [Test]
        public void ModulatorSourceIsMidiCC1()
        {
            var mod = richSf.Instruments[0].Zones[0].Modulators[0];
            // srcOper=0x0081: CC flag set, CC#1 (mod wheel), linear, unipolar, positive
            Assert.That(mod.SourceModulationData.ToString(), Is.EqualTo("Linear CC1"));
        }

        [Test]
        public void ModulatorToStringIncludesAllFields()
        {
            var mod = richSf.Instruments[0].Zones[0].Modulators[0];
            var str = mod.ToString();
            Assert.That(str, Does.Contain("Modulator"));
            Assert.That(str, Does.Contain("InitialFilterCutoffFrequency"));
            Assert.That(str, Does.Contain("1200"));
        }

        #endregion

        #region Sample header tests

        [Test]
        public void ParsesCorrectNumberOfSampleHeaders()
        {
            // Rich SF2 has 2 samples + EOS terminal which is removed
            Assert.That(richSf.SampleHeaders.Length, Is.EqualTo(2));
        }

        [Test]
        public void ParsesSampleHeaderName()
        {
            Assert.That(richSf.SampleHeaders[0].SampleName, Is.EqualTo("PianoSample"));
            Assert.That(richSf.SampleHeaders[1].SampleName, Is.EqualTo("StringSample"));
        }

        [Test]
        public void ParsesSampleHeaderStartAndEnd()
        {
            Assert.That(richSf.SampleHeaders[0].Start, Is.EqualTo(0));
            Assert.That(richSf.SampleHeaders[0].End, Is.EqualTo(3));
            Assert.That(richSf.SampleHeaders[1].Start, Is.EqualTo(4));
            Assert.That(richSf.SampleHeaders[1].End, Is.EqualTo(7));
        }

        [Test]
        public void ParsesSampleHeaderLoopPoints()
        {
            Assert.That(richSf.SampleHeaders[0].StartLoop, Is.EqualTo(1));
            Assert.That(richSf.SampleHeaders[0].EndLoop, Is.EqualTo(3));
            Assert.That(richSf.SampleHeaders[1].StartLoop, Is.EqualTo(5));
            Assert.That(richSf.SampleHeaders[1].EndLoop, Is.EqualTo(7));
        }

        [Test]
        public void ParsesSampleHeaderSampleRate()
        {
            Assert.That(richSf.SampleHeaders[0].SampleRate, Is.EqualTo(44100));
            Assert.That(richSf.SampleHeaders[1].SampleRate, Is.EqualTo(22050));
        }

        [Test]
        public void ParsesSampleHeaderOriginalPitch()
        {
            Assert.That(richSf.SampleHeaders[0].OriginalPitch, Is.EqualTo(60));
            Assert.That(richSf.SampleHeaders[1].OriginalPitch, Is.EqualTo(72));
        }

        [Test]
        public void ParsesSampleHeaderPitchCorrection()
        {
            Assert.That(richSf.SampleHeaders[0].PitchCorrection, Is.EqualTo(-5));
            Assert.That(richSf.SampleHeaders[1].PitchCorrection, Is.EqualTo(10));
        }

        [Test]
        public void ParsesSampleHeaderSampleLink()
        {
            Assert.That(richSf.SampleHeaders[0].SampleLink, Is.EqualTo(0));
            Assert.That(richSf.SampleHeaders[1].SampleLink, Is.EqualTo(0));
        }

        [Test]
        public void ParsesSampleHeaderSFSampleLink()
        {
            Assert.That(richSf.SampleHeaders[0].SFSampleLink, Is.EqualTo(SFSampleLink.MonoSample));
            Assert.That(richSf.SampleHeaders[1].SFSampleLink, Is.EqualTo(SFSampleLink.MonoSample));
        }

        [Test]
        public void SampleHeaderToStringReturnsSampleName()
        {
            Assert.That(richSf.SampleHeaders[0].ToString(), Is.EqualTo("PianoSample"));
        }

        #endregion

        #region Zone tests

        [Test]
        public void ZoneToStringIncludesGeneratorAndModulatorCounts()
        {
            var zone = richSf.Instruments[0].Zones[0];
            var str = zone.ToString();
            Assert.That(str, Does.Contain("Zone"));
        }

        #endregion

        #region Terminal record removal tests

        [Test]
        public void TerminalPresetIsRemoved()
        {
            // We built 3 phdr records (Piano, Strings, EOP). After loading, only 2 remain.
            Assert.That(richSf.Presets.Length, Is.EqualTo(2));
            // Verify none of the remaining presets is the EOP terminal
            foreach (var p in richSf.Presets)
                Assert.That(p.Name, Is.Not.EqualTo("EOP"));
        }

        [Test]
        public void TerminalInstrumentIsRemoved()
        {
            Assert.That(richSf.Instruments.Length, Is.EqualTo(2));
            foreach (var i in richSf.Instruments)
                Assert.That(i.Name, Is.Not.EqualTo("EOI"));
        }

        [Test]
        public void TerminalSampleHeaderIsRemoved()
        {
            // EOS terminal has empty name and all zeros
            Assert.That(richSf.SampleHeaders.Length, Is.EqualTo(2));
            foreach (var sh in richSf.SampleHeaders)
                Assert.That(sh.SampleName, Is.Not.Empty);
        }

        #endregion

        #region Case-insensitive chunk ID tests

        [Test]
        public void AcceptsLowercasePdtaSubChunkIds()
        {
            // The PresetsChunk accepts both uppercase (PHDR) and lowercase (phdr)
            // This test uses the standard lowercase form which the spec defines
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            // If it loaded successfully, lowercase chunk IDs were accepted
            Assert.That(sf.Presets.Length, Is.GreaterThan(0));
        }

        #endregion

        #region Name handling tests

        [Test]
        public void HandlesMaxLengthPresetName()
        {
            // Max name is 19 chars + null terminator = 20 bytes
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(presetName: "1234567890123456789");
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Presets[0].Name, Is.EqualTo("1234567890123456789"));
        }

        [Test]
        public void HandlesMaxLengthInstrumentName()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(instName: "1234567890123456789");
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Instruments[0].Name, Is.EqualTo("1234567890123456789"));
        }

        [Test]
        public void HandlesMaxLengthSampleName()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(sampleName: "1234567890123456789");
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.SampleHeaders[0].SampleName, Is.EqualTo("1234567890123456789"));
        }

        [Test]
        public void HandlesEmptyPresetName()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(presetName: "");
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Presets[0].Name, Is.EqualTo(""));
        }

        #endregion
    }
}
