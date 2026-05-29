using System.IO;
using System.Linq;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Core.Tests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class ModulatorTypeTests
    {
        [Test]
        public void DecodesMidiContinuousControllerSource()
        {
            // srcOper 0x0081: index 1 (CC#1), bit 7 set (MIDI CC),
            // bits 8-15 zero (linear, unipolar, increasing)
            var sf2 = SoundFontTestHelper.BuildRichSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            // the rich test font puts a real modulator on the first instrument's first zone
            var modulator = sf.Instruments
                .SelectMany(i => i.Zones)
                .Where(z => z.Modulators != null)
                .SelectMany(z => z.Modulators)
                .First(m => m.SourceModulationData.IsMidiContinuousController);

            var src = modulator.SourceModulationData;
            Assert.That(src.IsMidiContinuousController, Is.True);
            Assert.That(src.MidiContinuousControllerNumber, Is.EqualTo(1));
            Assert.That(src.SourceType, Is.EqualTo(SourceTypeEnum.Linear));
            Assert.That(src.Polarity, Is.False);  // unipolar
            Assert.That(src.Direction, Is.False); // increasing
            Assert.That(modulator.DestinationGenerator, Is.EqualTo(GeneratorEnum.InitialFilterCutoffFrequency));
            Assert.That(modulator.Amount, Is.EqualTo(1200));
        }

        // raw -> expected decoded fields, exercising each bit independently
        [TestCase((ushort)0x0000, false, false, false, SourceTypeEnum.Linear, ControllerSourceEnum.NoController)]
        [TestCase((ushort)0x0002, false, false, false, SourceTypeEnum.Linear, ControllerSourceEnum.NoteOnVelocity)]
        [TestCase((ushort)0x000E, false, false, false, SourceTypeEnum.Linear, ControllerSourceEnum.PitchWheel)]
        [TestCase((ushort)0x0100, false, true, false, SourceTypeEnum.Linear, ControllerSourceEnum.NoController)]  // direction
        [TestCase((ushort)0x0200, true, false, false, SourceTypeEnum.Linear, ControllerSourceEnum.NoController)]  // polarity
        [TestCase((ushort)0x0400, false, false, false, SourceTypeEnum.Concave, ControllerSourceEnum.NoController)] // type=1
        [TestCase((ushort)0x0C00, false, false, false, SourceTypeEnum.Switch, ControllerSourceEnum.NoController)]  // type=3
        public void DecodesBitFields(ushort raw, bool polarity, bool direction, bool isCc,
            SourceTypeEnum sourceType, ControllerSourceEnum controllerSource)
        {
            // Drive a modulator through the public parse path via ModulatorRecord.
            var modType = BuildModulatorTypeFromRaw(raw);

            Assert.That(modType.Polarity, Is.EqualTo(polarity));
            Assert.That(modType.Direction, Is.EqualTo(direction));
            Assert.That(modType.IsMidiContinuousController, Is.EqualTo(isCc));
            Assert.That(modType.SourceType, Is.EqualTo(sourceType));
            if (!isCc)
                Assert.That(modType.ControllerSource, Is.EqualTo(controllerSource));
        }

        /// <summary>
        /// Builds a SoundFont whose single instrument modulator has the given raw
        /// source operator, loads it, and returns the decoded ModulatorType.
        /// </summary>
        private static ModulatorType BuildModulatorTypeFromRaw(ushort raw)
        {
            var phdr = SoundFontTestHelper.Chunk("phdr", SoundFontTestHelper.Concat(
                SoundFontTestHelper.PresetHeaderRecord("P", 0, 0, 0),
                SoundFontTestHelper.PresetHeaderRecord("EOP", 0, 0, 1)));
            var pbag = SoundFontTestHelper.Chunk("pbag", SoundFontTestHelper.Concat(
                SoundFontTestHelper.BagRecord(0, 0),
                SoundFontTestHelper.BagRecord(1, 0)));
            var pmod = SoundFontTestHelper.Chunk("pmod", SoundFontTestHelper.ModulatorRecord());
            var pgen = SoundFontTestHelper.Chunk("pgen", SoundFontTestHelper.GeneratorRecord(41, 0));
            var inst = SoundFontTestHelper.Chunk("inst", SoundFontTestHelper.Concat(
                SoundFontTestHelper.InstrumentRecord("I", 0),
                SoundFontTestHelper.InstrumentRecord("EOI", 1)));
            var ibag = SoundFontTestHelper.Chunk("ibag", SoundFontTestHelper.Concat(
                SoundFontTestHelper.BagRecord(0, 0),
                SoundFontTestHelper.BagRecord(1, 1)));
            var imod = SoundFontTestHelper.Chunk("imod", SoundFontTestHelper.Concat(
                SoundFontTestHelper.ModulatorRecord(raw, 8, 0, 0, 0),
                SoundFontTestHelper.ModulatorRecord()));
            var igen = SoundFontTestHelper.Chunk("igen", SoundFontTestHelper.GeneratorRecord(53, 0));
            var shdr = SoundFontTestHelper.Chunk("shdr", SoundFontTestHelper.Concat(
                SoundFontTestHelper.SampleHeaderRecord("S", 0, 3, 0, 3, 44100, 60, 0, 0, 1),
                new byte[46]));

            var pdta = SoundFontTestHelper.ListChunk("pdta", phdr, pbag, pmod, pgen, inst, ibag, imod, igen, shdr);
            var sf2 = SoundFontTestHelper.BuildSoundFont(
                SoundFontTestHelper.BuildInfoList(),
                SoundFontTestHelper.BuildSdtaList(new byte[8]),
                pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));
            return sf.Instruments[0].Zones
                .Where(z => z.Modulators != null)
                .SelectMany(z => z.Modulators)
                .First()
                .SourceModulationData;
        }
    }
}
