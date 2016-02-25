using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.FileFormats.Wav
{
    class WaveFileChunkForwardOnlyReader
    {
        private WaveFormat waveFormat;
        private long dataChunkLength;
        private List<RiffChunkData> riffChunks;
        private bool isRf64;
        private readonly bool storeAllChunks;
        private long riffSize;

        public WaveFileChunkForwardOnlyReader(bool storeAllChunks)
        {
            this.storeAllChunks = storeAllChunks;
        }

        public void ReadWaveHeader(Stream stream)
        {
            this.waveFormat = null;
            this.riffChunks = new List<RiffChunkData>();
            this.dataChunkLength = 0;

            var br = new BinaryReader(stream);
            ReadRiffHeader(br);
            this.riffSize = br.ReadUInt32(); // read the file size (minus 8 bytes)

            if (br.ReadInt32() != ChunkIdentifier.ChunkIdentifierToInt32("WAVE"))
            {
                throw new FormatException("Not a WAVE file - no WAVE header");
            }

            if (isRf64)
            {
                ReadDs64Chunk(br);
            }

            int dataChunkId = ChunkIdentifier.ChunkIdentifierToInt32("data");
            int formatChunkId = ChunkIdentifier.ChunkIdentifierToInt32("fmt ");
            try
            {
                while (true)
                {
                    Int32 chunkIdentifier = br.ReadInt32();
                    var chunkLength = br.ReadUInt32();
                    if (chunkIdentifier == dataChunkId)
                    {
                        if (waveFormat == null)
                            throw new FormatException("Invalid WAV file - No fmt chunk found");

                        if (!isRf64) // we already know the dataChunkLength if this is an RF64 file
                        {
                            dataChunkLength = chunkLength;
                        }

                        // we have reached the data chunk, and for now we are not going to support reading 
                        // any extra Chunks after the data chunk, so we can just exit here.
                        return;
                    }
                    else if (chunkIdentifier == formatChunkId)
                    {
                        if (chunkLength > Int32.MaxValue)
                            throw new InvalidDataException(
                                string.Format("Format chunk length must be between 0 and {0}.", Int32.MaxValue));
                        waveFormat = WaveFormat.FromFormatChunk(br, (int)chunkLength);
                    }
                    else
                    {
                        if (chunkLength > Int32.MaxValue)
                            throw new InvalidDataException(
                                string.Format("RiffChunk chunk length must be between 0 and {0}.", Int32.MaxValue));
                        var data = GetRiffChunk(br, chunkIdentifier, (int)chunkLength);
                        if (storeAllChunks)
                        {
                            riffChunks.Add(data);
                        }
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new FormatException("Invalid WAV file - No data chunk found", ex);
            }
        }

        /// <summary>
        /// http://tech.ebu.ch/docs/tech/tech3306-2009.pdf
        /// </summary>
        private void ReadDs64Chunk(BinaryReader reader)
        {
            int ds64ChunkId = ChunkIdentifier.ChunkIdentifierToInt32("ds64");
            int chunkId = reader.ReadInt32();
            if (chunkId != ds64ChunkId)
            {
                throw new FormatException("Invalid RF64 WAV file - No ds64 chunk found");
            }
            int chunkSize = reader.ReadInt32();
            this.riffSize = reader.ReadInt64();
            this.dataChunkLength = reader.ReadInt64();
            long sampleCount = reader.ReadInt64(); // replaces the value in the fact chunk
            reader.ReadBytes(chunkSize - 24); // get to the end of this chunk (should parse extra stuff later)
        }

        private static RiffChunkData GetRiffChunk(BinaryReader br, Int32 chunkIdentifier, Int32 chunkLength)
        {
            return new RiffChunkData(chunkIdentifier, br.ReadBytes(chunkLength));
        }

        private void ReadRiffHeader(BinaryReader br)
        {
            int header = br.ReadInt32();
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
        /// Data Chunk Length
        /// </summary>
        public long DataChunkLength { get { return this.dataChunkLength; } }

        /// <summary>
        /// Riff Chunks
        /// </summary>
        public List<RiffChunkData> RiffChunks { get { return this.riffChunks; } }
    }
}
