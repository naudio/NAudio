using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class KeySignatureEventTests
    {
        [Test]
        public void ConstructorSetsProperties()
        {
            var keySignatureEvent = new KeySignatureEvent(-3, 1, 123);

            Assert.That(keySignatureEvent.AbsoluteTime, Is.EqualTo(123));
            Assert.That(keySignatureEvent.CommandCode, Is.EqualTo(MidiCommandCode.MetaEvent));
            Assert.That(keySignatureEvent.MetaEventType, Is.EqualTo(MetaEventType.KeySignature));
            Assert.That(keySignatureEvent.SharpsFlats, Is.EqualTo(-3));
            Assert.That(keySignatureEvent.MajorMinor, Is.EqualTo(1));
        }

        [TestCase(-8)]
        [TestCase(8)]
        public void ConstructorRejectsSharpsFlatsOutOfRange(int sharpsFlats)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new KeySignatureEvent(sharpsFlats, 0, 0));
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void ConstructorRejectsMajorMinorOutOfRange(int majorMinor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new KeySignatureEvent(0, majorMinor, 0));
        }

        [Test]
        public void BinaryReaderConstructorRejectsInvalidLength()
        {
            using (var ms = new MemoryStream(new byte[] { 0x00, 0x00 }))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => new KeySignatureEvent(br, 1));
            }
        }

        [Test]
        public void BinaryReaderConstructorRejectsSharpsFlatsOutOfRange()
        {
            using (var ms = new MemoryStream(new byte[] { 0x08, 0x00 }))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => new KeySignatureEvent(br, 2));
            }
        }

        [Test]
        public void BinaryReaderConstructorRejectsMajorMinorOutOfRange()
        {
            using (var ms = new MemoryStream(new byte[] { 0x00, 0x02 }))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => new KeySignatureEvent(br, 2));
            }
        }

        [Test]
        public void ReadNextEventParsesKeySignatureMetaEvent()
        {
            var bytes = new byte[] { 0x00, 0xFF, 0x59, 0x02, 0x00, 0x00 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, null);

                Assert.That(midiEvent, Is.TypeOf<KeySignatureEvent>());
                var keySignatureEvent = (KeySignatureEvent)midiEvent;
                Assert.That(keySignatureEvent.DeltaTime, Is.EqualTo(0));
                Assert.That(keySignatureEvent.Channel, Is.EqualTo(1));
                Assert.That(keySignatureEvent.CommandCode, Is.EqualTo(MidiCommandCode.MetaEvent));
                Assert.That(keySignatureEvent.MetaEventType, Is.EqualTo(MetaEventType.KeySignature));
                Assert.That(keySignatureEvent.SharpsFlats, Is.EqualTo(0));
                Assert.That(keySignatureEvent.MajorMinor, Is.EqualTo(0));
            }
        }

        [Test]
        public void SharpsFlatsInterpretsSignedValueFromStream()
        {
            var bytes = new byte[] { 0x00, 0xFF, 0x59, 0x02, 0xF9, 0x01 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var keySignatureEvent = (KeySignatureEvent)MidiEvent.ReadNextEvent(br, null);

                Assert.That(keySignatureEvent.SharpsFlats, Is.EqualTo(-7));
                Assert.That(keySignatureEvent.MajorMinor, Is.EqualTo(1));
            }
        }

        [TestCase(-2, 0, "Bb major")]
        [TestCase(4, 1, "C# minor")]
        [TestCase(0, 0, "C major")]
        public void KeyNameReturnsExpectedMusicalKey(int sharpsFlats, int majorMinor, string expected)
        {
            var keySignatureEvent = new KeySignatureEvent(sharpsFlats, majorMinor, 0);

            Assert.That(keySignatureEvent.KeyName, Is.EqualTo(expected));
        }

        [Test]
        public void ExportWritesDeltaMetaTypeLengthAndData()
        {
            var keySignatureEvent = new KeySignatureEvent(-1, 1, 10);

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                keySignatureEvent.Export(ref absoluteTime, writer);

                Assert.That(absoluteTime, Is.EqualTo(10));
                Assert.That(ms.ToArray(), Is.EqualTo(new byte[] { 0x0A, 0xFF, 0x59, 0x02, 0xFF, 0x01 }));
            }
        }

        [Test]
        public void CloneCopiesAllProperties()
        {
            var keySignatureEvent = new KeySignatureEvent(4, 0, 55);

            var clone = (KeySignatureEvent)keySignatureEvent.Clone();

            Assert.That(clone, Is.Not.SameAs(keySignatureEvent));
            Assert.That(clone.AbsoluteTime, Is.EqualTo(keySignatureEvent.AbsoluteTime));
            Assert.That(clone.CommandCode, Is.EqualTo(keySignatureEvent.CommandCode));
            Assert.That(clone.MetaEventType, Is.EqualTo(keySignatureEvent.MetaEventType));
            Assert.That(clone.SharpsFlats, Is.EqualTo(keySignatureEvent.SharpsFlats));
            Assert.That(clone.MajorMinor, Is.EqualTo(keySignatureEvent.MajorMinor));
            Assert.That(clone.KeyName, Is.EqualTo(keySignatureEvent.KeyName));
        }

        [Test]
        public void ToStringIncludesMusicalKeyName()
        {
            var keySignatureEvent = new KeySignatureEvent(-2, 0, 0);

            var text = keySignatureEvent.ToString();

            Assert.That(text, Does.Contain("KeySignature"));
            Assert.That(text, Does.Contain("Bb major"));
        }
    }
}
