using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NAudioTests.SoundFont
{
    /// <summary>
    /// Helper class for building binary SF2 test data.
    /// Constructs valid RIFF/sfbk structures that can be loaded by NAudio.SoundFont.SoundFont.
    /// </summary>
    static class SoundFontTestHelper
    {
        #region Low-level helpers

        public static byte[] Concat(params byte[][] arrays)
        {
            int total = 0;
            foreach (var a in arrays) total += a.Length;
            var result = new byte[total];
            int offset = 0;
            foreach (var a in arrays)
            {
                Buffer.BlockCopy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }
            return result;
        }

        /// <summary>
        /// Creates a fixed-length null-padded name field (used for preset/instrument/sample names).
        /// </summary>
        public static byte[] PaddedName(string name, int totalLength = 20)
        {
            var bytes = new byte[totalLength];
            var encoded = Encoding.ASCII.GetBytes(name);
            var len = Math.Min(encoded.Length, totalLength - 1);
            Buffer.BlockCopy(encoded, 0, bytes, 0, len);
            return bytes;
        }

        /// <summary>
        /// Creates a null-terminated string with even byte count (for INFO sub-chunk data).
        /// </summary>
        public static byte[] NullTerminatedEvenString(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s + "\0");
            if (bytes.Length % 2 != 0)
            {
                var padded = new byte[bytes.Length + 1];
                Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
                return padded;
            }
            return bytes;
        }

        /// <summary>
        /// Writes a RIFF chunk: 4-byte ID + uint32 size + data.
        /// </summary>
        public static byte[] Chunk(string id, byte[] data)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes(id));
            bw.Write((uint)data.Length);
            bw.Write(data);
            return ms.ToArray();
        }

        /// <summary>
        /// Writes a LIST chunk: "LIST" + uint32 size + 4-byte type + sub-chunk data.
        /// </summary>
        public static byte[] ListChunk(string type, params byte[][] subChunks)
        {
            var allSubChunks = Concat(subChunks);
            var typeBytes = Encoding.ASCII.GetBytes(type);
            var listData = Concat(typeBytes, allSubChunks);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes("LIST"));
            bw.Write((uint)listData.Length);
            bw.Write(listData);
            return ms.ToArray();
        }

        #endregion

        #region INFO sub-chunk helpers

        public static byte[] IfilChunk(ushort major = 2, ushort minor = 4)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(major);
            bw.Write(minor);
            return Chunk("ifil", ms.ToArray());
        }

        public static byte[] StringInfoChunk(string chunkId, string value)
        {
            return Chunk(chunkId, NullTerminatedEvenString(value));
        }

        public static byte[] IverChunk(ushort major, ushort minor)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(major);
            bw.Write(minor);
            return Chunk("iver", ms.ToArray());
        }

        /// <summary>
        /// Builds an INFO LIST chunk with mandatory and optional sub-chunks.
        /// </summary>
        public static byte[] BuildInfoList(ushort vMajor = 2, ushort vMinor = 4,
            string engine = "EMU8000", string bankName = "TestBnk",
            byte[][] extraSubChunks = null)
        {
            var subChunks = new List<byte[]>
            {
                IfilChunk(vMajor, vMinor),
                StringInfoChunk("isng", engine),
                StringInfoChunk("INAM", bankName)
            };
            if (extraSubChunks != null)
                subChunks.AddRange(extraSubChunks);
            return ListChunk("INFO", subChunks.ToArray());
        }

        /// <summary>
        /// Builds an INFO LIST chunk with all optional sub-chunks populated.
        /// </summary>
        public static byte[] BuildFullInfoList(ushort vMajor = 2, ushort vMinor = 4,
            string engine = "EMU8000", string bankName = "TestBnk",
            string dataRom = "TestROM", ushort romMajor = 1, ushort romMinor = 0,
            string creationDate = "2024-01-01", string author = "Test Author",
            string targetProduct = "Test Product", string copyright = "Copyright Test",
            string comments = "Test Comments", string tools = "TestTool")
        {
            // Ensure even-length strings by choosing values that are even with null terminator
            var subChunks = new List<byte[]>
            {
                IfilChunk(vMajor, vMinor),
                StringInfoChunk("isng", engine),
                StringInfoChunk("INAM", bankName),
                StringInfoChunk("irom", dataRom),
                IverChunk(romMajor, romMinor),
                StringInfoChunk("ICRD", creationDate),
                StringInfoChunk("IENG", author),
                StringInfoChunk("IPRD", targetProduct),
                StringInfoChunk("ICOP", copyright),
                StringInfoChunk("ICMT", comments),
                StringInfoChunk("ISFT", tools),
            };
            return ListChunk("INFO", subChunks.ToArray());
        }

        #endregion

        #region sdta helpers

        /// <summary>
        /// Builds an sdta LIST chunk containing a smpl sub-chunk with the given raw PCM data.
        /// </summary>
        public static byte[] BuildSdtaList(byte[] rawSampleData)
        {
            return ListChunk("sdta", Chunk("smpl", rawSampleData));
        }

        #endregion

        #region pdta record helpers

        public static byte[] PresetHeaderRecord(string name, ushort preset, ushort bank, ushort bagNdx,
            uint library = 0, uint genre = 0, uint morphology = 0)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(PaddedName(name));
            bw.Write(preset);
            bw.Write(bank);
            bw.Write(bagNdx);
            bw.Write(library);
            bw.Write(genre);
            bw.Write(morphology);
            return ms.ToArray();
        }

        public static byte[] InstrumentRecord(string name, ushort bagNdx)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(PaddedName(name));
            bw.Write(bagNdx);
            return ms.ToArray();
        }

        public static byte[] BagRecord(ushort genNdx, ushort modNdx)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(genNdx);
            bw.Write(modNdx);
            return ms.ToArray();
        }

        public static byte[] GeneratorRecord(ushort genOper, ushort amount)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(genOper);
            bw.Write(amount);
            return ms.ToArray();
        }

        public static byte[] ModulatorRecord(ushort srcOper = 0, ushort destOper = 0,
            short amount = 0, ushort amtSrcOper = 0, ushort transform = 0)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(srcOper);
            bw.Write(destOper);
            bw.Write(amount);
            bw.Write(amtSrcOper);
            bw.Write(transform);
            return ms.ToArray();
        }

        public static byte[] SampleHeaderRecord(string name, uint start, uint end,
            uint startLoop, uint endLoop, uint sampleRate, byte originalPitch,
            sbyte pitchCorrection, ushort sampleLink, ushort sfSampleType)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(PaddedName(name));
            bw.Write(start);
            bw.Write(end);
            bw.Write(startLoop);
            bw.Write(endLoop);
            bw.Write(sampleRate);
            bw.Write(originalPitch);
            bw.Write(pitchCorrection);
            bw.Write(sampleLink);
            bw.Write(sfSampleType);
            return ms.ToArray();
        }

        #endregion

        #region pdta list builders

        /// <summary>
        /// Builds a minimal pdta LIST with 1 preset, 1 instrument, 1 sample + terminals.
        /// </summary>
        public static byte[] BuildMinimalPdtaList(
            string presetName = "Piano", ushort presetNum = 0, ushort bank = 0,
            string instName = "Piano", string sampleName = "TestSample",
            uint sampleEnd = 3, uint sampleRate = 44100, byte originalPitch = 60)
        {
            var phdr = Chunk("phdr", Concat(
                PresetHeaderRecord(presetName, presetNum, bank, 0),
                PresetHeaderRecord("EOP", 0, 0, 1)
            ));
            var pbag = Chunk("pbag", Concat(
                BagRecord(0, 0),
                BagRecord(1, 0)
            ));
            var pmod = Chunk("pmod", ModulatorRecord());
            var pgen = Chunk("pgen", GeneratorRecord(41, 0)); // Instrument -> index 0
            var inst = Chunk("inst", Concat(
                InstrumentRecord(instName, 0),
                InstrumentRecord("EOI", 1)
            ));
            var ibag = Chunk("ibag", Concat(
                BagRecord(0, 0),
                BagRecord(1, 0)
            ));
            var imod = Chunk("imod", ModulatorRecord());
            var igen = Chunk("igen", GeneratorRecord(53, 0)); // SampleID -> index 0
            var shdr = Chunk("shdr", Concat(
                SampleHeaderRecord(sampleName, 0, sampleEnd, 0, sampleEnd, sampleRate,
                    originalPitch, 0, 0, 1),
                new byte[46] // EOS terminal
            ));

            return ListChunk("pdta", phdr, pbag, pmod, pgen, inst, ibag, imod, igen, shdr);
        }

        /// <summary>
        /// Builds a rich pdta LIST with 2 presets, 2 instruments, 2 samples, modulators, and key ranges.
        /// </summary>
        public static byte[] BuildRichPdtaList()
        {
            // phdr: Piano(preset=0, bank=0), Strings(preset=48, bank=0), EOP
            var phdr = Chunk("phdr", Concat(
                PresetHeaderRecord("Piano", 0, 0, 0),
                PresetHeaderRecord("Strings", 48, 0, 1),
                PresetHeaderRecord("EOP", 0, 0, 2)
            ));

            // pbag: 2 preset zones + terminal
            var pbag = Chunk("pbag", Concat(
                BagRecord(0, 0),  // Piano zone: gen[0], mod[0..)
                BagRecord(1, 0),  // Strings zone: gen[1], mod[0..)
                BagRecord(2, 0)   // terminal
            ));

            // pmod: terminal only (no preset-level modulators)
            var pmod = Chunk("pmod", ModulatorRecord());

            // pgen: 2 generators (one per preset zone)
            var pgen = Chunk("pgen", Concat(
                GeneratorRecord(41, 0),  // Instrument -> inst 0 (Piano)
                GeneratorRecord(41, 1)   // Instrument -> inst 1 (Strings)
            ));

            // inst: Piano, Strings, EOI
            var inst = Chunk("inst", Concat(
                InstrumentRecord("Piano", 0),
                InstrumentRecord("Strings", 1),
                InstrumentRecord("EOI", 2)
            ));

            // ibag: 2 instrument zones + terminal
            // Piano zone: 2 generators (KeyRange + SampleID), 1 modulator
            // Strings zone: 2 generators (KeyRange + SampleID), 0 modulators
            var ibag = Chunk("ibag", Concat(
                BagRecord(0, 0),  // Piano zone: gen[0..1], mod[0..0]
                BagRecord(2, 1),  // Strings zone: gen[2..3], mod[1..)
                BagRecord(4, 1)   // terminal
            ));

            // imod: 1 real modulator + terminal
            // Modulator: CC1 (Mod Wheel) -> InitialFilterCutoffFrequency, amount=1200
            // srcOper=0x0081: bits 0-6=1 (CC#1), bit 7=1 (MIDI CC), bits 8-15=0 (unipolar, positive, linear)
            var imod = Chunk("imod", Concat(
                ModulatorRecord(0x0081, 8, 1200, 0, 0),
                ModulatorRecord() // terminal
            ));

            // igen: 4 generators
            // KeyRange amount: low byte = lo key, high byte = hi key
            // Full range 0-127: amount = (127 << 8) | 0 = 0x7F00
            var igen = Chunk("igen", Concat(
                GeneratorRecord(43, 0x7F00),  // KeyRange 0-127 (Piano)
                GeneratorRecord(53, 0),       // SampleID -> sample 0
                GeneratorRecord(43, 0x7F00),  // KeyRange 0-127 (Strings)
                GeneratorRecord(53, 1)        // SampleID -> sample 1
            ));

            // shdr: PianoSample, StringSample, EOS
            var shdr = Chunk("shdr", Concat(
                SampleHeaderRecord("PianoSample", 0, 3, 1, 3, 44100, 60, -5, 0, 1),
                SampleHeaderRecord("StringSample", 4, 7, 5, 7, 22050, 72, 10, 0, 1),
                new byte[46] // EOS terminal
            ));

            return ListChunk("pdta", phdr, pbag, pmod, pgen, inst, ibag, imod, igen, shdr);
        }

        #endregion

        #region Complete SF2 builders

        /// <summary>
        /// Builds a complete RIFF/sfbk file from pre-built INFO, sdta, and pdta LIST chunks.
        /// </summary>
        public static byte[] BuildSoundFont(byte[] infoList, byte[] sdtaList, byte[] pdtaList)
        {
            var riffData = Concat(Encoding.ASCII.GetBytes("sfbk"), infoList, sdtaList, pdtaList);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write((uint)riffData.Length);
            bw.Write(riffData);
            return ms.ToArray();
        }

        /// <summary>
        /// Builds a minimal valid SF2 file with 1 preset, 1 instrument, 1 sample.
        /// </summary>
        public static byte[] BuildMinimalSoundFont(
            byte[] sampleData = null,
            ushort vMajor = 2, ushort vMinor = 4,
            string bankName = "TestBnk",
            string presetName = "Piano",
            string instName = "Piano",
            string sampleName = "TestSample")
        {
            if (sampleData == null)
                sampleData = new byte[] { 0x00, 0x10, 0x00, 0x20, 0x00, 0x30, 0x00, 0x40 };

            var info = BuildInfoList(vMajor, vMinor, bankName: bankName);
            var sdta = BuildSdtaList(sampleData);
            var pdta = BuildMinimalPdtaList(
                presetName: presetName,
                instName: instName,
                sampleName: sampleName,
                sampleEnd: (uint)(sampleData.Length / 2 - 1));

            return BuildSoundFont(info, sdta, pdta);
        }

        /// <summary>
        /// Builds a rich SF2 file with 2 presets, 2 instruments, 2 samples, modulators, key ranges.
        /// </summary>
        public static byte[] BuildRichSoundFont()
        {
            var sampleData = new byte[]
            {
                0x00, 0x10, 0x00, 0x20, 0x00, 0x30, 0x00, 0x40, // sample 0: 4 points
                0x00, 0x50, 0x00, 0x60, 0x00, 0x70, 0x00, 0x80  // sample 1: 4 points
            };

            var info = BuildFullInfoList(bankName: "RichBank");
            var sdta = BuildSdtaList(sampleData);
            var pdta = BuildRichPdtaList();

            return BuildSoundFont(info, sdta, pdta);
        }

        #endregion
    }
}
