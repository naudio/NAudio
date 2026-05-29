using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Builds in-memory SF2 binaries for the sampler tests. A focused subset of
    /// the SoundFont test helper in NAudio.Core.Tests (not shared because it
    /// lives in a different test assembly).
    /// </summary>
    internal static class SoundFontTestBuilder
    {
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

        private static byte[] PaddedName(string name, int totalLength = 20)
        {
            var bytes = new byte[totalLength];
            var encoded = Encoding.ASCII.GetBytes(name);
            Buffer.BlockCopy(encoded, 0, bytes, 0, Math.Min(encoded.Length, totalLength - 1));
            return bytes;
        }

        public static byte[] Chunk(string id, byte[] data)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes(id));
            bw.Write((uint)data.Length);
            bw.Write(data);
            // RIFF chunks are word-aligned
            if (data.Length % 2 != 0) bw.Write((byte)0);
            return ms.ToArray();
        }

        private static byte[] ListChunk(string type, params byte[][] subChunks)
        {
            var listData = Concat(Encoding.ASCII.GetBytes(type), Concat(subChunks));
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes("LIST"));
            bw.Write((uint)listData.Length);
            bw.Write(listData);
            return ms.ToArray();
        }

        /// <summary>A single generator record: 16-bit operator + 16-bit amount.</summary>
        public static byte[] Gen(ushort oper, ushort amount)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(oper);
            bw.Write(amount);
            return ms.ToArray();
        }

        private static byte[] Bag(ushort genNdx, ushort modNdx)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(genNdx);
            bw.Write(modNdx);
            return ms.ToArray();
        }

        private static byte[] Modulator()
        {
            return new byte[10];
        }

        private static byte[] PresetHeader(string name, ushort preset, ushort bank, ushort bagNdx)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(PaddedName(name));
            bw.Write(preset);
            bw.Write(bank);
            bw.Write(bagNdx);
            bw.Write((uint)0); // library
            bw.Write((uint)0); // genre
            bw.Write((uint)0); // morphology
            return ms.ToArray();
        }

        private static byte[] InstrumentRecord(string name, ushort bagNdx)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(PaddedName(name));
            bw.Write(bagNdx);
            return ms.ToArray();
        }

        private static byte[] SampleHeader(string name, uint start, uint end,
            uint startLoop, uint endLoop, uint sampleRate, byte originalPitch)
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
            bw.Write((sbyte)0);   // pitch correction
            bw.Write((ushort)0);  // sample link
            bw.Write((ushort)1);  // mono
            return ms.ToArray();
        }

        private static byte[] InfoList()
        {
            using var ifilData = new MemoryStream();
            using (var bw = new BinaryWriter(ifilData, Encoding.ASCII, true))
            {
                bw.Write((ushort)2);
                bw.Write((ushort)4);
            }
            return ListChunk("INFO",
                Chunk("ifil", ifilData.ToArray()),
                Chunk("INAM", Encoding.ASCII.GetBytes("Test\0\0")));
        }

        private static byte[] WrapSoundFont(byte[] infoList, byte[] sdtaList, byte[] pdtaList)
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
        /// Builds a single-preset, single-instrument, single-region SF2 with the
        /// caller-supplied igen chunk, one sample header, and the given sample data.
        /// </summary>
        public static NAudio.SoundFont.SoundFont BuildSingleRegion(byte[] sampleData, byte[] igen,
            uint sampleStart, uint sampleEnd, uint loopStart, uint loopEnd, uint sampleRate, byte originalPitch)
        {
            var phdr = Chunk("phdr", Concat(
                PresetHeader("P", 0, 0, 0),
                PresetHeader("EOP", 0, 0, 1)));
            var pbag = Chunk("pbag", Concat(Bag(0, 0), Bag(1, 0)));
            var pmod = Chunk("pmod", Modulator());
            var pgen = Chunk("pgen", Gen(41, 0)); // Instrument -> 0
            var inst = Chunk("inst", Concat(
                InstrumentRecord("I", 0),
                InstrumentRecord("EOI", 1)));
            var ibag = Chunk("ibag", Concat(Bag(0, 0), Bag(GenCount(igen), 0)));
            var imod = Chunk("imod", Modulator());
            var shdr = Chunk("shdr", Concat(
                SampleHeader("S", sampleStart, sampleEnd, loopStart, loopEnd, sampleRate, originalPitch),
                new byte[46]));

            var pdta = ListChunk("pdta", phdr, pbag, pmod, pgen, inst, ibag, imod, igen, shdr);
            var sdta = ListChunk("sdta", Chunk("smpl", sampleData));
            var sf2 = WrapSoundFont(InfoList(), sdta, pdta);
            return new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));
        }

        /// <summary>
        /// Builds an instrument with two zones sharing exclusiveClass 1, one on
        /// key 42 and one on key 46 (closed/open hi-hat style), both looped.
        /// </summary>
        public static NAudio.SoundFont.SoundFont BuildTwoRegionExclusiveClass(byte[] sampleData, uint sampleRate)
        {
            // zone 0: keyRange 42-42, exclusiveClass 1, loop, sampleID 0
            var zone0 = Concat(
                Gen(43, (ushort)((42 << 8) | 42)), // KeyRange
                Gen(57, 1),                         // ExclusiveClass
                Gen(54, 1),                         // loop
                Gen(58, 60),
                Gen(53, 0));
            // zone 1: keyRange 46-46, exclusiveClass 1, loop, sampleID 0
            var zone1 = Concat(
                Gen(43, (ushort)((46 << 8) | 46)),
                Gen(57, 1),
                Gen(54, 1),
                Gen(58, 60),
                Gen(53, 0));
            var igen = Chunk("igen", Concat(zone0, zone1));

            ushort zone0Gens = 5;
            ushort zone1Gens = 10;

            var phdr = Chunk("phdr", Concat(
                PresetHeader("P", 0, 0, 0),
                PresetHeader("EOP", 0, 0, 1)));
            var pbag = Chunk("pbag", Concat(Bag(0, 0), Bag(1, 0)));
            var pmod = Chunk("pmod", Modulator());
            var pgen = Chunk("pgen", Gen(41, 0));
            var inst = Chunk("inst", Concat(
                InstrumentRecord("I", 0),
                InstrumentRecord("EOI", 2)));
            var ibag = Chunk("ibag", Concat(Bag(0, 0), Bag(zone0Gens, 0), Bag(zone1Gens, 0)));
            var imod = Chunk("imod", Modulator());
            var shdr = Chunk("shdr", Concat(
                SampleHeader("S", 0, 4, 0, 4, sampleRate, 60),
                new byte[46]));

            var pdta = ListChunk("pdta", phdr, pbag, pmod, pgen, inst, ibag, imod, igen, shdr);
            var sdta = ListChunk("sdta", Chunk("smpl", sampleData));
            var sf2 = WrapSoundFont(InfoList(), sdta, pdta);
            return new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));
        }

        private static ushort GenCount(byte[] igenChunk)
        {
            // igenChunk = "igen" + uint32 size + records (4 bytes each)
            uint size = BitConverter.ToUInt32(igenChunk, 4);
            return (ushort)(size / 4);
        }
    }
}
