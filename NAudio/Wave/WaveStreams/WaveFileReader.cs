using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace NAudio.Wave 
{
    /// <summary>A read-only stream of WAVE data based on a wave file
    /// with an associated WaveFormat
    /// </summary>
    public class WaveFileReader : WaveStream
    {
        private WaveFormat waveFormat;
        private Stream waveStream;
        private bool ownInput;
        private long dataPosition;
        private int dataChunkLength;
        private List<RiffChunk> chunks = new List<RiffChunk>();

        /// <summary>Supports opening a WAV file</summary>
        /// <remarks>The WAV file format is a real mess, but we will only
        /// support the basic WAV file format which actually covers the vast
        /// majority of WAV files out there. For more WAV file format information
        /// visit www.wotsit.org. If you have a WAV file that can't be read by
        /// this class, email it to the nAudio project and we will probably
        /// fix this reader to support it
        /// </remarks>
        public WaveFileReader(String waveFile) :
            this(File.OpenRead(waveFile))
        {
            ownInput = true;
        }

        /// <summary>
        /// Creates a Wave File Reader based on an input stream
        /// </summary>
        /// <param name="inputStream">The input stream containing a WAV file including header</param>
        public WaveFileReader(Stream inputStream)
        {
            this.waveStream = inputStream;
            ReadWaveHeader(waveStream, out waveFormat, out dataPosition, out dataChunkLength, chunks);
            Position = 0;
        }

        /// <summary>
        /// Reads the header part of a WAV file from a stream
        /// </summary>
        /// <param name="stream">The stream, positioned at the start of audio data</param>
        /// <param name="format">The format found</param>
        /// <param name="dataChunkPosition">The position of the data chunk</param>
        /// <param name="dataChunkLength">The length of the data chunk</param>
        /// <param name="chunks">Additional chunks found</param>
        public static void ReadWaveHeader(Stream stream, out WaveFormat format, out long dataChunkPosition, out int dataChunkLength, List<RiffChunk> chunks)        
        {
            dataChunkPosition = -1;
            BinaryReader br = new BinaryReader(stream);
            if (br.ReadInt32() != WaveInterop.mmioStringToFOURCC("RIFF", 0))
            {
                throw new FormatException("Not a WAVE file - no RIFF header");
            }
            uint fileSize = br.ReadUInt32(); // read the file size (minus 8 bytes)
            if (br.ReadInt32() != WaveInterop.mmioStringToFOURCC("WAVE", 0))
            {
                throw new FormatException("Not a WAVE file - no WAVE header");
            }

            // now we expect the format chunk
            if (br.ReadInt32() != WaveInterop.mmioStringToFOURCC("fmt ", 0))
            {
                throw new FormatException("Not a WAVE file - no fmt header");
            }
            format = new WaveFormatExtraData(br);
            
            Int32 dataChunkID = WaveInterop.mmioStringToFOURCC("data", 0);
            dataChunkLength = 0;

            // sometimes a file has more data than is specified after the RIFF header
            long stopPosition = Math.Min(fileSize + 8, stream.Length);
            
            // this -8 is so we can be sure that there are at least 8 bytes for a chunk id and length
            while (stream.Position < stopPosition - 8)
            {
                Int32 chunkIdentifier = br.ReadInt32();                
                Int32 chunkLength = br.ReadInt32();
                if (chunkIdentifier == dataChunkID)
                {
                    dataChunkPosition = stream.Position;
                    dataChunkLength = chunkLength;
                }
                else
                {
                    if (chunks != null)
                    {
                        chunks.Add(new RiffChunk(chunkIdentifier, chunkLength, stream.Position));
                    }
                }
                stream.Position += chunkLength;
            }
            
        }

        /// <summary>
        /// Gets a list of the additional chunks found in this file
        /// </summary>
        public List<RiffChunk> ExtraChunks
        {
            get
            {
                return chunks;
            }
        }

        /// <summary>
        /// Gets the data for the specified chunk
        /// </summary>
        public byte[] GetChunkData(RiffChunk chunk)
        {
            long oldPosition = waveStream.Position;
            waveStream.Position = chunk.StreamPosition;
            byte[] data = new byte[chunk.Length];
            waveStream.Read(data, 0, data.Length);
            waveStream.Position = oldPosition;
            return data;
        }

        /// <summary>
        /// Cleans up the resources associated with this WaveFileReader
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
                        waveStream.Close();
                    }
                    waveStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveFileReader was not disposed");
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get
            {
                return waveFormat;
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override long Length
        {
            get
            {
                return dataChunkLength;
            }
        }

        /*public long SampleCount
        {
            get
            {
                return dataChunkLength / BlockAlign;
            }
        }*/

        /// <summary>
        /// Position in the wave file
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
                lock (this)
                {
                    value = Math.Min(value, Length);
                    // make sure we don't get out of sync
                    value -= (value % waveFormat.BlockAlign);
                    waveStream.Position = value + dataPosition;
                }
            }
        }


        /// <summary>
        /// Reads bytes from the Wave File
        /// <see cref="Stream.Read"/>
        /// </summary>
        public override int Read(byte[] array, int offset, int count)
        {
            if (count % waveFormat.BlockAlign != 0)
                throw new ApplicationException("Must read complete blocks");
            // sometimes there is more junk at the end of the file past the data chunk
            if (Position + count > dataChunkLength)
                count = dataChunkLength - (int)Position;
            return waveStream.Read(array, offset, count);
        }
        
        /// <summary>
        /// Attempts to read a float from a 16 bit stream
        /// </summary>
        public bool TryReadFloat(ref float sampleValue)
        {
            if (waveFormat.BitsPerSample != 16)
                throw new InvalidOperationException("Only 16 bit audio supported");
            byte[] buffer = new byte[2];
            int read = waveStream.Read(buffer, 0, 2);
            if (read < 2)
                return false;
            sampleValue = BitConverter.ToInt16(buffer, 0) / 32768.0f;
            return true;
        }

        /// <summary>
        /// Attempts to read a double from a 16 bit stream
        /// </summary>
        public bool TryReadDouble(ref double sampleValue)
        {
            if (waveFormat.BitsPerSample != 16)
                throw new InvalidOperationException("Only 16 bit audio supported");
            byte[] buffer = new byte[2];
            int read = waveStream.Read(buffer, 0, 2);
            if (read < 2)
                return false;
            sampleValue = BitConverter.ToInt16(buffer, 0) / 32768.0;
            return true;
        }

        // for the benefit of oggencoder we divide by 32768.0f;
        /// <summary>
        /// Reads floats into arrays of channels
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="samples">number of samples to read</param>
        /// <returns></returns>
        public int Read(float[][] buffer, int samples)
        {
            BinaryReader br = new BinaryReader(waveStream);
            if (waveFormat.BitsPerSample != 16)
                throw new ApplicationException("Only 16 bit audio supported");
            for (int sample = 0; sample < samples; sample++)
            {
                for (int channel = 0; channel < waveFormat.Channels; channel++)
                {
                    if (waveStream.Position < waveStream.Length)
                        buffer[channel][sample] = (float)br.ReadInt16() / 32768.0f;
                    else
                        return 0;
                }
            }
            return samples;
        }


        /// <summary>
        /// Reads multi-channel 16 bit audio into buffers of doubles
        /// </summary>
        /// <param name="buffer">Buffer for audio</param>
        /// <param name="samples">number of samples to read</param>
        /// <returns>number of samples read</returns>
        public int Read(double[][] buffer, int samples)
        {
            BinaryReader br = new BinaryReader(waveStream); 
            if (waveFormat.BitsPerSample != 16)
                throw new ApplicationException("Only 16 bit audio supported");
            for (int sample = 0; sample < samples; sample++)
            {
                for (int channel = 0; channel < waveFormat.Channels; channel++)
                {
                    if (waveStream.Position < waveStream.Length)
                        buffer[channel][sample] = br.ReadInt16() / 32768.0;
                    else
                        return sample;
                }
            }
            return samples;
        }
    }
}
