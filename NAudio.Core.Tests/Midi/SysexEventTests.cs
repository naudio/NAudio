using System;
using System.IO;
using System.Reflection;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class SysexEventTests
    {
        [Test]
        public void Constructor_SetsAbsoluteTimeAndPayload()
        {
            var payload = new byte[] { 0x01, 0x02 };
            var sysex = new SysexEvent(123, payload);

            Assert.That(sysex.AbsoluteTime, Is.EqualTo(123));
            Assert.That(sysex.CommandCode, Is.EqualTo(MidiCommandCode.Sysex));
            Assert.That(sysex.Channel, Is.EqualTo(1));
            Assert.That(GetData(sysex), Is.EqualTo(payload));
        }

        [Test]
        public void Constructor_ClonesPayload()
        {
            var payload = new byte[] { 0x01, 0x02 };
            var sysex = new SysexEvent(0, payload);

            payload[0] = 0x7F;

            Assert.That(GetData(sysex), Is.EqualTo(new byte[] { 0x01, 0x02 }));
        }

        [Test]
        public void Constructor_RejectsNullPayload()
        {
            Assert.Throws<ArgumentNullException>(() => new SysexEvent(0, null));
        }

        [Test]
        public void ReadSysexEvent_ReadsDataUntilF7Terminator()
        {
            using (var ms = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0xF7 }))
            using (var br = new BinaryReader(ms))
            {
                var sysex = SysexEvent.ReadSysexEvent(br);

                var data = GetData(sysex);
                Assert.That(data, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
                Assert.That(ms.Position, Is.EqualTo(4));
            }
        }

        [Test]
        public void ReadNextEvent_ParsesSysexEventAndAssignsBaseFields()
        {
            using (var ms = new MemoryStream(new byte[] { 0x05, 0xF0, 0x10, 0x20, 0xF7 }))
            using (var br = new BinaryReader(ms))
            {
                var midiEvent = MidiEvent.ReadNextEvent(br, null);

                Assert.That(midiEvent, Is.TypeOf<SysexEvent>());
                Assert.That(midiEvent.DeltaTime, Is.EqualTo(5));
                Assert.That(midiEvent.Channel, Is.EqualTo(1));
                Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.Sysex));
                Assert.That(GetData((SysexEvent)midiEvent), Is.EqualTo(new byte[] { 0x10, 0x20 }));
            }
        }

        [Test]
        public void Export_WritesDeltaStatusDataAndTerminator()
        {
            var sysex = ReadViaMidiEvent(new byte[] { 0x01, 0x02 });
            sysex.AbsoluteTime = 10;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                sysex.Export(ref absoluteTime, writer);

                Assert.That(absoluteTime, Is.EqualTo(10));
                Assert.That(ms.ToArray(), Is.EqualTo(new byte[] { 0x0A, 0xF0, 0x01, 0x02, 0xF7 }));
            }
        }

        [Test]
        public void Clone_CopiesBaseProperties()
        {
            var sysex = ReadViaMidiEvent(new byte[] { 0x7D, 0x7E });
            sysex.AbsoluteTime = 42;
            sysex.Channel = 4;

            var clone = (SysexEvent)sysex.Clone();

            Assert.That(clone, Is.Not.SameAs(sysex));
            Assert.That(clone.AbsoluteTime, Is.EqualTo(sysex.AbsoluteTime));
            Assert.That(clone.Channel, Is.EqualTo(sysex.Channel));
            Assert.That(clone.CommandCode, Is.EqualTo(sysex.CommandCode));
            Assert.That(clone.DeltaTime, Is.EqualTo(sysex.DeltaTime));
        }

        [Test]
        public void Clone_DeepCopiesDataArray()
        {
            var sysex = ReadViaMidiEvent(new byte[] { 0x01, 0x02, 0x03 });
            var clone = (SysexEvent)sysex.Clone();

            var originalData = GetData(sysex);
            var cloneData = GetData(clone);

            Assert.That(cloneData, Is.EqualTo(originalData));
            Assert.That(cloneData, Is.Not.SameAs(originalData));
        }

        [Test]
        public void Export_IgnoresChannelForSysexStatusByte()
        {
            var sysex = ReadViaMidiEvent(new byte[] { 0x55 });
            sysex.Channel = 6;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                long absoluteTime = 0;
                sysex.Export(ref absoluteTime, writer);

                var bytes = ms.ToArray();
                Assert.That(bytes[1], Is.EqualTo(0xF0));
            }
        }

        [Test]
        public void ToString_HandlesEmptySysexEvent()
        {
            var sysex = new SysexEvent();

            Assert.DoesNotThrow(() => sysex.ToString());
        }

        private static SysexEvent ReadViaMidiEvent(byte[] data)
        {
            var bytes = new byte[2 + data.Length + 1];
            bytes[0] = 0x00;
            bytes[1] = 0xF0;
            Array.Copy(data, 0, bytes, 2, data.Length);
            bytes[bytes.Length - 1] = 0xF7;

            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                return (SysexEvent)MidiEvent.ReadNextEvent(br, null);
            }
        }

        private static byte[] GetData(SysexEvent sysex)
        {
            var field = typeof(SysexEvent).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            return (byte[])field.GetValue(sysex);
        }
    }
}
