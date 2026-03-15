using System;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class MidiFileTests
    {
        [Test]
        public void ConstructorRejectsMissingHeaderChunk()
        {
            var bytes = Encoding.ASCII.GetBytes("NOPE");
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws<FormatException>(() => new MidiFile(stream, true));
            }
        }

        [Test]
        public void ConstructorRejectsUnexpectedHeaderChunkLength()
        {
            var bytes = new byte[]
            {
                (byte)'M', (byte)'T', (byte)'h', (byte)'d',
                0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x60
            };

            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws<FormatException>(() => new MidiFile(stream, true));
            }
        }

        [Test]
        public void ReadsType0FileAndPopulatesBasicMetadata()
        {
            var track = new byte[]
            {
                0x00, 0x90, 0x3C, 0x64,
                0x0A, 0x80, 0x3C, 0x40,
                0x00, 0xFF, 0x2F, 0x00
            };
            var bytes = CreateMidiFileBytes(0, 480, track);

            using (var stream = new MemoryStream(bytes))
            {
                var midiFile = new MidiFile(stream, true);

                Assert.That(midiFile.FileFormat, Is.EqualTo(0));
                Assert.That(midiFile.DeltaTicksPerQuarterNote, Is.EqualTo(480));
                Assert.That(midiFile.Tracks, Is.EqualTo(1));
                Assert.That(midiFile.Events[0].Count, Is.EqualTo(3));
                Assert.That(midiFile.Events[0][0].AbsoluteTime, Is.EqualTo(0));
                Assert.That(midiFile.Events[0][1].AbsoluteTime, Is.EqualTo(10));
                Assert.That(MidiEvent.IsEndTrack(midiFile.Events[0][2]), Is.True);
            }
        }

        [Test]
        public void StrictCheckingRejectsUnmatchedNoteOn()
        {
            var track = new byte[]
            {
                0x00, 0x90, 0x3C, 0x64,
                0x00, 0xFF, 0x2F, 0x00
            };
            var bytes = CreateMidiFileBytes(0, 120, track);

            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws<FormatException>(() => new MidiFile(stream, true));
            }
        }

        [Test]
        public void NonStrictCheckingAllowsUnmatchedNoteOn()
        {
            var track = new byte[]
            {
                0x00, 0x90, 0x3C, 0x64,
                0x00, 0xFF, 0x2F, 0x00
            };
            var bytes = CreateMidiFileBytes(0, 120, track);

            using (var stream = new MemoryStream(bytes))
            {
                var midiFile = new MidiFile(stream, false);

                Assert.That(midiFile.Events[0].Count, Is.EqualTo(2));
                Assert.That(midiFile.Events[0][0], Is.TypeOf<NoteOnEvent>());
                var noteOn = (NoteOnEvent)midiFile.Events[0][0];
                Assert.That(noteOn.OffEvent, Is.Null);
            }
        }

        [Test]
        public void StrictCheckingRejectsEventsAfterEndTrack()
        {
            var track = new byte[]
            {
                0x00, 0x90, 0x3C, 0x64,
                0x00, 0xFF, 0x2F, 0x00,
                0x00, 0x80, 0x3C, 0x40
            };
            var bytes = CreateMidiFileBytes(0, 120, track);

            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws<FormatException>(() => new MidiFile(stream, true));
            }
        }

        [Test]
        public void NonStrictCheckingAllowsEventsAfterEndTrack()
        {
            var track = new byte[]
            {
                0x00, 0x90, 0x3C, 0x64,
                0x00, 0xFF, 0x2F, 0x00,
                0x00, 0x80, 0x3C, 0x40
            };
            var bytes = CreateMidiFileBytes(0, 120, track);

            using (var stream = new MemoryStream(bytes))
            {
                var midiFile = new MidiFile(stream, false);
                Assert.That(midiFile.Events[0].Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void Type1TracksUseIndependentAbsoluteTimeBases()
        {
            var track1 = new byte[]
            {
                0x05, 0x90, 0x3C, 0x64,
                0x05, 0x80, 0x3C, 0x40,
                0x00, 0xFF, 0x2F, 0x00
            };
            var track2 = new byte[]
            {
                0x05, 0x91, 0x40, 0x64,
                0x05, 0x81, 0x40, 0x40,
                0x00, 0xFF, 0x2F, 0x00
            };
            var bytes = CreateMidiFileBytes(1, 120, track1, track2);

            using (var stream = new MemoryStream(bytes))
            {
                var midiFile = new MidiFile(stream, true);

                Assert.That(midiFile.Events[0][0].AbsoluteTime, Is.EqualTo(5));
                Assert.That(midiFile.Events[1][0].AbsoluteTime, Is.EqualTo(5));
            }
        }

        [Test]
        public void Type2TracksUseIndependentAbsoluteTimeBases()
        {
            var track1 = new byte[]
            {
                0x05, 0x90, 0x3C, 0x64,
                0x05, 0x80, 0x3C, 0x40,
                0x00, 0xFF, 0x2F, 0x00
            };
            var track2 = new byte[]
            {
                0x05, 0x91, 0x40, 0x64,
                0x05, 0x81, 0x40, 0x40,
                0x00, 0xFF, 0x2F, 0x00
            };
            var bytes = CreateMidiFileBytes(2, 120, track1, track2);

            using (var stream = new MemoryStream(bytes))
            {
                var midiFile = new MidiFile(stream, true);

                Assert.That(midiFile.Events[0][0].AbsoluteTime, Is.EqualTo(5));
                Assert.That(midiFile.Events[1][0].AbsoluteTime, Is.EqualTo(5));
            }
        }

        [Test]
        public void Type2FileTypeIsPreservedInEventCollection()
        {
            var track = new byte[]
            {
                0x00, 0xFF, 0x2F, 0x00
            };
            var bytes = CreateMidiFileBytes(2, 120, track);

            using (var stream = new MemoryStream(bytes))
            {
                var midiFile = new MidiFile(stream, true);
                Assert.That(midiFile.Events.MidiFileType, Is.EqualTo(2));
            }
        }

        [Test]
        public void ExportRejectsType0CollectionWithMoreThanOneTrack()
        {
            var events = new MidiEventCollection(0, 120);
            events.AddTrack();
            events.AddTrack();

            var fileName = Path.Combine(Path.GetTempPath(), $"naudio-midifiletests-{Guid.NewGuid():N}.mid");
            try
            {
                Assert.Throws<ArgumentException>(() => MidiFile.Export(fileName, events));
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        [Test]
        public void ExportRoundTripsType1File()
        {
            var events = new MidiEventCollection(1, 480);
            events.AddTrack();
            events.AddTrack();

            events[0].Add(new TextEvent("Conductor", MetaEventType.SequenceTrackName, 0));
            events[0].Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

            events[1].Add(new NoteOnEvent(0, 1, 60, 100, 10));
            events[1].Add(new NoteEvent(10, 1, MidiCommandCode.NoteOff, 60, 64));
            events[1].Add(new MetaEvent(MetaEventType.EndTrack, 0, 11));

            var fileName = Path.Combine(Path.GetTempPath(), $"naudio-midifiletests-{Guid.NewGuid():N}.mid");
            try
            {
                MidiFile.Export(fileName, events);
                var midiFile = new MidiFile(fileName, true);

                Assert.That(midiFile.FileFormat, Is.EqualTo(1));
                Assert.That(midiFile.DeltaTicksPerQuarterNote, Is.EqualTo(480));
                Assert.That(midiFile.Tracks, Is.EqualTo(2));
                Assert.That(midiFile.Events[0].Any(MidiEvent.IsEndTrack), Is.True);
                Assert.That(midiFile.Events[1].Any(MidiEvent.IsEndTrack), Is.True);
                Assert.That(midiFile.Events[1].OfType<NoteOnEvent>().Count(), Is.EqualTo(1));
                Assert.That(midiFile.Events[1].Count(e => e.CommandCode == MidiCommandCode.NoteOff), Is.EqualTo(1));
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        private static byte[] CreateMidiFileBytes(ushort format, ushort division, params byte[][] tracks)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                WriteUInt32BigEndian(writer, 6);
                WriteUInt16BigEndian(writer, format);
                WriteUInt16BigEndian(writer, (ushort)tracks.Length);
                WriteUInt16BigEndian(writer, division);

                foreach (var track in tracks)
                {
                    writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                    WriteUInt32BigEndian(writer, (uint)track.Length);
                    writer.Write(track);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
        {
            writer.Write((byte)(value >> 8));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteUInt32BigEndian(BinaryWriter writer, uint value)
        {
            writer.Write((byte)(value >> 24));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }
    }
}
