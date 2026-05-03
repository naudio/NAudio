using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.FileFormats.Wav
{
    /// <summary>
    /// Reader of RIFF chunks from a WAV file
    /// </summary>
    public class WaveFileChunkReader
    {
        private WaveFormat waveFormat;
        private long dataChunkPosition;
        private long dataChunkLength;
        private List<RiffChunk> riffChunks;
        private readonly bool strictMode;
        private bool isRf64;
        private readonly bool storeAllChunks;
        private long riffSize;

        /// <summary>
        /// Gets the size, in bytes, of a RIFF chunk.
        /// </summary>
        public const int ChunkHeaderSize = 8;

        /// <summary>
        /// Creates a new WaveFileChunkReader
        /// </summary>
        public WaveFileChunkReader()
        {
            storeAllChunks = true;
            strictMode = false;
        }

        /// <summary>
        /// Read the WAV header
        /// </summary>
        public void ReadWaveHeader(Stream stream)
        {
            this.dataChunkPosition = -1;
            this.waveFormat = null;
            this.riffChunks = new List<RiffChunk>();
            this.dataChunkLength = 0;

            ReadRiffHeader(stream);
            this.riffSize = StreamUtils.ReadUIntLittleEndian(stream); // read the file size (minus 8 bytes)

            if (StreamUtils.ReadIntLittleEndian(stream) != ChunkIdentifier.ChunkIdentifierToInt32("WAVE"))
            {
                throw new FormatException("Not a WAVE file - no WAVE header");
            }

            if (isRf64)
            {
                ReadDs64Chunk(stream);
            }

            // sometimes a file has more data than is specified after the RIFF header
            long stopPosition = Math.Min(riffSize + ChunkHeaderSize, stream.Length);

            // this -8 is so we can be sure that there are at least 8 bytes for a chunk id and length
            while (stream.Position <= stopPosition - ChunkHeaderSize)
            {
                Int32 chunkIdentifier = StreamUtils.ReadIntLittleEndian(stream);
                var chunkLength = StreamUtils.ReadUIntLittleEndian(stream);
                if (chunkIdentifier == ChunkIdentifier.DataChunkIdentifier)
                {
                    dataChunkPosition = stream.Position;
                    if (!isRf64) // we already know the dataChunkLength if this is an RF64 file
                    {
                        dataChunkLength = chunkLength;
                    }
                    stream.Position += dataChunkLength;
                }
                else if (chunkIdentifier == ChunkIdentifier.FormatChunkIdentifier)
                {
                    if (chunkLength > Int32.MaxValue)
                         throw new InvalidDataException(string.Format("Format chunk length must be between 0 and {0}.", Int32.MaxValue));
                    waveFormat = WaveFormat.FromFormatChunk(new BinaryReader(stream), (int)chunkLength);
                }
                else
                {
                    // check for invalid chunk length
                    if (chunkLength > stream.Length - stream.Position)
                    {
                        if (strictMode)
                        {
                            Debug.Assert(false, String.Format("Invalid chunk length {0}, pos: {1}. length: {2}",
                                chunkLength, stream.Position, stream.Length));
                        }
                        // an exception will be thrown further down if we haven't got a format and data chunk yet,
                        // otherwise we will tolerate this file despite it having corrupt data at the end
                        break;
                    }
                    if (storeAllChunks)
                    {
                        if (chunkLength > Int32.MaxValue)
                            throw new InvalidDataException(string.Format("RiffChunk chunk length must be between 0 and {0}.", Int32.MaxValue));
                        riffChunks.Add(GetRiffChunk(stream, chunkIdentifier, (int)chunkLength));
                    }
                    stream.Position += chunkLength;
                }

                // All Chunks have to be word aligned.
                // https://www.tactilemedia.com/info/MCI_Control_Info.html
                // "If the chunk size is an odd number of bytes, a pad byte with value zero is
                //  written after ckData. Word aligning improves access speed (for chunks resident in memory)
                //  and maintains compatibility with EA IFF. The ckSize value does not include the pad byte."
                if ((chunkLength % 2) != 0)
                {
                    stream.Position += (stream.ReadByte() == 0) ? 1 : 0;
                }
            }

            if (waveFormat == null)
            {
                throw new FormatException("Invalid WAV file - No fmt chunk found");
            }
            if (dataChunkPosition == -1)
            {
                throw new FormatException("Invalid WAV file - No data chunk found");
            }
        }

        /// <summary>
        /// See <see href="https://tech.ebu.ch/docs/tech/tech3306v1_0.pdf"/> for more information.
        /// </summary>
        private void ReadDs64Chunk(Stream stream)
        {
            int chunkId = StreamUtils.ReadIntLittleEndian(stream);
            if (chunkId != ChunkIdentifier.DS64ChunkIdentifier)
            {
                throw new FormatException("Invalid RF64 WAV file - No ds64 chunk found");
            }
            uint chunkSize = StreamUtils.ReadUIntLittleEndian(stream); // Chunk size is uint, not int. See A.2 section, page 12.
            this.riffSize = StreamUtils.ReadLongLittleEndian(stream);
            this.dataChunkLength = StreamUtils.ReadLongLittleEndian(stream);
            long sampleCount = StreamUtils.ReadLongLittleEndian(stream); // replaces the value in the fact chunk
            StreamUtils.ReadBytes(stream, (int)(chunkSize - 24L)); // get to the end of this chunk (should parse extra stuff later)
        }

        private static RiffChunk GetRiffChunk(Stream stream, Int32 chunkIdentifier, Int32 chunkLength)
        {
            return new RiffChunk(chunkIdentifier, chunkLength, stream.Position);
        }

        private void ReadRiffHeader(Stream stream)
        {
            int header = StreamUtils.ReadIntLittleEndian(stream);
            if (header == ChunkIdentifier.ChunkIdentifierToInt32("RF64"))
            {
                this.isRf64 = true;
            }
            else if (header != ChunkIdentifier.ChunkIdentifierToInt32("RIFF"))
            {
                throw new FormatException("Not a WAVE file - no RIFF header");
            }
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat { get { return this.waveFormat; } }

        /// <summary>
        /// Data Chunk Position
        /// </summary>
        public long DataChunkPosition { get { return this.dataChunkPosition; } }

        /// <summary>
        /// Data Chunk Length
        /// </summary>
        public long DataChunkLength { get { return this.dataChunkLength; } }

        /// <summary>
        /// Riff Chunks
        /// </summary>
        public List<RiffChunk> RiffChunks { get { return this.riffChunks; } }
    }
}
