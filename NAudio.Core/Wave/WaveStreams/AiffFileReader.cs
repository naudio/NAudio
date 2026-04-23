using System;
using System.IO;
using System.Collections.Generic;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>A read-only stream of AIFF data based on an aiff file
    /// with an associated WaveFormat
    /// originally contributed to NAudio by Giawa
    /// </summary>
    public class AiffFileReader : WaveStream
    {
        private readonly WaveFormat waveFormat;
        private readonly bool ownInput;
        private readonly long dataPosition;
        private readonly int dataChunkLength;
        private readonly List<AiffChunk> chunks = new List<AiffChunk>();
        private Stream waveStream;
        private readonly object lockObject = new object();

        /// <summary>Supports opening a AIF file</summary>
        /// <remarks>The AIF is of similar nastiness to the WAV format.
        /// This supports basic reading of uncompressed PCM AIF files,
        /// with 8, 16, 24 and 32 bit PCM data.
        /// </remarks>
        public AiffFileReader(String aiffFile) :
            this(File.OpenRead(aiffFile))
        {
            ownInput = true;
        }

        /// <summary>
        /// Creates an Aiff File Reader based on an input stream
        /// </summary>
        /// <param name="inputStream">The input stream containing a AIF file including header</param>
        public AiffFileReader(Stream inputStream)
        {
            waveStream = inputStream;
            ReadAiffHeader(waveStream, out waveFormat, out dataPosition, out dataChunkLength, chunks);
            Position = 0;
        }

        /// <summary>
        /// Ensures valid AIFF header and then finds data offset.
        /// </summary>
        /// <param name="stream">The stream, positioned at the start of audio data</param>
        /// <param name="format">The format found</param>
        /// <param name="dataChunkPosition">The position of the data chunk</param>
        /// <param name="dataChunkLength">The length of the data chunk</param>
        /// <param name="chunks">Additional chunks found</param>
        public static void ReadAiffHeader(Stream stream, out WaveFormat format, out long dataChunkPosition, out int dataChunkLength, List<AiffChunk> chunks)
        {
            dataChunkPosition = -1;
            format = null;
            BinaryReader br = new BinaryReader(stream);
            
            if (ReadChunkName(br) != "FORM")
            {
                throw new FormatException("Not an AIFF file - no FORM header.");
            }
            uint fileSize = ConvertInt(br.ReadBytes(4));
            string formType = ReadChunkName(br);
            if (formType != "AIFC" && formType != "AIFF")
            {
                throw new FormatException("Not an AIFF file - no AIFF/AIFC header.");
            }

            dataChunkLength = 0;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                AiffChunk nextChunk = ReadChunkHeader(br);
                if (nextChunk.ChunkName == "\0\0\0\0") break;

                if (br.BaseStream.Position + nextChunk.ChunkLength > br.BaseStream.Length)
                {
                    break;
                }
                if (nextChunk.ChunkName == "COMM")
                {
                    short numChannels = ConvertShort(br.ReadBytes(2));
                    uint numSampleFrames = ConvertInt(br.ReadBytes(4));
                    short sampleSize = ConvertShort(br.ReadBytes(2));
                    double sampleRate = IEEE.ConvertFromIeeeExtended(br.ReadBytes(10));

                    format = new WaveFormat((int)sampleRate, (int)sampleSize, (int)numChannels);

                    if (nextChunk.ChunkLength > 18 && formType == "AIFC")
                    {   
                        // In an AIFC file, the compression format is tacked on to the COMM chunk
                        string compress = new string(br.ReadChars(4)).ToLower();
                        if (compress != "none") throw new FormatException("Compressed AIFC is not supported.");
                        br.ReadBytes((int)nextChunk.ChunkLength - 22);
                    }
                    else br.ReadBytes((int)nextChunk.ChunkLength - 18);
                }
                else if (nextChunk.ChunkName == "SSND")
                {
                    uint offset = ConvertInt(br.ReadBytes(4));
                    uint blockSize = ConvertInt(br.ReadBytes(4));
                    dataChunkPosition = nextChunk.ChunkStart + 16 + offset;
                    dataChunkLength = (int)nextChunk.ChunkLength - 8;
                    br.BaseStream.Position += (nextChunk.ChunkLength - 8);
                }
                else
                {
                    if (chunks != null)
                    {
                        chunks.Add(nextChunk);
                    }
                    br.BaseStream.Position += nextChunk.ChunkLength;
                }

                
            }

            if (format == null)
            {
                throw new FormatException("Invalid AIFF file - No COMM chunk found.");
            }
            if (dataChunkPosition == -1)
            {
                throw new FormatException("Invalid AIFF file - No SSND chunk found.");
            }
        }

        /// <summary>
        /// Cleans up the resources associated with this AiffFileReader
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
                if (waveStream != null)
                {
                    // only dispose our source if we created it
                    if (ownInput)
                    {
                        waveStream.Dispose();
                    }
                    waveStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "AiffFileReader was not disposed");
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override long Length => dataChunkLength;

        /// <summary>
        /// Number of Samples (if possible to calculate)
        /// </summary>
        public long SampleCount
        {
            get
            {
                if (waveFormat.Encoding == WaveFormatEncoding.Pcm ||
                    waveFormat.Encoding == WaveFormatEncoding.Extensible ||
                    waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    return dataChunkLength / BlockAlign;
                }
                else
                {
                    throw new FormatException("Sample count is calculated only for the standard encodings");
                }
            }
        }

        /// <summary>
        /// Position in the AIFF file
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return waveStream.Position - dataPosition;
            }
            set
            {
                lock (lockObject)
                {
                    value = Math.Min(value, Length);
                    // make sure we don't get out of sync
                    value -= (value % waveFormat.BlockAlign);
                    waveStream.Position = value + dataPosition;
                }
            }
        }


        /// <summary>
        /// Reads bytes from the AIFF File into the provided span.
        /// AIFF sample data is big-endian on disk; this method swaps to little-endian.
        /// <see cref="Stream.Read(Span{byte})"/>
        /// </summary>
        public override int Read(Span<byte> buffer)
        {
            int count = buffer.Length;
            if (count % waveFormat.BlockAlign != 0)
            {
                throw new ArgumentException(
                    $"Must read complete blocks: requested {count}, block align is {WaveFormat.BlockAlign}");
            }
            lock (lockObject)
            {
                // sometimes there is more junk at the end of the file past the data chunk
                if (Position + count > dataChunkLength)
                {
                    count = dataChunkLength - (int)Position;
                }

                // Read big-endian source bytes into the caller's span, then swap in place.
                var dest = buffer.Slice(0, count);
                int length = waveStream.Read(dest);
                var read = dest.Slice(0, length);

                int bytesPerSample = WaveFormat.BitsPerSample / 8;
                switch (WaveFormat.BitsPerSample)
                {
                    case 8:
                        // no swap required
                        break;
                    case 16:
                        for (int i = 0; i < read.Length; i += bytesPerSample)
                        {
                            (read[i], read[i + 1]) = (read[i + 1], read[i]);
                        }
                        break;
                    case 24:
                        for (int i = 0; i < read.Length; i += bytesPerSample)
                        {
                            (read[i], read[i + 2]) = (read[i + 2], read[i]);
                        }
                        break;
                    case 32:
                        for (int i = 0; i < read.Length; i += bytesPerSample)
                        {
                            (read[i], read[i + 3]) = (read[i + 3], read[i]);
                            (read[i + 1], read[i + 2]) = (read[i + 2], read[i + 1]);
                        }
                        break;
                    default:
                        throw new FormatException("Unsupported PCM format.");
                }

                return length;
            }
        }

        /// <summary>
        /// Reads bytes from the AIFF File.
        /// <see cref="Stream.Read(byte[], int, int)"/>
        /// </summary>
        public override int Read(byte[] array, int offset, int count)
            => Read(array.AsSpan(offset, count));

