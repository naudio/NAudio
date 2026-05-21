using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudioTests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class GeneratorTests
    {
        #region Generator amount property tests

        [Test]
        public void UInt16AmountStoresAndRetrievesCorrectly()
        {
            var g = new Generator();
            g.UInt16Amount = 0x1234;
            Assert.That(g.UInt16Amount, Is.EqualTo(0x1234));
        }

        [Test]
        public void Int16AmountConvertsFromUInt16()
        {
            var g = new Generator();
            g.UInt16Amount = 0xFFFF;
            Assert.That(g.Int16Amount, Is.EqualTo(-1));
        }

        [Test]
        public void Int16AmountConvertsPositiveValue()
        {
            var g = new Generator();
            g.UInt16Amount = 0x7FFF;
            Assert.That(g.Int16Amount, Is.EqualTo(32767));
        }

        [Test]
        public void SettingInt16AmountUpdatesUInt16Amount()
        {
            var g = new Generator();
            g.Int16Amount = -1;
            Assert.That(g.UInt16Amount, Is.EqualTo(0xFFFF));
        }

        [Test]
        public void LowByteAmountGetsLowByte()
        {
            var g = new Generator();
            g.UInt16Amount = 0x1234;
            Assert.That(g.LowByteAmount, Is.EqualTo(0x34));
        }

        [Test]
        public void HighByteAmountGetsHighByte()
        {
            var g = new Generator();
            g.UInt16Amount = 0x1234;
            Assert.That(g.HighByteAmount, Is.EqualTo(0x12));
        }

        [Test]
        public void SettingLowBytePreservesHighByte()
        {
            var g = new Generator();
            g.UInt16Amount = 0x1234;
            g.LowByteAmount = 0x56;
            Assert.That(g.UInt16Amount, Is.EqualTo(0x1256));
        }

        [Test]
        public void SettingHighBytePreservesLowByte()
        {
            var g = new Generator();
            g.UInt16Amount = 0x1234;
            g.HighByteAmount = 0x78;
            Assert.That(g.UInt16Amount, Is.EqualTo(0x7834));
        }

        [Test]
        public void LowAndHighBytesFormKeyRange()
        {
            // KeyRange: lo byte = low key, hi byte = high key
            var g = new Generator();
            g.GeneratorType = GeneratorEnum.KeyRange;
            g.LowByteAmount = 36;   // low key = C2
            g.HighByteAmount = 96;  // high key = C7
            Assert.That(g.LowByteAmount, Is.EqualTo(36));
            Assert.That(g.HighByteAmount, Is.EqualTo(96));
            Assert.That(g.UInt16Amount, Is.EqualTo((96 << 8) | 36));
        }

        [Test]
        public void LowAndHighBytesFormVelocityRange()
        {
            var g = new Generator();
            g.GeneratorType = GeneratorEnum.VelocityRange;
            g.LowByteAmount = 1;    // min velocity
            g.HighByteAmount = 127;  // max velocity
            Assert.That(g.LowByteAmount, Is.EqualTo(1));
            Assert.That(g.HighByteAmount, Is.EqualTo(127));
        }

        #endregion

        #region Generator ToString tests

        [Test]
        public void ToStringShowsInstrumentName()
        {
            var g = new Generator();
            g.GeneratorType = GeneratorEnum.Instrument;
            g.Instrument = new Instrument { Name = "TestInst" };
            Assert.That(g.ToString(), Does.Contain("Instrument"));
            Assert.That(g.ToString(), Does.Contain("TestInst"));
        }

        [Test]
        public void ToStringShowsSampleHeader()
        {
            var g = new Generator();
            g.GeneratorType = GeneratorEnum.SampleID;
            g.SampleHeader = new SampleHeader { SampleName = "TestSample" };
            Assert.That(g.ToString(), Does.Contain("SampleID"));
            Assert.That(g.ToString(), Does.Contain("TestSample"));
        }

        [Test]
        public void ToStringShowsGeneratorTypeAndAmount()
        {
            var g = new Generator();
            g.GeneratorType = GeneratorEnum.Pan;
            g.Int16Amount = -500;
            var str = g.ToString();
            Assert.That(str, Does.Contain("Pan"));
        }

        #endregion

        #region GeneratorEnum value tests (SF2 spec section 8.1.2)

        [TestCase(GeneratorEnum.StartAddressOffset, 0)]
        [TestCase(GeneratorEnum.EndAddressOffset, 1)]
        [TestCase(GeneratorEnum.StartLoopAddressOffset, 2)]
        [TestCase(GeneratorEnum.EndLoopAddressOffset, 3)]
        [TestCase(GeneratorEnum.StartAddressCoarseOffset, 4)]
        [TestCase(GeneratorEnum.ModulationLFOToPitch, 5)]
        [TestCase(GeneratorEnum.VibratoLFOToPitch, 6)]
        [TestCase(GeneratorEnum.ModulationEnvelopeToPitch, 7)]
        [TestCase(GeneratorEnum.InitialFilterCutoffFrequency, 8)]
        [TestCase(GeneratorEnum.InitialFilterQ, 9)]
        [TestCase(GeneratorEnum.ModulationLFOToFilterCutoffFrequency, 10)]
        [TestCase(GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency, 11)]
        [TestCase(GeneratorEnum.EndAddressCoarseOffset, 12)]
        [TestCase(GeneratorEnum.ModulationLFOToVolume, 13)]
        [TestCase(GeneratorEnum.Unused1, 14)]
        [TestCase(GeneratorEnum.ChorusEffectsSend, 15)]
        [TestCase(GeneratorEnum.ReverbEffectsSend, 16)]
        [TestCase(GeneratorEnum.Pan, 17)]
        [TestCase(GeneratorEnum.Unused2, 18)]
        [TestCase(GeneratorEnum.Unused3, 19)]
        [TestCase(GeneratorEnum.Unused4, 20)]
        [TestCase(GeneratorEnum.DelayModulationLFO, 21)]
        [TestCase(GeneratorEnum.FrequencyModulationLFO, 22)]
        [TestCase(GeneratorEnum.DelayVibratoLFO, 23)]
        [TestCase(GeneratorEnum.FrequencyVibratoLFO, 24)]
        [TestCase(GeneratorEnum.DelayModulationEnvelope, 25)]
        [TestCase(GeneratorEnum.AttackModulationEnvelope, 26)]
        [TestCase(GeneratorEnum.HoldModulationEnvelope, 27)]
        [TestCase(GeneratorEnum.DecayModulationEnvelope, 28)]
        [TestCase(GeneratorEnum.SustainModulationEnvelope, 29)]
        [TestCase(GeneratorEnum.ReleaseModulationEnvelope, 30)]
        [TestCase(GeneratorEnum.KeyNumberToModulationEnvelopeHold, 31)]
        [TestCase(GeneratorEnum.KeyNumberToModulationEnvelopeDecay, 32)]
        [TestCase(GeneratorEnum.DelayVolumeEnvelope, 33)]
        [TestCase(GeneratorEnum.AttackVolumeEnvelope, 34)]
        [TestCase(GeneratorEnum.HoldVolumeEnvelope, 35)]
        [TestCase(GeneratorEnum.DecayVolumeEnvelope, 36)]
        [TestCase(GeneratorEnum.SustainVolumeEnvelope, 37)]
        [TestCase(GeneratorEnum.ReleaseVolumeEnvelope, 38)]
        [TestCase(GeneratorEnum.KeyNumberToVolumeEnvelopeHold, 39)]
        [TestCase(GeneratorEnum.KeyNumberToVolumeEnvelopeDecay, 40)]
        [TestCase(GeneratorEnum.Instrument, 41)]
        [TestCase(GeneratorEnum.Reserved1, 42)]
        [TestCase(GeneratorEnum.KeyRange, 43)]
        [TestCase(GeneratorEnum.VelocityRange, 44)]
        [TestCase(GeneratorEnum.StartLoopAddressCoarseOffset, 45)]
        [TestCase(GeneratorEnum.KeyNumber, 46)]
        [TestCase(GeneratorEnum.Velocity, 47)]
        [TestCase(GeneratorEnum.InitialAttenuation, 48)]
        [TestCase(GeneratorEnum.Reserved2, 49)]
        [TestCase(GeneratorEnum.EndLoopAddressCoarseOffset, 50)]
        [TestCase(GeneratorEnum.CoarseTune, 51)]
        [TestCase(GeneratorEnum.FineTune, 52)]
        [TestCase(GeneratorEnum.SampleID, 53)]
        [TestCase(GeneratorEnum.SampleModes, 54)]
        [TestCase(GeneratorEnum.Reserved3, 55)]
        [TestCase(GeneratorEnum.ScaleTuning, 56)]
        [TestCase(GeneratorEnum.ExclusiveClass, 57)]
        [TestCase(GeneratorEnum.OverridingRootKey, 58)]
        [TestCase(GeneratorEnum.Unused5, 59)]
        [TestCase(GeneratorEnum.UnusedEnd, 60)]
        public void GeneratorEnumValueMatchesSpec(GeneratorEnum gen, int expectedValue)
        {
            Assert.That((int)gen, Is.EqualTo(expectedValue),
                $"GeneratorEnum.{gen} should have value {expectedValue} per SF2 spec section 8.1.2");
        }

        #endregion

        #region SFSampleLink enum value tests (SF2 spec section 7.10)

        [TestCase(SFSampleLink.MonoSample, (ushort)1)]
        [TestCase(SFSampleLink.RightSample, (ushort)2)]
        [TestCase(SFSampleLink.LeftSample, (ushort)4)]
        [TestCase(SFSampleLink.LinkedSample, (ushort)8)]
        [TestCase(SFSampleLink.RomMonoSample, (ushort)0x8001)]
        [TestCase(SFSampleLink.RomRightSample, (ushort)0x8002)]
        [TestCase(SFSampleLink.RomLeftSample, (ushort)0x8004)]
        [TestCase(SFSampleLink.RomLinkedSample, (ushort)0x8008)]
        public void SFSampleLinkValueMatchesSpec(SFSampleLink link, ushort expectedValue)
        {
            Assert.That((ushort)link, Is.EqualTo(expectedValue),
                $"SFSampleLink.{link} should have value 0x{expectedValue:X4} per SF2 spec section 7.10");
        }

        #endregion

        #region SampleMode enum value tests (SF2 spec section 8.1.2, generator 54)

        [TestCase(SampleMode.NoLoop, 0)]
        [TestCase(SampleMode.LoopContinuously, 1)]
        [TestCase(SampleMode.ReservedNoLoop, 2)]
        [TestCase(SampleMode.LoopAndContinue, 3)]
        public void SampleModeValueMatchesSpec(SampleMode mode, int expectedValue)
        {
            Assert.That((int)mode, Is.EqualTo(expectedValue),
                $"SampleMode.{mode} should have value {expectedValue} per SF2 spec");
        }

        #endregion

        #region ControllerSourceEnum value tests (SF2 spec section 8.2.1)

        [TestCase(ControllerSourceEnum.NoController, 0)]
        [TestCase(ControllerSourceEnum.NoteOnVelocity, 2)]
        [TestCase(ControllerSourceEnum.NoteOnKeyNumber, 3)]
        [TestCase(ControllerSourceEnum.PolyPressure, 10)]
        [TestCase(ControllerSourceEnum.ChannelPressure, 13)]
        [TestCase(ControllerSourceEnum.PitchWheel, 14)]
        [TestCase(ControllerSourceEnum.PitchWheelSensitivity, 16)]
        public void ControllerSourceEnumValueMatchesSpec(ControllerSourceEnum src, int expectedValue)
        {
            Assert.That((int)src, Is.EqualTo(expectedValue),
                $"ControllerSourceEnum.{src} should have value {expectedValue} per SF2 spec section 8.2.1");
        }

        #endregion

        #region SourceTypeEnum value tests (SF2 spec section 8.2.1)

        [TestCase(SourceTypeEnum.Linear, 0)]
        [TestCase(SourceTypeEnum.Concave, 1)]
        [TestCase(SourceTypeEnum.Convex, 2)]
        [TestCase(SourceTypeEnum.Switch, 3)]
        public void SourceTypeEnumValueMatchesSpec(SourceTypeEnum type, int expectedValue)
        {
            Assert.That((int)type, Is.EqualTo(expectedValue),
                $"SourceTypeEnum.{type} should have value {expectedValue} per SF2 spec section 8.2.1");
        }

        #endregion

        #region TransformEnum value tests

        [Test]
        public void TransformEnumLinearIsZero()
        {
            Assert.That((int)TransformEnum.Linear, Is.EqualTo(0));
        }

        #endregion
    }
}
