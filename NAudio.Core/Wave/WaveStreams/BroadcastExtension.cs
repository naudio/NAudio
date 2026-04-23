using System;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Parsed contents of a Broadcast Wave Format <c>bext</c> chunk.
    /// See https://tech.ebu.ch/docs/tech/tech3285.pdf for the full specification.
    /// </summary>
    public sealed class BroadcastExtension
    {
        /// <summary>
        /// Helper: formats a <see cref="DateTime"/> into the <c>yyyy-MM-dd</c> form
        /// expected by <see cref="OriginationDate"/>.
        /// </summary>
        public static string FormatOriginationDate(DateTime date) => date.ToString("yyyy-MM-dd");

        /// <summary>
        /// Helper: formats a <see cref="DateTime"/> into the <c>HH:mm:ss</c> form
        /// expected by <see cref="OriginationTime"/>.
        /// </summary>
        public static string FormatOriginationTime(DateTime time) => time.ToString("HH:mm:ss");

        /// <summary>
        /// Description (up to 256 bytes in the file).
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Originator (up to 32 bytes in the file).
        /// </summary>
        public string Originator { get; init; }

        /// <summary>
        /// Originator Reference (up to 32 bytes in the file).
        /// </summary>
        public string OriginatorReference { get; init; }

        /// <summary>
        /// Origination date as stored in the file (10 ASCII characters — typically <c>yyyy-mm-dd</c>).
        /// </summary>
        public string OriginationDate { get; init; }

        /// <summary>
        /// Origination time as stored in the file (8 ASCII characters — typically <c>hh:mm:ss</c>).
        /// </summary>
        public string OriginationTime { get; init; }

        /// <summary>
        /// Time reference — sample count since midnight, little-endian 64-bit.
        /// </summary>
        public long TimeReference { get; init; }

        /// <summary>
        /// Version of the <c>bext</c> chunk (1 or 2). Version 2 includes the loudness fields.
        /// </summary>
        public ushort Version { get; init; }

        /// <summary>
        /// SMPTE UMID (up to 64 bytes).
        /// </summary>
        public string UniqueMaterialIdentifier { get; init; }

        /// <summary>
        /// Integrated loudness value in LUFS × 100. Null for version 1 chunks.
        /// </summary>
        public short? LoudnessValue { get; init; }

        /// <summary>
        /// Loudness range in LU × 100. Null for version 1 chunks.
        /// </summary>
        public short? LoudnessRange { get; init; }

        /// <summary>
        /// Maximum true peak level in dBTP × 100. Null for version 1 chunks.
        /// </summary>
        public short? MaxTruePeakLevel { get; init; }

        /// <summary>
        /// Maximum momentary loudness in LUFS × 100. Null for version 1 chunks.
        /// </summary>
        public short? MaxMomentaryLoudness { get; init; }

        /// <summary>
        /// Maximum short-term loudness in LUFS × 100. Null for version 1 chunks.
        /// </summary>
        public short? MaxShortTermLoudness { get; init; }

        /// <summary>
        /// Coding history (variable-length ASCII string at the end of the chunk).
        /// </summary>
        public string CodingHistory { get; init; }

        /// <summary>
        /// Serialises this instance into the body of a <c>bext</c> RIFF chunk
        /// (no chunk id / size header). Use with
        /// <see cref="WaveFileWriter.AddChunk(string, byte[], ChunkPosition)"/>.
        /// </summary>
        public byte[] ToChunkData()
        {
            ushort version = Version == 0 ? (ushort)1 : Version;
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(FixedAscii(Description, 256));
            w.Write(FixedAscii(Originator, 32));
            w.Write(FixedAscii(OriginatorReference, 32));
            w.Write(FixedAscii(OriginationDate, 10));
            w.Write(FixedAscii(OriginationTime, 8));
            w.Write(TimeReference);
            w.Write(version);
            w.Write(FixedAscii(UniqueMaterialIdentifier, 64));
            if (version >= 2)
            {
                w.Write(LoudnessValue ?? (short)0);
                w.Write(LoudnessRange ?? (short)0);
                w.Write(MaxTruePeakLevel ?? (short)0);
                w.Write(MaxMomentaryLoudness ?? (short)0);
                w.Write(MaxShortTermLoudness ?? (short)0);
                w.Write(new byte[180]); // reserved (v2)
            }
            else
            {
                w.Write(new byte[190]); // reserved (v1)
            }
            if (!string.IsNullOrEmpty(CodingHistory))
            {
                w.Write(Encoding.ASCII.GetBytes(CodingHistory));
                w.Write((byte)0);
            }
            return ms.ToArray();
        }

        private static byte[] FixedAscii(string s, int length)
        {
            var buffer = new byte[length];
            if (!string.IsNullOrEmpty(s))
            {
                var encoded = Encoding.ASCII.GetBytes(s);
                Array.Copy(encoded, buffer, Math.Min(encoded.Length, length));
            }
            return buffer;
        }
    }

    /// <summary>
    /// <see cref="IWaveChunkInterpreter{T}"/> that parses the <c>bext</c> (Broadcast Wave Format) chunk.
    /// Returns <c>null</c> if no <c>bext</c> chunk is present.
    /// </summary>
    public sealed class BextInterpreter : IWaveChunkInterpreter<BroadcastExtension>
    {
        /// <summary>
        /// Shared stateless instance.
        /// </summary>
        public static readonly BextInterpreter Instance = new BextInterpreter();

        /// <inheritdoc />
        public BroadcastExtension Interpret(WaveChunks chunks)
        {
            if (chunks == null) return null;
            var bextChunk = chunks.Find("bext");
            if (bextChunk == null) return null;

            var data = chunks.GetData(bextChunk);
            if (data.Length < 602) return null; // truncated chunk — not enough for v1 fixed fields

            var ascii = Encoding.ASCII;
            int offset = 0;
            string description = ReadFixedString(data, offset, 256, ascii); offset += 256;
            string originator = ReadFixedString(data, offset, 32, ascii); offset += 32;
            string originatorReference = ReadFixedString(data, offset, 32, ascii); offset += 32;
            string originationDate = ReadFixedString(data, offset, 10, ascii); offset += 10;
            string originationTime = ReadFixedString(data, offset, 8, ascii); offset += 8;
            long timeReference = BitConverter.ToInt64(data, offset); offset += 8;
            ushort version = BitConverter.ToUInt16(data, offset); offset += 2;
            string umid = ReadFixedString(data, offset, 64, ascii); offset += 64;

            short? loudnessValue = null, loudnessRange = null, maxTruePeak = null, maxMomentary = null, maxShortTerm = null;
            if (version >= 2 && data.Length >= offset + 10)
            {
                loudnessValue = BitConverter.ToInt16(data, offset);
                loudnessRange = BitConverter.ToInt16(data, offset + 2);
                maxTruePeak = BitConverter.ToInt16(data, offset + 4);
                maxMomentary = BitConverter.ToInt16(data, offset + 6);
                maxShortTerm = BitConverter.ToInt16(data, offset + 8);
            }
            // Skip version-dependent reserved area to reach the coding history.
            // v1: 190 reserved bytes follow the UMID. v2: 10 loudness + 180 reserved.
            int afterReserved = 256 + 32 + 32 + 10 + 8 + 8 + 2 + 64 + 190;
            string codingHistory = data.Length > afterReserved
                ? ReadNullTerminatedString(data, afterReserved, data.Length - afterReserved, ascii)
                : string.Empty;

            return new BroadcastExtension
            {
                Description = description,
                Originator = originator,
                OriginatorReference = originatorReference,
                OriginationDate = originationDate,
                OriginationTime = originationTime,
                TimeReference = timeReference,
                Version = version,
                UniqueMaterialIdentifier = umid,
                LoudnessValue = loudnessValue,
                LoudnessRange = loudnessRange,
                MaxTruePeakLevel = maxTruePeak,
                MaxMomentaryLoudness = maxMomentary,
                MaxShortTermLoudness = maxShortTerm,
                CodingHistory = codingHistory
            };
        }

        private static string ReadFixedString(byte[] data, int offset, int length, Encoding encoding)
        {
            int end = offset + length;
            int actualEnd = offset;
            while (actualEnd < end && data[actualEnd] != 0) actualEnd++;
            return encoding.GetString(data, offset, actualEnd - offset);
        }

        private static string ReadNullTerminatedString(byte[] data, int offset, int maxLength, Encoding encoding)
        {
            int end = Math.Min(offset + maxLength, data.Length);
            int actualEnd = offset;
            while (actualEnd < end && data[actualEnd] != 0) actualEnd++;
            return encoding.GetString(data, offset, actualEnd - offset);
        }
    }
}
