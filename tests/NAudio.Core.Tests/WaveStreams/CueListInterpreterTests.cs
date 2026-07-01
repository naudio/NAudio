using System;
using System.IO;
using System.Text;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
[Category("UnitTest")]
public class CueListInterpreterTests
{
    private static WaveFormat Format => new(8000, 16, 1);

    // Writes a WAV with the given cue+label pairs via the real writer, then opens it for read.
    private static WaveFileReader OpenWithCues(params (int pos, string label)[] cues)
    {
        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            foreach (var (pos, label) in cues) w.AddCue(pos, label);
            w.WriteSamples(new short[] { 1, 2, 3, 4 }, 0, 4);
        }
        ms.Position = 0;
        return new WaveFileReader(ms);
    }

    [Test]
    public void InstanceIsSingleton()
    {
        Assert.That(CueListInterpreter.Instance, Is.Not.Null);
        Assert.That(CueListInterpreter.Instance, Is.SameAs(CueListInterpreter.Instance));
    }

    [Test]
    public void ReturnsNullWhenNoCueChunk()
    {
        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.Chunks.Read(CueListInterpreter.Instance), Is.Null);
    }

    [Test]
    public void ReadsLabellessCueListWhenCuePresentButNoLabels()
    {
        // Orphan "cue " chunk (no companion LIST/adtl), e.g. unnamed markers from Wavosaur (#549).
        // The cue must still be returned, with an empty label.
        var cueBytes = BuildOrphanCueChunkBody(cueId: 1, samplePosition: 100);
        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddChunk("cue ", cueBytes, ChunkPosition.AfterData);
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(1));
        Assert.That(cues[0].Position, Is.EqualTo(100));
        Assert.That(cues[0].Label, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ReadsLabellessCueListWhenOnlyInfoListIsPresent()
    {
        // cue chunk + LIST/INFO (no adtl) — the interpreter must not mistake INFO for adtl,
        // but should still return the cue with an empty label rather than null (#549).
        var cueBytes = BuildOrphanCueChunkBody(cueId: 1, samplePosition: 100);
        var info = new InfoMetadata();
        info.Set("INAM", "Title");

        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddChunk("cue ", cueBytes, ChunkPosition.AfterData);
            w.WriteInfoMetadata(info);
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(1));
        Assert.That(cues[0].Position, Is.EqualTo(100));
        Assert.That(cues[0].Label, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ReadsCueListFromCueAndAdtlChunks()
    {
        using var reader = OpenWithCues((100, "Intro"), (200, "Verse"));
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(2));
        Assert.That(cues[0].Position, Is.EqualTo(100));
        Assert.That(cues[0].Label, Is.EqualTo("Intro"));
        Assert.That(cues[1].Position, Is.EqualTo(200));
        Assert.That(cues[1].Label, Is.EqualTo("Verse"));
    }

    [Test]
    public void PicksAdtlListWhenInfoAndAdtlCoexist()
    {
        // When a file has both INFO metadata and adtl cue labels the interpreter must still
        // find the adtl list.
        var info = new InfoMetadata();
        info.Set("INAM", "Title");

        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddCue(1000, "TheLabel");
            w.WriteInfoMetadata(info);
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(1));
        Assert.That(cues[0].Label, Is.EqualTo("TheLabel"));
    }

    [Test]
    public void AcceptsLowercaseListChunkId()
    {
        // RIFF treats LIST/list as the same chunk; our writer emits "LIST" but readers
        // must accept "list" too. Hand-built to exercise that.
        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddCue(42, "lower");
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        // rewrite the LIST id in place to lowercase
        var bytes = ms.ToArray();
        FourCcReplace(bytes, "LIST", "list");
        using var reader = new WaveFileReader(new MemoryStream(bytes));
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues[0].Label, Is.EqualTo("lower"));
    }

    [Test]
    public void RoundTripsLabelsWithNonAsciiCharacters()
    {
        using var reader = OpenWithCues(
            (100, "Björk"),
            (200, "Ω-section"),
            (300, "音楽"));
        var cues = reader.Chunks.ReadCueList();
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues[0].Label, Is.EqualTo("Björk"));
        Assert.That(cues[1].Label, Is.EqualTo("Ω-section"));
        Assert.That(cues[2].Label, Is.EqualTo("音楽"));
    }

    [Test]
    public void RoundTripsViaWaveFileWriter()
    {
        using var reader = OpenWithCues((1000, "Alpha"), (2000, "Beta"));
        var cues = reader.Chunks.ReadCueList();
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(2));
        Assert.That(cues[0].Position, Is.EqualTo(1000));
        Assert.That(cues[0].Label, Is.EqualTo("Alpha"));
        Assert.That(cues[1].Position, Is.EqualTo(2000));
        Assert.That(cues[1].Label, Is.EqualTo("Beta"));
    }

    [Test]
    public void ReadsCueLengthFromLtxtChunk()
    {
        // Hand-built WAV: two cue points, one carrying a region length via an ltxt sub-chunk
        // in the LIST/adtl. The writer doesn't emit ltxt itself, so we compose the chunks
        // and let the interpreter parse them back.
        var cueBytes = BuildCueChunkBody(
            (cueId: 1, samplePosition: 100),
            (cueId: 2, samplePosition: 200));
        var adtlBytes = BuildAdtlListChunkBody(
            (cueId: 1, label: "Point", sampleLength: null),
            (cueId: 2, label: "Region", sampleLength: 50));

        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddChunk("cue ", cueBytes, ChunkPosition.AfterData);
            w.AddChunk("LIST", adtlBytes, ChunkPosition.AfterData);
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(2));
        Assert.That(cues[0].Position, Is.EqualTo(100));
        Assert.That(cues[0].Label, Is.EqualTo("Point"));
        Assert.That(cues[0].Length, Is.Null);
        Assert.That(cues[1].Position, Is.EqualTo(200));
        Assert.That(cues[1].Label, Is.EqualTo("Region"));
        Assert.That(cues[1].Length, Is.EqualTo(50));
    }

    [Test]
    public void RoundTripsCueLengthViaWaveFileWriter()
    {
        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddCue(100, "Point");
            w.AddCue(200, "Region", 50);
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var cues = reader.Chunks.ReadCueList();
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(2));
        Assert.That(cues[0].Position, Is.EqualTo(100));
        Assert.That(cues[0].Label, Is.EqualTo("Point"));
        Assert.That(cues[0].Length, Is.Null);
        Assert.That(cues[1].Position, Is.EqualTo(200));
        Assert.That(cues[1].Label, Is.EqualTo("Region"));
        Assert.That(cues[1].Length, Is.EqualTo(50));
    }

    [Test]
    public void IgnoresLtxtChunkReferencingUnknownCueId()
    {
        // An ltxt referring to a cue id that isn't in the cue chunk should be silently
        // dropped rather than throwing or corrupting a neighbouring cue.
        var cueBytes = BuildCueChunkBody((cueId: 1, samplePosition: 100));
        var adtlBytes = BuildAdtlListChunkBody(
            (cueId: 1, label: "Real", sampleLength: null),
            (cueId: 999, label: null, sampleLength: 12345));

        var ms = new MemoryStream();
        using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
        {
            w.AddChunk("cue ", cueBytes, ChunkPosition.AfterData);
            w.AddChunk("LIST", adtlBytes, ChunkPosition.AfterData);
            w.WriteSamples(new short[] { 1, 2 }, 0, 2);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var cues = reader.Chunks.Read(CueListInterpreter.Instance);
        Assert.That(cues, Is.Not.Null);
        Assert.That(cues.Count, Is.EqualTo(1));
        Assert.That(cues[0].Label, Is.EqualTo("Real"));
        Assert.That(cues[0].Length, Is.Null);
    }

    // ---- helpers ------------------------------------------------------------

    /// <summary>
    /// Minimal "cue " chunk body: 1 cue point, no label. Used to exercise the "no adtl"
    /// branch of the interpreter (the real writer always emits matching adtl).
    /// </summary>
    private static byte[] BuildOrphanCueChunkBody(int cueId, int samplePosition)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(1);                                 // point count
        w.Write(cueId);                             // dwIdentifier
        w.Write(samplePosition);                    // dwPosition
        w.Write(ChunkIdentifier.ChunkIdentifierToInt32("data")); // fccChunk
        w.Write(0);                                 // dwChunkStart
        w.Write(0);                                 // dwBlockStart
        w.Write(samplePosition);                    // dwSampleOffset
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a <c>cue </c> chunk body with the given cue points. Uses the sample position
    /// as both <c>dwPosition</c> and <c>dwSampleOffset</c>, and points at the <c>data</c> chunk.
    /// </summary>
    private static byte[] BuildCueChunkBody(params (int cueId, int samplePosition)[] points)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(points.Length);
        foreach (var (cueId, samplePosition) in points)
        {
            w.Write(cueId);
            w.Write(samplePosition);
            w.Write(ChunkIdentifier.ChunkIdentifierToInt32("data"));
            w.Write(0);
            w.Write(0);
            w.Write(samplePosition);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a <c>LIST</c> chunk body of type <c>adtl</c> containing a <c>labl</c> sub-chunk
    /// for each entry, and an <c>ltxt</c> sub-chunk when <paramref name="entries"/> supplies
    /// a <c>sampleLength</c>. The writer only emits <c>labl</c>, so we build these by hand
    /// to exercise the reader's <c>ltxt</c> path.
    /// </summary>
    private static byte[] BuildAdtlListChunkBody(params (int cueId, string label, int? sampleLength)[] entries)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Encoding.UTF8.GetBytes("adtl"));
        foreach (var (cueId, label, sampleLength) in entries)
        {
            if (label != null)
            {
                var labelBytes = Encoding.UTF8.GetBytes(label);
                w.Write(ChunkIdentifier.ChunkIdentifierToInt32("labl"));
                w.Write(labelBytes.Length + 1 + 4); // dwIdentifier + text + NUL
                w.Write(cueId);
                w.Write(labelBytes);
                w.Write((byte)0);
                if ((labelBytes.Length + 1) % 2 == 1) w.Write((byte)0); // word-align
            }
            if (sampleLength.HasValue)
            {
                w.Write(ChunkIdentifier.ChunkIdentifierToInt32("ltxt"));
                w.Write(20); // dwIdentifier + dwSampleLength + dwPurpose + 4x Int16
                w.Write(cueId);
                w.Write(sampleLength.Value);
                w.Write(0);       // dwPurpose
                w.Write((short)0); // wCountry
                w.Write((short)0); // wLanguage
                w.Write((short)0); // wDialect
                w.Write((short)0); // wCodePage
            }
        }
        return ms.ToArray();
    }

    private static void FourCcReplace(byte[] bytes, string from, string to)
    {
        if (from.Length != 4 || to.Length != 4) throw new ArgumentException("FourCC must be 4 chars");
        for (int i = 0; i <= bytes.Length - 4; i++)
        {
            if (bytes[i] == from[0] && bytes[i + 1] == from[1] && bytes[i + 2] == from[2] && bytes[i + 3] == from[3])
            {
                bytes[i] = (byte)to[0];
                bytes[i + 1] = (byte)to[1];
                bytes[i + 2] = (byte)to[2];
                bytes[i + 3] = (byte)to[3];
                return;
            }
        }
        throw new InvalidOperationException($"Marker '{from}' not found");
    }
}
