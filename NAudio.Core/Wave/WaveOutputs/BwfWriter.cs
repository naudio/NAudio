using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Broadcast WAVE File Writer
    /// </summary>
    public class BwfWriter : IDisposable
    {
        private readonly WaveFormat format;
        private readonly BinaryWriter writer;
        private readonly long dataChunkSizePosition;
        private long dataLength;
        private bool isDisposed;

        /// <summary>
        /// Createa a new BwfWriter
        /// </summary>
        /// <param name="filename">Rarget filename</param>
        /// <param name="format">WaveFormat</param>
        /// <param name="bextChunkInfo">Chunk information</param>
        public BwfWriter(string filename, WaveFormat format, BextChunkInfo bextChunkInfo)
        {
            this.format = format;
            writer = new BinaryWriter(File.OpenWrite(filename));
            writer.Write(Encoding.UTF8.GetBytes("RIFF")); // will be updated to RF64 if large 
            writer.Write(0); // placeholder
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(Encoding.UTF8.GetBytes("JUNK")); // ds64
            writer.Write(28); // ds64 size
            writer.Write(0L); // RIFF size
            writer.Write(0L); // data size
            writer.Write(0L); // sampleCount size
            writer.Write(0); // table length
            // TABLE appears here - to store the sizes of other huge chunks other than

            // write the broadcast audio extension
            writer.Write(Encoding.UTF8.GetBytes("bext"));
            var codingHistory = Encoding.ASCII.GetBytes(bextChunkInfo.CodingHistory ?? "");
            var bextLength = 602 + codingHistory.Length;
            if (bextLength % 2 != 0)
                bextLength++;
            writer.Write(bextLength); // bext size
            var bextStart = writer.BaseStream.Position;
            writer.Write(GetAsBytes(bextChunkInfo.Description, 256));
            writer.Write(GetAsBytes(bextChunkInfo.Originator, 32));
            writer.Write(GetAsBytes(bextChunkInfo.OriginatorReference, 32));
            writer.Write(GetAsBytes(bextChunkInfo.OriginationDate, 10));
            writer.Write(GetAsBytes(bextChunkInfo.OriginationTime, 8));
            writer.Write(bextChunkInfo.TimeReference); // 8 bytes long
            writer.Write(bextChunkInfo.Version); // 2 bytes long
            writer.Write(GetAsBytes(bextChunkInfo.UniqueMaterialIdentifier, 64));
            writer.Write(bextChunkInfo.Reserved); // for version 1 this is 190 bytes
            writer.Write(codingHistory);
            if (codingHistory.Length % 2 != 0)
                writer.Write((byte)0);
            Debug.Assert(writer.BaseStream.Position == bextStart + bextLength, "Invalid bext chunk size");

            // write the format chunk
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);

            writer.Write(Encoding.UTF8.GetBytes("data"));
            dataChunkSizePosition = writer.BaseStream.Position;
            writer.Write(-1); // will be overwritten unless this is RF64
            // now finally the data chunk
        }

        /// <summary>
        /// Write audio data to this BWF
        /// </summary>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (isDisposed) throw new ObjectDisposedException("This BWF Writer already disposed");
            writer.Write(buffer, offset, count);
            dataLength += count;
        }

        /// <summary>
        /// Flush writer, and fix up header sizes
        /// </summary>
        public void Flush()
        {
            if (isDisposed) throw new ObjectDisposedException("This BWF Writer already disposed");
            writer.Flush();
            FixUpChunkSizes(true); // here to ensure WAV file created is always playable after Flush
        }

        private void FixUpChunkSizes(bool restorePosition)
        {
            var pos = writer.BaseStream.Position;
            var isLarge = dataLength > Int32.MaxValue;
            var riffSize = writer.BaseStream.Length - 8;
            if (isLarge)
            {
                var bytesPerSample = (format.BitsPerSample / 8) * format.Channels;
                writer.BaseStream.Position = 0;
                writer.Write(Encoding.UTF8.GetBytes("RF64"));
                writer.Write(-1);
                writer.BaseStream.Position += 4; // skip over WAVE
                writer.Write(Encoding.UTF8.GetBytes("ds64"));
                writer.BaseStream.Position += 4; // skip over ds64 chunk size
                writer.Write(riffSize);
                writer.Write(dataLength);
                writer.Write(dataLength / bytesPerSample);

                // data chunk size can stay as -1
            }
            else
            {
                // fix up the RIFF size
                writer.BaseStream.Position = 4;
                writer.Write((uint)riffSize);
                // fix up the data chunk size
                writer.BaseStream.Position = dataChunkSizePosition;
                writer.Write((uint)dataLength);
            }
            if (restorePosition)
            {
                writer.BaseStream.Position = pos;
            }

        }

        /// <summary>
        /// Disposes this writer
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                FixUpChunkSizes(false);
                writer.Dispose();
                isDisposed = true;
            }
        }

        private static byte[] GetAsBytes(string message, int byteSize)
        {
            var outputBuffer = new byte[byteSize];
            var encoded = Encoding.ASCII.GetBytes(message ?? "");
            Array.Copy(encoded, outputBuffer, Math.Min(encoded.Length, byteSize));
            return outputBuffer;
        }
    }
}