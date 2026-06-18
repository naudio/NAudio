using System;
using System.IO;
using System.Text;
using NAudio.Vst3;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Vst3
{
    /// <summary>
    /// Pure-format tests for the <c>.vstpreset</c> reader/writer. These exercise only the binary
    /// container (<see cref="Vst3Preset"/>) and need no real plug-in, so they run headlessly.
    /// </summary>
    [TestFixture]
    public class Vst3PresetTests
    {
        // A raw-TUID class id (32 hex chars) as the factory reports it, e.g. Vst3ClassInfo.ClassId.
        private const string SampleClassId = "78563412BC9AF0DE1122334455667788";

        [Test]
        public void Write_Then_Read_RoundTripsComponentAndController()
        {
            var component = new byte[] { 1, 2, 3, 4, 5 };
            var controller = new byte[] { 9, 8, 7 };

            using var ms = new MemoryStream();
            Vst3Preset.Write(ms, SampleClassId, component, controller);
            ms.Position = 0;

            var contents = Vst3Preset.Read(ms);

            Assert.That(contents.ClassId, Is.EqualTo(SampleClassId));
            Assert.That(contents.ComponentState, Is.EqualTo(component));
            Assert.That(contents.ControllerState, Is.EqualTo(controller));
            Assert.That(contents.MetaInfoXml, Is.Null);
        }

        [Test]
        public void Write_WithEmptyController_OmitsControllerChunk()
        {
            var component = new byte[] { 1, 2, 3 };

            using var ms = new MemoryStream();
            Vst3Preset.Write(ms, SampleClassId, component, ReadOnlySpan<byte>.Empty);
            ms.Position = 0;

            var contents = Vst3Preset.Read(ms);
            Assert.That(contents.ComponentState, Is.EqualTo(component));
            Assert.That(contents.ControllerState, Is.Null);
        }

        [Test]
        public void Write_WithMetaInfo_RoundTripsXml()
        {
            const string xml = "<MetaInfo><Attribute id=\"Name\" value=\"Test\"/></MetaInfo>";

            using var ms = new MemoryStream();
            Vst3Preset.Write(ms, SampleClassId, new byte[] { 1 }, ReadOnlySpan<byte>.Empty, xml);
            ms.Position = 0;

            var contents = Vst3Preset.Read(ms);
            Assert.That(contents.MetaInfoXml, Is.EqualTo(xml));
        }

        [Test]
        public void HeaderClassId_IsStoredInGuidStringForm()
        {
            // On Windows (COM_COMPATIBLE) the on-disk class id is the GUID mixed-endian rendering of
            // the raw TUID — the first three fields byte-swapped relative to the raw hex.
            using var ms = new MemoryStream();
            Vst3Preset.Write(ms, SampleClassId, new byte[] { 1 }, ReadOnlySpan<byte>.Empty);

            var bytes = ms.ToArray();
            var headerClassId = Encoding.ASCII.GetString(bytes, 8, 32); // after "VST3" + int32 version
            Assert.That(headerClassId, Is.EqualTo("123456789ABCDEF01122334455667788"));
        }

        [Test]
        public void ReadClassId_ReturnsRawTuidHex_WithoutReadingChunks()
        {
            using var ms = new MemoryStream();
            Vst3Preset.Write(ms, SampleClassId, new byte[] { 1, 2, 3 }, new byte[] { 4 });
            ms.Position = 0;

            Assert.That(Vst3Preset.ReadClassId(ms), Is.EqualTo(SampleClassId));
        }

        [Test]
        public void Read_RejectsNonPresetData()
        {
            using var ms = new MemoryStream(Encoding.ASCII.GetBytes("NOT A PRESET FILE AT ALL...."));
            Assert.Throws<InvalidDataException>(() => Vst3Preset.Read(ms));
        }

        [Test]
        public void Read_RejectsTruncatedHeader()
        {
            using var ms = new MemoryStream("VST3"u8.ToArray());
            Assert.Throws<InvalidDataException>(() => Vst3Preset.Read(ms));
        }

        [TestCase(-100L)]            // negative offset → would seek to a negative position
        [TestCase(long.MaxValue)]    // huge offset → startPos + offset + size would overflow Int64
        public void Read_RejectsCorruptChunkOffset_WithoutCrashing(long badOffset)
        {
            // A corrupt chunk offset must surface as InvalidDataException, not an unhandled
            // ArgumentOutOfRangeException (negative seek) or a wrapped-overflow over-read.
            using var ms = new MemoryStream();
            Vst3Preset.Write(ms, SampleClassId, new byte[] { 1, 2, 3 }, ReadOnlySpan<byte>.Empty);
            var bytes = ms.ToArray();

            // Header: "VST3"(4) + version(4) + classId(32) = 40, then the int64 chunk-list offset.
            long listOffset = BitConverter.ToInt64(bytes, 40);
            // First chunk entry sits at listOffset + "List"(4) + entryCount(4); its layout is
            // id(4) + offset(8) + size(8). Patch the offset field to the corrupt value.
            int firstEntryOffsetField = (int)listOffset + 8 + 4;
            BitConverter.GetBytes(badOffset).CopyTo(bytes, firstEntryOffsetField);

            using var corrupt = new MemoryStream(bytes);
            Assert.Throws<InvalidDataException>(() => Vst3Preset.Read(corrupt));
        }

        [Test]
        public void Write_RejectsBadClassId()
        {
            using var ms = new MemoryStream();
            Assert.Throws<ArgumentException>(() => Vst3Preset.Write(ms, "tooshort", new byte[] { 1 }, ReadOnlySpan<byte>.Empty));
        }

        [Test]
        public void Write_RespectsStreamStartOffset()
        {
            // A preset written at a non-zero stream position must still read back: offsets are
            // stored relative to the start of the preset, not absolute file positions.
            var component = new byte[] { 10, 20, 30 };
            using var ms = new MemoryStream();
            ms.Write(new byte[7], 0, 7); // leading padding
            var start = ms.Position;
            Vst3Preset.Write(ms, SampleClassId, component, ReadOnlySpan<byte>.Empty);

            ms.Position = start;
            var contents = Vst3Preset.Read(ms);
            Assert.That(contents.ComponentState, Is.EqualTo(component));
        }
    }
}
