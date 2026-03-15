using System;
using System.IO;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class MidiEventTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(0x7F)]
        [TestCase(0x80)]
        [TestCase(0x3FFF)]
        [TestCase(0x4000)]
        [TestCase(0x1FFFFF)]
        [TestCase(0x200000)]
        [TestCase(0x0FFFFFFF)]
        public void VarIntRoundTrip(int value)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                MidiEvent.WriteVarInt(writer, value);
                ms.Position = 0;
                using (var reader = new BinaryReader(ms))
                {
                    Assert.That(MidiEvent.ReadVarInt(reader), Is.EqualTo(value));
                }
            }
        }

        [Test]
        public void WriteVarIntRejectsNegativeValues()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => MidiEvent.WriteVarInt(writer, -1));
            }
        }

        [Test]
        public void WriteVarIntRejectsTooLargeValues()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => MidiEvent.WriteVarInt(writer, 0x10000000));
            }
        }

        [Test]
        public void ReadVarIntRejectsInvalidEncoding()
        {
            using (var ms = new MemoryStream(new byte[] { 0x80, 0x80, 0x80, 0x80 }))
            using (var reader = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => MidiEvent.ReadVarInt(reader));
            }
        }

        [Test]
        public void ConstructorSetsProperties()
        {
            var midiEvent = new MidiEvent(123, 2, MidiCommandCode.NoteOff);

            Assert.That(midiEvent.AbsoluteTime, Is.EqualTo(123));
            Assert.That(midiEvent.Channel, Is.EqualTo(2));
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.NoteOff));
            Assert.That(midiEvent.DeltaTime, Is.EqualTo(0));
        }

        [Test]
        public void ChannelRejectsValuesOutOfRange()
        {
            var midiEvent = new MidiEvent(0, 1, MidiCommandCode.NoteOff);

            Assert.Throws<ArgumentOutOfRangeException>(() => midiEvent.Channel = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => midiEvent.Channel = 17);
        }

        [Test]
        public void BaseGetAsShortMessageUsesCommandAndChannel()
        {
            var midiEvent = new MidiEvent(0, 3, MidiCommandCode.ControlChange);
            Assert.That(midiEvent.GetAsShortMessage(), Is.EqualTo(0xB2));
        }

        [Test]
        public void IsNoteOnAndIsNoteOffHandleVelocityConvention()
        {
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);
            var noteOnWithZeroVelocity = new NoteOnEvent(0, 1, 60, 0, 10);
            var noteOff = new NoteEvent(0, 1, MidiCommandCode.NoteOff, 60, 64);

            Assert.That(MidiEvent.IsNoteOn(noteOn), Is.True);
            Assert.That(MidiEvent.IsNoteOff(noteOn), Is.False);

            Assert.That(MidiEvent.IsNoteOn(noteOnWithZeroVelocity), Is.False);
            Assert.That(MidiEvent.IsNoteOff(noteOnWithZeroVelocity), Is.True);

            Assert.That(MidiEvent.IsNoteOff(noteOff), Is.True);
            Assert.That(MidiEvent.IsNoteOn(noteOff), Is.False);
        }

        [Test]
        public void IsNoteHelpersReturnFalseForNull()
        {
            Assert.That(MidiEvent.IsNoteOn(null), Is.False);
            Assert.That(MidiEvent.IsNoteOff(null), Is.False);
        }

        [Test]
        public void IsEndTrackReturnsTrueOnlyForEndTrackMetaEvent()
        {
            var endTrack = new MetaEvent(MetaEventType.EndTrack, 0, 0);
            var otherMeta = new MetaEvent(MetaEventType.TextEvent, 0, 0);
            var noteOn = new NoteOnEvent(0, 1, 60, 100, 10);

            Assert.That(MidiEvent.IsEndTrack(endTrack), Is.True);
            Assert.That(MidiEvent.IsEndTrack(otherMeta), Is.False);
            Assert.That(MidiEvent.IsEndTrack(noteOn), Is.False);
            Assert.That(MidiEvent.IsEndTrack(null), Is.False);
        }

        [Test]
        public void FromRawMessageParsesNoteOn()
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage(0x92, 60, 100));

            Assert.That(midiEvent, Is.TypeOf<NoteOnEvent>());
            var noteOn = (NoteOnEvent)midiEvent;
            Assert.That(noteOn.Channel, Is.EqualTo(3));
            Assert.That(noteOn.NoteNumber, Is.EqualTo(60));
            Assert.That(noteOn.Velocity, Is.EqualTo(100));
        }

        [Test]
        public void FromRawMessageConvertsZeroVelocityNoteOnToNoteEvent()
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage(0x91, 60, 0));

            Assert.That(midiEvent, Is.TypeOf<NoteEvent>());
            Assert.That(midiEvent, Is.Not.TypeOf<NoteOnEvent>());
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.NoteOn));
            Assert.That(((NoteEvent)midiEvent).Velocity, Is.EqualTo(0));
        }

        [Test]
        public void FromRawMessageParsesControlChange()
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage(0xB4, (int)MidiController.Expression, 127));

            Assert.That(midiEvent, Is.TypeOf<ControlChangeEvent>());
            var control = (ControlChangeEvent)midiEvent;
            Assert.That(control.Channel, Is.EqualTo(5));
            Assert.That(control.Controller, Is.EqualTo(MidiController.Expression));
            Assert.That(control.ControllerValue, Is.EqualTo(127));
        }

        [Test]
        public void FromRawMessageParsesPatchChange()
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage(0xC0, 10, 0));

            Assert.That(midiEvent, Is.TypeOf<PatchChangeEvent>());
            var patch = (PatchChangeEvent)midiEvent;
            Assert.That(patch.Channel, Is.EqualTo(1));
            Assert.That(patch.Patch, Is.EqualTo(10));
        }

        [Test]
        public void FromRawMessageParsesChannelAfterTouch()
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage(0xD5, 12, 0));

            Assert.That(midiEvent, Is.TypeOf<ChannelAfterTouchEvent>());
            var afterTouch = (ChannelAfterTouchEvent)midiEvent;
            Assert.That(afterTouch.Channel, Is.EqualTo(6));
            Assert.That(afterTouch.AfterTouchPressure, Is.EqualTo(12));
        }

        [Test]
        public void FromRawMessageParsesPitchWheelChange()
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage(0xE1, 0x7D, 0x40));

            Assert.That(midiEvent, Is.TypeOf<PitchWheelChangeEvent>());
            var pitch = (PitchWheelChangeEvent)midiEvent;
            Assert.That(pitch.Channel, Is.EqualTo(2));
            Assert.That(pitch.Pitch, Is.EqualTo(0x207D));
        }

        [TestCase(MidiCommandCode.TimingClock)]
        [TestCase(MidiCommandCode.StartSequence)]
        [TestCase(MidiCommandCode.ContinueSequence)]
        [TestCase(MidiCommandCode.StopSequence)]
        [TestCase(MidiCommandCode.AutoSensing)]
        public void FromRawMessageParsesSystemRealtimeMessages(MidiCommandCode commandCode)
        {
            var midiEvent = MidiEvent.FromRawMessage(CreateRawMessage((int)commandCode, 0, 0));

            Assert.That(midiEvent, Is.TypeOf<MidiEvent>());
            Assert.That(midiEvent.CommandCode, Is.EqualTo(commandCode));
            Assert.That(midiEvent.Channel, Is.EqualTo(1));
        }

        [TestCase(MidiCommandCode.MetaEvent)]
        [TestCase(MidiCommandCode.Sysex)]
        [TestCase(MidiCommandCode.Eox)]
        public void FromRawMessageRejectsUnsupportedSystemMessages(MidiCommandCode commandCode)
        {
            Assert.Throws<FormatException>(() => MidiEvent.FromRawMessage(CreateRawMessage((int)commandCode, 0, 0)));
        }

        [Test]
        public void ReadNextEventParsesStatusByteEvent()
        {
            var bytes = new byte[] { 0x00, 0x91, 0x3C, 0x64 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, null);

                Assert.That(midiEvent, Is.TypeOf<NoteOnEvent>());
                var noteOn = (NoteOnEvent)midiEvent;
                Assert.That(noteOn.DeltaTime, Is.EqualTo(0));
                Assert.That(noteOn.Channel, Is.EqualTo(2));
                Assert.That(noteOn.NoteNumber, Is.EqualTo(60));
                Assert.That(noteOn.Velocity, Is.EqualTo(100));
            }
        }

        [Test]
        public void ReadNextEventParsesRunningStatusEvent()
        {
            var previous = new NoteOnEvent(0, 2, 1, 1, 0);
            var bytes = new byte[] { 0x00, 0x3D, 0x40 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, previous);

                Assert.That(midiEvent, Is.TypeOf<NoteOnEvent>());
                var noteOn = (NoteOnEvent)midiEvent;
                Assert.That(noteOn.Channel, Is.EqualTo(2));
                Assert.That(noteOn.NoteNumber, Is.EqualTo(61));
                Assert.That(noteOn.Velocity, Is.EqualTo(64));
                Assert.That(noteOn.CommandCode, Is.EqualTo(MidiCommandCode.NoteOn));
            }
        }

        [Test]
        public void ReadNextEventWithRunningStatusAndNoPreviousThrows()
        {
            var bytes = new byte[] { 0x00, 0x3C, 0x40 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<NullReferenceException>(() => MidiEvent.ReadNextEvent(br, null));
            }
        }

        [Test]
        public void ReadNextEventParsesMetaEvent()
        {
            var bytes = new byte[] { 0x00, 0xFF, (byte)MetaEventType.EndTrack, 0x00 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, null);

                Assert.That(midiEvent, Is.TypeOf<MetaEvent>());
                Assert.That(MidiEvent.IsEndTrack(midiEvent), Is.True);
                Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.MetaEvent));
            }
        }

        [Test]
        public void ReadNextEventParsesSysexEvent()
        {
            var bytes = new byte[] { 0x00, 0xF0, 0x01, 0x02, 0xF7 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, null);

                Assert.That(midiEvent, Is.TypeOf<SysexEvent>());
                Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.Sysex));
            }
        }

        [Test]
        public void ReadNextEventRejectsUnsupportedCommandCode()
        {
            var bytes = new byte[] { 0x00, 0xF1 };
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<FormatException>(() => MidiEvent.ReadNextEvent(br, null));
            }
        }

        [Test]
        public void ExportWritesDeltaAndStatusByte()
        {
            var midiEvent = new MidiEvent(240, 2, MidiCommandCode.NoteOn);
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long currentAbsolute = 120;
                midiEvent.Export(ref currentAbsolute, writer);

                Assert.That(currentAbsolute, Is.EqualTo(240));
                var bytes = ms.ToArray();
                Assert.That(bytes, Is.EqualTo(new byte[] { 0x78, 0x91 }));
            }
        }

        [Test]
        public void ExportRejectsUnsortedEvents()
        {
            var midiEvent = new MidiEvent(10, 1, MidiCommandCode.NoteOn);
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long currentAbsolute = 11;
                Assert.Throws<FormatException>(() => midiEvent.Export(ref currentAbsolute, writer));
            }
        }

        [Test]
        public void CloneCopiesValueProperties()
        {
            var source = new MidiEvent(50, 4, MidiCommandCode.StopSequence);
            var clone = source.Clone();

            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.AbsoluteTime, Is.EqualTo(source.AbsoluteTime));
            Assert.That(clone.Channel, Is.EqualTo(source.Channel));
            Assert.That(clone.CommandCode, Is.EqualTo(source.CommandCode));
            Assert.That(clone.DeltaTime, Is.EqualTo(source.DeltaTime));
        }

        [Test]
        public void ToStringIncludesChannelForChannelMessages()
        {
            var midiEvent = new MidiEvent(12, 3, MidiCommandCode.NoteOff);
            Assert.That(midiEvent.ToString(), Is.EqualTo("12 NoteOff Ch: 3"));
        }

        [Test]
        public void ToStringOmitsChannelForSystemMessages()
        {
            var midiEvent = new MidiEvent(12, 1, MidiCommandCode.Sysex);
            Assert.That(midiEvent.ToString(), Is.EqualTo("12 Sysex"));
        }

        private static int CreateRawMessage(int status, int data1, int data2)
        {
            return (status & 0xFF) | ((data1 & 0xFF) << 8) | ((data2 & 0xFF) << 16);
        }
    }
}
