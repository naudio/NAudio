using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class PitchWheelChangeEventTests
    {
        [Test]
        public void ConstructorSetsProperties()
        {
            var pitchEvent = new PitchWheelChangeEvent(123, 2, 0x2000);

            Assert.That(pitchEvent.AbsoluteTime, Is.EqualTo(123));
            Assert.That(pitchEvent.Channel, Is.EqualTo(2));
            Assert.That(pitchEvent.CommandCode, Is.EqualTo(MidiCommandCode.PitchWheelChange));
            Assert.That(pitchEvent.Pitch, Is.EqualTo(0x2000));
        }

        [Test]
        public void BinaryReaderConstructorSetsPitchFromLsbAndMsb()
        {
            using (var ms = new MemoryStream(new byte[] { 0x7D, 0x40 }))
            using (var br = new BinaryReader(ms))
            {
                var pitchEvent = new PitchWheelChangeEvent(br);

                Assert.That(pitchEvent.Pitch, Is.EqualTo(0x207D));
            }
        }

        [Test]
        public void BinaryReaderConstructorRejectsInvalidFirstDataByte()
        {
            using (var ms = new MemoryStream(new byte[] { 0x80, 0x00 }))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => new PitchWheelChangeEvent(br));
            }
        }

        [Test]
        public void BinaryReaderConstructorRejectsInvalidSecondDataByte()
        {
            using (var ms = new MemoryStream(new byte[] { 0x00, 0x80 }))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => new PitchWheelChangeEvent(br));
            }
        }

        [TestCase(-1)]
        [TestCase(0x4000)]
        public void ConstructorRejectsPitchOutOfRange(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PitchWheelChangeEvent(0, 1, value));
        }

        [TestCase(-1)]
        [TestCase(0x4000)]
        public void PitchPropertyRejectsOutOfRangeValues(int value)
        {
            var pitchEvent = new PitchWheelChangeEvent(0, 1, 0x2000);

            Assert.Throws<ArgumentOutOfRangeException>(() => pitchEvent.Pitch = value);
        }

        [Test]
        public void GetAsShortMessageReturnsCorrectValue()
        {
            var pitchEvent = new PitchWheelChangeEvent(0, 2, 0x3FFF);

            Assert.That(pitchEvent.GetAsShortMessage(), Is.EqualTo(0x007F7FE1));
        }

        [Test]
        public void ExportWritesDeltaStatusAndPitchBytes()
        {
            var pitchEvent = new PitchWheelChangeEvent(0, 2, 0x207D);
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                pitchEvent.Export(ref absoluteTime, writer);

                var bytes = ms.ToArray();
                Assert.That(bytes.Length, Is.EqualTo(4));
                Assert.That(bytes[0], Is.EqualTo(0x00));
                Assert.That(bytes[1], Is.EqualTo(0xE1));
                Assert.That(bytes[2], Is.EqualTo(0x7D));
                Assert.That(bytes[3], Is.EqualTo(0x40));
            }
        }

        [Test]
        public void ToStringIncludesAbsoluteAndCenteredPitch()
        {
            var pitchEvent = new PitchWheelChangeEvent(0, 1, 0x2001);

            var text = pitchEvent.ToString();

            Assert.That(text, Does.Contain("Pitch 8193"));
            Assert.That(text, Does.Contain("(1)"));
        }
    }
}
