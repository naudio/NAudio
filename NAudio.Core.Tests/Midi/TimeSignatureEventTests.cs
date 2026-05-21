using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class TimeSignatureEventTests
    {
        [Test]
        public void ConstructorSetsProperties()
        {
            var timeSignatureEvent = new TimeSignatureEvent(123, 3, 2, 24, 8);

            Assert.That(timeSignatureEvent.AbsoluteTime, Is.EqualTo(123));
            Assert.That(timeSignatureEvent.CommandCode, Is.EqualTo(MidiCommandCode.MetaEvent));
            Assert.That(timeSignatureEvent.MetaEventType, Is.EqualTo(MetaEventType.TimeSignature));
            Assert.That(timeSignatureEvent.Numerator, Is.EqualTo(3));
            Assert.That(timeSignatureEvent.Denominator, Is.EqualTo(2));
            Assert.That(timeSignatureEvent.TicksInMetronomeClick, Is.EqualTo(24));
            Assert.That(timeSignatureEvent.No32ndNotesInQuarterNote, Is.EqualTo(8));
        }

        [TestCase(-1)]
        [TestCase(256)]
        public void ConstructorRejectsNumeratorOutOfRange(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignatureEvent(0, value, 2, 24, 8));
        }

        [TestCase(-1)]
        [TestCase(256)]
        public void ConstructorRejectsDenominatorOutOfRange(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignatureEvent(0, 3, value, 24, 8));
        }

        [TestCase(-1)]
        [TestCase(256)]
        public void ConstructorRejectsTicksInMetronomeClickOutOfRange(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignatureEvent(0, 3, 2, value, 8));
        }

        [TestCase(-1)]
        [TestCase(256)]
        public void ConstructorRejectsNo32ndNotesInQuarterNoteOutOfRange(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSignatureEvent(0, 3, 2, 24, value));
        }

        [Test]
        public void BinaryReaderConstructorRejectsInvalidLength()
        {
            using (var ms = new MemoryStream(new byte[] { 0x04, 0x02, 0x18, 0x08 }))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => new TimeSignatureEvent(br, 3));
            }
        }

        [Test]
        public void BinaryReaderConstructorReadsAllFields()
        {
            using (var ms = new MemoryStream(new byte[] { 0x04, 0x02, 0x18, 0x08 }))
            using (var br = new BinaryReader(ms))
            {
                var timeSignatureEvent = new TimeSignatureEvent(br, 4);

                Assert.That(timeSignatureEvent.Numerator, Is.EqualTo(4));
                Assert.That(timeSignatureEvent.Denominator, Is.EqualTo(2));
                Assert.That(timeSignatureEvent.TicksInMetronomeClick, Is.EqualTo(24));
                Assert.That(timeSignatureEvent.No32ndNotesInQuarterNote, Is.EqualTo(8));
            }
        }

        [Test]
        public void ReadNextEventParsesTimeSignatureMetaEvent()
        {
            var bytes = new byte[] { 0x00, 0xFF, 0x58, 0x04, 0x03, 0x02, 0x18, 0x08 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, null);

                Assert.That(midiEvent, Is.TypeOf<TimeSignatureEvent>());
                var timeSignatureEvent = (TimeSignatureEvent)midiEvent;
                Assert.That(timeSignatureEvent.DeltaTime, Is.EqualTo(0));
                Assert.That(timeSignatureEvent.Channel, Is.EqualTo(1));
                Assert.That(timeSignatureEvent.CommandCode, Is.EqualTo(MidiCommandCode.MetaEvent));
                Assert.That(timeSignatureEvent.MetaEventType, Is.EqualTo(MetaEventType.TimeSignature));
                Assert.That(timeSignatureEvent.Numerator, Is.EqualTo(3));
                Assert.That(timeSignatureEvent.Denominator, Is.EqualTo(2));
                Assert.That(timeSignatureEvent.TicksInMetronomeClick, Is.EqualTo(24));
                Assert.That(timeSignatureEvent.No32ndNotesInQuarterNote, Is.EqualTo(8));
            }
        }

        [TestCase(0, "3/1")]
        [TestCase(1, "3/2")]
        [TestCase(2, "3/4")]
        [TestCase(3, "3/8")]
        [TestCase(4, "3/16")]
        [TestCase(5, "3/32")]
        [TestCase(6, "3/64")]
        public void TimeSignatureReturnsPowerOfTwoDenominatorText(int denominator, string expected)
        {
            var timeSignatureEvent = new TimeSignatureEvent(0, 3, denominator, 24, 8);

            Assert.That(timeSignatureEvent.TimeSignature, Is.EqualTo(expected));
        }

        [Test]
        public void TimeSignatureReturnsUnknownForVeryLargeDenominatorExponent()
        {
            var timeSignatureEvent = new TimeSignatureEvent(0, 3, 31, 24, 8);

            Assert.That(timeSignatureEvent.TimeSignature, Is.EqualTo("3/Unknown (31)"));
        }

        [Test]
        public void ExportWritesDeltaMetaTypeLengthAndData()
        {
            var timeSignatureEvent = new TimeSignatureEvent(10, 4, 2, 24, 8);

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                timeSignatureEvent.Export(ref absoluteTime, writer);

                Assert.That(absoluteTime, Is.EqualTo(10));
                Assert.That(ms.ToArray(), Is.EqualTo(new byte[] { 0x0A, 0xFF, 0x58, 0x04, 0x04, 0x02, 0x18, 0x08 }));
            }
        }

        [Test]
        public void CloneCopiesAllProperties()
        {
            var timeSignatureEvent = new TimeSignatureEvent(55, 6, 3, 36, 8);

            var clone = (TimeSignatureEvent)timeSignatureEvent.Clone();

            Assert.That(clone, Is.Not.SameAs(timeSignatureEvent));
            Assert.That(clone.AbsoluteTime, Is.EqualTo(timeSignatureEvent.AbsoluteTime));
            Assert.That(clone.CommandCode, Is.EqualTo(timeSignatureEvent.CommandCode));
            Assert.That(clone.MetaEventType, Is.EqualTo(timeSignatureEvent.MetaEventType));
            Assert.That(clone.Numerator, Is.EqualTo(timeSignatureEvent.Numerator));
            Assert.That(clone.Denominator, Is.EqualTo(timeSignatureEvent.Denominator));
            Assert.That(clone.TicksInMetronomeClick, Is.EqualTo(timeSignatureEvent.TicksInMetronomeClick));
            Assert.That(clone.No32ndNotesInQuarterNote, Is.EqualTo(timeSignatureEvent.No32ndNotesInQuarterNote));
            Assert.That(clone.TimeSignature, Is.EqualTo(timeSignatureEvent.TimeSignature));
        }

        [Test]
        public void ToStringIncludesTimeSignatureAndTimingFields()
        {
            var timeSignatureEvent = new TimeSignatureEvent(0, 3, 2, 24, 8);

            var text = timeSignatureEvent.ToString();

            Assert.That(text, Does.Contain("TimeSignature"));
            Assert.That(text, Does.Contain("3/4"));
            Assert.That(text, Does.Contain("TicksInClick:24"));
            Assert.That(text, Does.Contain("32ndsInQuarterNote:8"));
        }
    }
}
