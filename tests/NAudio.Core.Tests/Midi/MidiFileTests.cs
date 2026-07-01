using System;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudio.Core.Tests.Midi;

[TestFixture]
[Category("UnitTest")]
public class MidiFileTests
{
    [Test]
    public void ConstructorRejectsMissingHeaderChunk()
    {
        var bytes = Encoding.ASCII.GetBytes("NOPE");
        using var stream = new MemoryStream(bytes);
        Assert.Throws<FormatException>(() => new MidiFile(stream, true));
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

        using var stream = new MemoryStream(bytes);
        Assert.Throws<FormatException>(() => new MidiFile(stream, true));
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

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, true);

        Assert.That(midiFile.FileFormat, Is.EqualTo(0));
        Assert.That(midiFile.DeltaTicksPerQuarterNote, Is.EqualTo(480));
        Assert.That(midiFile.Tracks, Is.EqualTo(1));
        Assert.That(midiFile.Events[0].Count, Is.EqualTo(3));
        Assert.That(midiFile.Events[0][0].AbsoluteTime, Is.EqualTo(0));
        Assert.That(midiFile.Events[0][1].AbsoluteTime, Is.EqualTo(10));
        Assert.That(MidiEvent.IsEndTrack(midiFile.Events[0][2]), Is.True);
    }

    [Test]
    public void ReadsRiffRmidWrappedFile()
    {
        var track = new byte[]
        {
            0x00, 0x90, 0x3C, 0x64,
            0x0A, 0x80, 0x3C, 0x40,
            0x00, 0xFF, 0x2F, 0x00
        };
        var midi = CreateMidiFileBytes(0, 480, track);
        var rmid = WrapInRiffRmid(midi);

        using var stream = new MemoryStream(rmid);
        var midiFile = new MidiFile(stream, true);

        Assert.That(midiFile.FileFormat, Is.EqualTo(0));
        Assert.That(midiFile.DeltaTicksPerQuarterNote, Is.EqualTo(480));
        Assert.That(midiFile.Tracks, Is.EqualTo(1));
        Assert.That(midiFile.Events[0].Count, Is.EqualTo(3));
    }

    [Test]
    public void ReadsRiffRmidWithOtherChunksBeforeData()
    {
        var track = new byte[]
        {
            0x00, 0x90, 0x3C, 0x64,
            0x0A, 0x80, 0x3C, 0x40,
            0x00, 0xFF, 0x2F, 0x00
        };
        var midi = CreateMidiFileBytes(1, 120, track);
        // An odd-length leading chunk exercises RIFF word-alignment padding.
        var infoChunk = ("IART", Encoding.ASCII.GetBytes("abc"));
        var rmid = WrapInRiffRmid(midi, infoChunk);

        using var stream = new MemoryStream(rmid);
        var midiFile = new MidiFile(stream, true);

        Assert.That(midiFile.FileFormat, Is.EqualTo(1));
        Assert.That(midiFile.DeltaTicksPerQuarterNote, Is.EqualTo(120));
        Assert.That(midiFile.Events[0].Count, Is.EqualTo(3));
    }

    [Test]
    public void RejectsNonRmidRiffFile()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(4u);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Flush();
        }
        stream.Position = 0;
        Assert.Throws<FormatException>(() => new MidiFile(stream, true));
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

        using var stream = new MemoryStream(bytes);
        Assert.Throws<FormatException>(() => new MidiFile(stream, true));
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

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, false);

        Assert.That(midiFile.Events[0].Count, Is.EqualTo(2));
        Assert.That(midiFile.Events[0][0], Is.TypeOf<NoteOnEvent>());
        var noteOn = (NoteOnEvent)midiFile.Events[0][0];
        Assert.That(noteOn.OffEvent, Is.Null);
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

        using var stream = new MemoryStream(bytes);
        Assert.Throws<FormatException>(() => new MidiFile(stream, true));
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

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, false);
        Assert.That(midiFile.Events[0].Count, Is.EqualTo(3));
    }

    [Test]
    public void RunningStatusSurvivesAcrossMetaEvents()
    {
        // Issue #205: a meta event embedded between channel-voice messages must not
        // clobber running status, otherwise the next high-bit-clear byte gets reparsed
        // as a meta event type.
        var track = new byte[]
        {
            0x00, 0x90, 0x3C, 0x64,             // NoteOn ch1 note 0x3C vel 100 (sets running status)
            0x00, 0xFF, 0x01, 0x01, (byte)'x',  // Text meta event "x"
            0x00, 0x40, 0x64,                   // running-status NoteOn ch1 note 0x40 vel 100
            0x00, 0x3C, 0x00,                   // running-status NoteOn vel 0 (note off for 0x3C)
            0x00, 0x40, 0x00,                   // running-status NoteOn vel 0 (note off for 0x40)
            0x00, 0xFF, 0x2F, 0x00              // EndTrack
        };
        var bytes = CreateMidiFileBytes(0, 480, track);

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, true);

        Assert.That(midiFile.Events[0].Count, Is.EqualTo(6));
        Assert.That(midiFile.Events[0][0], Is.TypeOf<NoteOnEvent>());
        Assert.That(midiFile.Events[0][1], Is.TypeOf<TextEvent>());
        Assert.That(midiFile.Events[0][2], Is.TypeOf<NoteOnEvent>());

        var runningNoteOn = (NoteEvent)midiFile.Events[0][2];
        Assert.That(runningNoteOn.NoteNumber, Is.EqualTo(0x40));
        Assert.That(runningNoteOn.Velocity, Is.EqualTo(0x64));
        Assert.That(runningNoteOn.Channel, Is.EqualTo(1));
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

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, true);

        Assert.That(midiFile.Events[0][0].AbsoluteTime, Is.EqualTo(5));
        Assert.That(midiFile.Events[1][0].AbsoluteTime, Is.EqualTo(5));
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

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, true);

        Assert.That(midiFile.Events[0][0].AbsoluteTime, Is.EqualTo(5));
        Assert.That(midiFile.Events[1][0].AbsoluteTime, Is.EqualTo(5));
    }

    [Test]
    public void Type2FileTypeIsPreservedInEventCollection()
    {
        var track = new byte[]
        {
            0x00, 0xFF, 0x2F, 0x00
        };
        var bytes = CreateMidiFileBytes(2, 120, track);

        using var stream = new MemoryStream(bytes);
        var midiFile = new MidiFile(stream, true);
        Assert.That(midiFile.Events.MidiFileType, Is.EqualTo(2));
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

    [Test]
    public void ExportRoundTripsType1FileViaStream()
    {
        var events = new MidiEventCollection(1, 480);
        events.AddTrack();
        events.AddTrack();

        events[0].Add(new TextEvent("Conductor", MetaEventType.SequenceTrackName, 0));
        events[0].Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

        events[1].Add(new NoteOnEvent(0, 1, 60, 100, 10));
        events[1].Add(new NoteEvent(10, 1, MidiCommandCode.NoteOff, 60, 64));
        events[1].Add(new MetaEvent(MetaEventType.EndTrack, 0, 11));

        using var stream = new MemoryStream();
        MidiFile.Export(stream, events);

        // Byte-for-byte match with the file overload for the same events, so we can be
        // confident the stream path isn't quietly diverging (padding, encoding, etc.).
        var fileName = Path.Combine(Path.GetTempPath(), $"naudio-midifiletests-{Guid.NewGuid():N}.mid");
        try
        {
            MidiFile.Export(fileName, events);
            var fileBytes = File.ReadAllBytes(fileName);
            Assert.That(stream.ToArray(), Is.EqualTo(fileBytes));
        }
        finally
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }

        stream.Position = 0;
        var midiFile = new MidiFile(stream, true);
        Assert.That(midiFile.FileFormat, Is.EqualTo(1));
        Assert.That(midiFile.DeltaTicksPerQuarterNote, Is.EqualTo(480));
        Assert.That(midiFile.Tracks, Is.EqualTo(2));
        Assert.That(midiFile.Events[1].OfType<NoteOnEvent>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void ExportLeavesCallerStreamOpen()
    {
        var events = new MidiEventCollection(0, 120);
        events.AddTrack();
        events[0].Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

        var stream = new MemoryStream();
        MidiFile.Export(stream, events);

        // Verify by using the stream after Export returns.
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.CanWrite, Is.True);
        Assert.DoesNotThrow(() => stream.WriteByte(0));
        stream.Dispose();
    }

    [Test]
    public void ExportRejectsNonSeekableStream()
    {
        var events = new MidiEventCollection(0, 120);
        events.AddTrack();
        events[0].Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

        using var inner = new MemoryStream();
        using var stream = new NonSeekableWriteStream(inner);
        Assert.Throws<ArgumentException>(() => MidiFile.Export(stream, events));
    }

    [Test]
    public void ExportRejectsNonWritableStream()
    {
        var events = new MidiEventCollection(0, 120);
        events.AddTrack();
        events[0].Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

        using var stream = new MemoryStream(new byte[16], writable: false);
        Assert.Throws<ArgumentException>(() => MidiFile.Export(stream, events));
    }

    [Test]
    public void ExportRejectsNullArguments()
    {
        var events = new MidiEventCollection(0, 120);
        events.AddTrack();
        events[0].Add(new MetaEvent(MetaEventType.EndTrack, 0, 0));

        Assert.Throws<ArgumentNullException>(() => MidiFile.Export((Stream)null, events));
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => MidiFile.Export(stream, null));
    }

    // Minimal write-only, non-seekable Stream wrapper used to exercise the CanSeek check.
    private sealed class NonSeekableWriteStream : Stream
    {
        private readonly Stream inner;
        public NonSeekableWriteStream(Stream inner) { this.inner = inner; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }

    private static byte[] CreateMidiFileBytes(ushort format, ushort division, params byte[][] tracks)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
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

    private static byte[] WrapInRiffRmid(byte[] midi, params (string id, byte[] data)[] extraChunks)
    {
        using var body = new MemoryStream();
        void WriteChunk(string id, byte[] data)
        {
            body.Write(Encoding.ASCII.GetBytes(id), 0, 4);
            body.Write(BitConverter.GetBytes((uint)data.Length), 0, 4); // RIFF sizes are little-endian
            body.Write(data, 0, data.Length);
            if ((data.Length & 1) == 1)
            {
                body.WriteByte(0); // word-alignment pad byte
            }
        }

        body.Write(Encoding.ASCII.GetBytes("RMID"), 0, 4);
        foreach (var (id, data) in extraChunks)
        {
            WriteChunk(id, data);
        }
        WriteChunk("data", midi);

        var bodyBytes = body.ToArray();
        using var stream = new MemoryStream();
        stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes((uint)bodyBytes.Length), 0, 4);
        stream.Write(bodyBytes, 0, bodyBytes.Length);
        return stream.ToArray();
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
