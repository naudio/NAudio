using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class NoteOnEventTests
    {
        [Test]
        public void ConstructorSetsOffEventAndNoteLength()
        {
            var noteOn = new NoteOnEvent(10, 2, 60, 100, 25);

            Assert.That(noteOn.AbsoluteTime, Is.EqualTo(10));
            Assert.That(noteOn.Channel, Is.EqualTo(2));
            Assert.That(noteOn.NoteNumber, Is.EqualTo(60));
            Assert.That(noteOn.Velocity, Is.EqualTo(100));
            Assert.That(noteOn.NoteLength, Is.EqualTo(25));

            Assert.That(noteOn.OffEvent, Is.Not.Null);
            Assert.That(noteOn.OffEvent.CommandCode, Is.EqualTo(MidiCommandCode.NoteOff));
            Assert.That(noteOn.OffEvent.NoteNumber, Is.EqualTo(60));
            Assert.That(noteOn.OffEvent.Channel, Is.EqualTo(2));
            Assert.That(noteOn.OffEvent.AbsoluteTime, Is.EqualTo(35));
        }

        [Test]
        public void CloneCreatesEquivalentIndependentCopy()
        {
            var noteOn = new NoteOnEvent(10, 2, 60, 100, 25);

            var clone = (NoteOnEvent)noteOn.Clone();

            Assert.That(clone, Is.Not.SameAs(noteOn));
            Assert.That(clone.AbsoluteTime, Is.EqualTo(noteOn.AbsoluteTime));
            Assert.That(clone.Channel, Is.EqualTo(noteOn.Channel));
            Assert.That(clone.NoteNumber, Is.EqualTo(noteOn.NoteNumber));
            Assert.That(clone.Velocity, Is.EqualTo(noteOn.Velocity));
            Assert.That(clone.NoteLength, Is.EqualTo(noteOn.NoteLength));
            Assert.That(clone.OffEvent, Is.Not.SameAs(noteOn.OffEvent));

            clone.NoteNumber = 61;
            clone.Channel = 3;
            clone.NoteLength = 40;

            Assert.That(noteOn.NoteNumber, Is.EqualTo(60));
            Assert.That(noteOn.Channel, Is.EqualTo(2));
            Assert.That(noteOn.NoteLength, Is.EqualTo(25));
        }

        [Test]
        public void OffEventRejectsNull()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);

            Assert.Throws<ArgumentException>(() => noteOn.OffEvent = null);
        }

        [Test]
        public void OffEventRejectsNonNoteOffEvent()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);
            var invalidOffEvent = new NoteEvent(0, 1, MidiCommandCode.ControlChange, 60, 0);

            Assert.Throws<ArgumentException>(() => noteOn.OffEvent = invalidOffEvent);
        }

        [Test]
        public void OffEventRejectsDifferentNoteNumber()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);
            var invalidOffEvent = new NoteEvent(0, 1, MidiCommandCode.NoteOff, 61, 0);

            Assert.Throws<ArgumentException>(() => noteOn.OffEvent = invalidOffEvent);
        }

        [Test]
        public void OffEventRejectsDifferentChannel()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);
            var invalidOffEvent = new NoteEvent(0, 2, MidiCommandCode.NoteOff, 60, 0);

            Assert.Throws<ArgumentException>(() => noteOn.OffEvent = invalidOffEvent);
        }

        [Test]
        public void OffEventAcceptsNoteOnWithZeroVelocity()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);
            var validOffEvent = new NoteEvent(0, 1, MidiCommandCode.NoteOn, 60, 0);

            noteOn.OffEvent = validOffEvent;

            Assert.That(noteOn.OffEvent, Is.SameAs(validOffEvent));
        }

        [Test]
        public void NoteNumberSetterUpdatesOffEventNoteNumber()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);

            noteOn.NoteNumber = 62;

            Assert.That(noteOn.NoteNumber, Is.EqualTo(62));
            Assert.That(noteOn.OffEvent.NoteNumber, Is.EqualTo(62));
        }

        [Test]
        public void ChannelSetterUpdatesOffEventChannel()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);

            noteOn.Channel = 2;

            Assert.That(noteOn.Channel, Is.EqualTo(2));
            Assert.That(noteOn.OffEvent.Channel, Is.EqualTo(2));
        }

        [Test]
        public void NoteLengthSetterRejectsNegativeValues()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);

            Assert.Throws<ArgumentException>(() => noteOn.NoteLength = -1);
        }

        [Test]
        public void NoteLengthSetterUpdatesOffEventAbsoluteTime()
        {
            var noteOn = new NoteOnEvent(10, 1, 60, 100, 5);

            noteOn.NoteLength = 20;

            Assert.That(noteOn.NoteLength, Is.EqualTo(20));
            Assert.That(noteOn.OffEvent.AbsoluteTime, Is.EqualTo(30));
        }

        [Test]
        public void ToStringIncludesNoteOffMarkerWhenVelocityZeroAndNoOffEvent()
        {
            using (var ms = new MemoryStream(new byte[] { 60, 0 }))
            using (var reader = new BinaryReader(ms))
            {
                var noteOn = new NoteOnEvent(reader);
                var text = noteOn.ToString();

                Assert.That(text, Does.Contain("(Note Off)"));
            }
        }

        [Test]
        public void ToStringIncludesUnknownLengthWhenOffEventMissing()
        {
            using (var ms = new MemoryStream(new byte[] { 60, 100 }))
            using (var reader = new BinaryReader(ms))
            {
                var noteOn = new NoteOnEvent(reader);
                var text = noteOn.ToString();

                Assert.That(text, Does.Contain("Len: ?"));
            }
        }

        [Test]
        public void NoteLengthGetterThrowsInvalidOperationExceptionWhenOffEventMissing()
        {
            using (var ms = new MemoryStream(new byte[] { 60, 100 }))
            using (var reader = new BinaryReader(ms))
            {
                var noteOn = new NoteOnEvent(reader);
                var ex = Assert.Throws<InvalidOperationException>(() => _ = noteOn.NoteLength);
                Assert.That(ex.Message, Is.EqualTo("Cannot get NoteLength when OffEvent is null"));
            }
        }

        [Test]
        public void NoteLengthSetterThrowsInvalidOperationExceptionWhenOffEventMissing()
        {
            using (var ms = new MemoryStream(new byte[] { 60, 100 }))
            using (var reader = new BinaryReader(ms))
            {
                var noteOn = new NoteOnEvent(reader);
                var ex = Assert.Throws<InvalidOperationException>(() => noteOn.NoteLength = 10);
                Assert.That(ex.Message, Is.EqualTo("Cannot set NoteLength when OffEvent is null"));
            }
        }
    }
}
