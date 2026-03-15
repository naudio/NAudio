using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class NoteEventTests
    {
        [Test]
        public void ConstructorSetsProperties()
        {
            var noteEvent = new NoteEvent(123, 2, MidiCommandCode.NoteOn, 60, 100);

            Assert.That(noteEvent.AbsoluteTime, Is.EqualTo(123));
            Assert.That(noteEvent.Channel, Is.EqualTo(2));
            Assert.That(noteEvent.CommandCode, Is.EqualTo(MidiCommandCode.NoteOn));
            Assert.That(noteEvent.NoteNumber, Is.EqualTo(60));
            Assert.That(noteEvent.Velocity, Is.EqualTo(100));
        }

        [Test]
        public void BinaryReaderConstructorSetsNoteAndVelocity()
        {
            using (var ms = new MemoryStream(new byte[] { 60, 100 }))
            using (var reader = new BinaryReader(ms))
            {
                var noteEvent = new NoteEvent(reader);

                Assert.That(noteEvent.NoteNumber, Is.EqualTo(60));
                Assert.That(noteEvent.Velocity, Is.EqualTo(100));
            }
        }

        [Test]
        public void BinaryReaderConstructorClampsVelocityAbove127()
        {
            using (var ms = new MemoryStream(new byte[] { 60, 200 }))
            using (var reader = new BinaryReader(ms))
            {
                var noteEvent = new NoteEvent(reader);
                Assert.That(noteEvent.Velocity, Is.EqualTo(127));
            }
        }

        [TestCase(-1)]
        [TestCase(128)]
        public void NoteNumberRejectsValuesOutOfRange(int value)
        {
            var noteEvent = new NoteEvent(0, 1, MidiCommandCode.NoteOn, 60, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => noteEvent.NoteNumber = value);
        }

        [TestCase(-1)]
        [TestCase(128)]
        public void VelocityRejectsValuesOutOfRange(int value)
        {
            var noteEvent = new NoteEvent(0, 1, MidiCommandCode.NoteOn, 60, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => noteEvent.Velocity = value);
        }

        [Test]
        public void GetAsShortMessageReturnsCorrectValue()
        {
            var noteEvent = new NoteEvent(0, 2, MidiCommandCode.NoteOn, 60, 100);
            Assert.That(noteEvent.GetAsShortMessage(), Is.EqualTo(0x00643C91));
        }

        [TestCase(10)]
        [TestCase(16)]
        public void NoteNameReturnsDrumNameOnDrumChannels(int channel)
        {
            var noteEvent = new NoteEvent(0, channel, MidiCommandCode.NoteOn, 35, 100);
            Assert.That(noteEvent.NoteName, Is.EqualTo("Acoustic Bass Drum"));
        }

        [TestCase(10)]
        [TestCase(16)]
        public void NoteNameReturnsDrumFallbackWhenUnknown(int channel)
        {
            var noteEvent = new NoteEvent(0, channel, MidiCommandCode.NoteOn, 34, 100);
            Assert.That(noteEvent.NoteName, Is.EqualTo("Drum 34"));
        }

        [Test]
        public void NoteNameReturnsMelodicNameOnNonDrumChannel()
        {
            var noteEvent = new NoteEvent(0, 1, MidiCommandCode.NoteOn, 60, 100);
            Assert.That(noteEvent.NoteName, Is.EqualTo("C5"));
        }

        [Test]
        public void ExportWritesDeltaStatusNoteAndVelocity()
        {
            var noteEvent = new NoteEvent(0, 2, MidiCommandCode.NoteOn, 60, 100);
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                noteEvent.Export(ref absoluteTime, writer);

                var bytes = ms.ToArray();
                Assert.That(bytes.Length, Is.EqualTo(4));
                Assert.That(bytes[0], Is.EqualTo(0x00));
                Assert.That(bytes[1], Is.EqualTo(0x91));
                Assert.That(bytes[2], Is.EqualTo(0x3C));
                Assert.That(bytes[3], Is.EqualTo(0x64));
            }
        }
    }
}
