using System;
using System.IO;
using NUnit.Framework;
using static NAudio.Core.Tests.SoundFont.SoundFontTestHelper;

namespace NAudio.Core.Tests.SoundFont;

/// <summary>
/// Corrupt SoundFonts must fail with <see cref="InvalidDataException"/> - not a huge
/// allocation, and not an IndexOutOfRange/Overflow/Argument exception that a caller
/// catching bad-file errors wouldn't expect.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class SoundFontMalformedTests
{
    private static NAudio.SoundFont.SoundFont Load(byte[] sf2) => new(new MemoryStream(sf2));

    private static byte[] WithPdta(byte[] phdr = null, byte[] pbag = null, byte[] pgen = null,
        byte[] inst = null, byte[] ibag = null, byte[] igen = null, byte[] shdr = null)
    {
        var pdta = ListChunk("pdta",
            phdr ?? Chunk("phdr", Concat(PresetHeaderRecord("P", 0, 0, 0), PresetHeaderRecord("EOP", 0, 0, 1))),
            pbag ?? Chunk("pbag", Concat(BagRecord(0, 0), BagRecord(1, 0))),
            Chunk("pmod", ModulatorRecord()),
            pgen ?? Chunk("pgen", GeneratorRecord(41, 0)),
            inst ?? Chunk("inst", Concat(InstrumentRecord("I", 0), InstrumentRecord("EOI", 1))),
            ibag ?? Chunk("ibag", Concat(BagRecord(0, 0), BagRecord(1, 0))),
            Chunk("imod", ModulatorRecord()),
            igen ?? Chunk("igen", GeneratorRecord(53, 0)),
            shdr ?? Chunk("shdr", Concat(SampleHeaderRecord("S", 0, 3, 0, 3, 44100, 60, 0, 0, 1), new byte[46])));
        return BuildSoundFont(BuildInfoList(), BuildSdtaList(new byte[8]), pdta);
    }

    [Test]
    public void TheBaselineFileLoads()
    {
        Assert.DoesNotThrow(() => Load(WithPdta()));
    }

    [Test]
    public void ChunkSizeLargerThanTheFileIsRejectedWithoutAllocatingIt()
    {
        // an INFO sub-chunk claiming 2 GB, in a file of a few hundred bytes
        var sf2 = WithPdta();
        int unknown = FindChunk(sf2, "INAM");
        BitConverter.TryWriteBytes(sf2.AsSpan(unknown + 4), 0x7FFFFFF0u);
        Assert.Throws<InvalidDataException>(() => Load(sf2));
    }

    [Test]
    public void StructureChunkSizeLargerThanTheFileIsRejected()
    {
        var sf2 = WithPdta();
        int pbag = FindChunk(sf2, "pbag");
        BitConverter.TryWriteBytes(sf2.AsSpan(pbag + 4), 0xFFFFFFFCu);
        Assert.Throws<InvalidDataException>(() => Load(sf2));
    }

    [Test]
    public void PresetGeneratorReferringToAMissingInstrumentIsRejected()
    {
        Assert.Throws<InvalidDataException>(() => Load(WithPdta(pgen: Chunk("pgen", GeneratorRecord(41, 0xFFFF)))));
    }

    [Test]
    public void InstrumentGeneratorReferringToAMissingSampleIsRejected()
    {
        Assert.Throws<InvalidDataException>(() => Load(WithPdta(igen: Chunk("igen", GeneratorRecord(53, 0xFFFF)))));
    }

    [Test]
    public void DecreasingInstrumentZoneIndicesAreRejected()
    {
        var inst = Chunk("inst", Concat(InstrumentRecord("I", 1), InstrumentRecord("EOI", 0)));
        Assert.Throws<InvalidDataException>(() => Load(WithPdta(inst: inst)));
    }

    [Test]
    public void PresetZoneIndicesPastTheEndAreRejected()
    {
        var phdr = Chunk("phdr", Concat(PresetHeaderRecord("P", 0, 0, 0), PresetHeaderRecord("EOP", 0, 0, 5)));
        Assert.Throws<InvalidDataException>(() => Load(WithPdta(phdr: phdr)));
    }

    [Test]
    public void DecreasingBagGeneratorIndicesAreRejected()
    {
        var ibag = Chunk("ibag", Concat(BagRecord(1, 0), BagRecord(0, 0)));
        Assert.Throws<InvalidDataException>(() => Load(WithPdta(ibag: ibag)));
    }

    [Test]
    public void EmptySampleHeaderChunkIsRejected()
    {
        Assert.Throws<InvalidDataException>(() => Load(WithPdta(shdr: Chunk("shdr", Array.Empty<byte>()))));
    }

    private static int FindChunk(byte[] data, string id)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(id);
        int index = data.AsSpan().IndexOf(bytes);
        Assert.That(index, Is.GreaterThanOrEqualTo(0), $"{id} not found");
        return index;
    }
}
