using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class ControlChangeEventTests
    {
        [Test]
        public void ConstructorSetsProperties()
        {
            var controlChangeEvent = new ControlChangeEvent(123, 2, MidiController.Expression, 100);

            Assert.That(controlChangeEvent.AbsoluteTime, Is.EqualTo(123));
            Assert.That(controlChangeEvent.Channel, Is.EqualTo(2));
            Assert.That(controlChangeEvent.CommandCode, Is.EqualTo(MidiCommandCode.ControlChange));
            Assert.That(controlChangeEvent.Controller, Is.EqualTo(MidiController.Expression));
            Assert.That(controlChangeEvent.ControllerValue, Is.EqualTo(100));
        }

        [Test]
        public void BinaryReaderConstructorSetsControllerAndValue()
        {
            using (var ms = new MemoryStream(new byte[] { (byte)MidiController.MainVolume, 127 }))
            using (var reader = new BinaryReader(ms))
            {
                var controlChangeEvent = new ControlChangeEvent(reader);

                Assert.That(controlChangeEvent.Controller, Is.EqualTo(MidiController.MainVolume));
                Assert.That(controlChangeEvent.ControllerValue, Is.EqualTo(127));
            }
        }

        [Test]
        public void BinaryReaderConstructorRejectsControllerWithMsbSet()
        {
            using (var ms = new MemoryStream(new byte[] { 0x80, 0x00 }))
            using (var reader = new BinaryReader(ms))
            {
                Assert.Throws<InvalidDataException>(() => new ControlChangeEvent(reader));
            }
        }

        [Test]
        public void BinaryReaderConstructorRejectsControllerValueWithMsbSet()
        {
            using (var ms = new MemoryStream(new byte[] { 0x07, 0x80 }))
            using (var reader = new BinaryReader(ms))
            {
                Assert.Throws<InvalidDataException>(() => new ControlChangeEvent(reader));
            }
        }

        [TestCase(128)]
        [TestCase(255)]
        public void ControllerRejectsValuesOutOfRange(int value)
        {
            var controlChangeEvent = new ControlChangeEvent(0, 1, MidiController.Modulation, 64);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                controlChangeEvent.Controller = unchecked((MidiController)value));
        }

        [TestCase(-1)]
        [TestCase(128)]
        public void ControllerValueRejectsValuesOutOfRange(int value)
        {
            var controlChangeEvent = new ControlChangeEvent(0, 1, MidiController.Modulation, 64);

            Assert.Throws<ArgumentOutOfRangeException>(() => controlChangeEvent.ControllerValue = value);
        }

        [Test]
        public void GetAsShortMessageReturnsCorrectValue()
        {
            var controlChangeEvent = new ControlChangeEvent(0, 2, MidiController.Expression, 127);

            Assert.That(controlChangeEvent.GetAsShortMessage(), Is.EqualTo(0x007F0BB1));
        }

        [Test]
        public void ExportWritesDeltaStatusControllerAndValue()
        {
            var controlChangeEvent = new ControlChangeEvent(0, 2, MidiController.Expression, 127);
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                controlChangeEvent.Export(ref absoluteTime, writer);

                var bytes = ms.ToArray();
                Assert.That(bytes.Length, Is.EqualTo(4));
                Assert.That(bytes[0], Is.EqualTo(0x00));
                Assert.That(bytes[1], Is.EqualTo(0xB1));
                Assert.That(bytes[2], Is.EqualTo((byte)MidiController.Expression));
                Assert.That(bytes[3], Is.EqualTo(0x7F));
            }
        }
    }
}