#region Endian Helpers
        private static uint ConvertInt(byte[] buffer)
        {
            if (buffer.Length != 4) throw new InvalidDataException("Incorrect length for long.");
            return (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        }

        private static short ConvertShort(byte[] buffer)
        {
            if (buffer.Length != 2) throw new InvalidDataException("Incorrect length for int.");
            return (short)((buffer[0] << 8) | buffer[1]);
        }
#endregion


#region AiffChunk
        /// <summary>
        /// AIFF Chunk
        /// </summary>
        public struct AiffChunk
        {
            /// <summary>
            /// Chunk Name
            /// </summary>
            public string ChunkName;

            /// <summary>
            /// Chunk Length
            /// </summary>
            public uint ChunkLength;

            /// <summary>
            /// Chunk start
            /// </summary>
            public uint ChunkStart;

            /// <summary>
            /// Creates a new AIFF Chunk
            /// </summary>
            public AiffChunk(uint start, string name, uint length)
            {
                ChunkStart = start;
                ChunkName = name;
                ChunkLength = length + (uint)(length % 2 == 1 ? 1 : 0);
            }
        }

        private static AiffChunk ReadChunkHeader(BinaryReader br)
        {
            var chunk = new AiffChunk((uint)br.BaseStream.Position, ReadChunkName(br), ConvertInt(br.ReadBytes(4)));
            return chunk;
        }

        private static string ReadChunkName(BinaryReader br)
        {
            return new string(br.ReadChars(4));
        }
#endregion
    }
}
