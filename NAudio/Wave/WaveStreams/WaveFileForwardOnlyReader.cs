using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.FileFormats.Wav;

namespace NAudio.Wave
{
    /// <summary>This class supports the reading of WAV files from none seekable streams,
    /// providing a repositionable WaveStream that returns the raw data
    /// contained in the WAV file
    /// </summary>
    public class WaveFileForwardOnlyReader : WaveStream
    {
        /// <summary>
        /// This riff chunks that were found.
        /// </summary>
        protected readonly List<RiffChunkData> Chunks;
        private readonly WaveFormat waveFormat;
        private readonly bool ownInput;
        private readonly long dataChunkLength;
        private long currentPosition = 0;
        private readonly object lockObject = new object();
        private Stream waveStream;

        /// <summary>Supports opening a WAV file</summary>
        /// <remarks>The WAV file format is a real mess, but we will only
        /// support the basic WAV file format which actually covers the vast
        /// majority of WAV files out there. For more WAV file format information
        /// visit www.wotsit.org. If you have a WAV file that can't be read by
        /// this class, email it to the NAudio project and we will probably
        /// fix this reader to support it
        /// </remarks>
        public WaveFileForwardOnlyReader(String waveFile) :
            this(File.OpenRead(waveFile), true, false)
        {
        }

        /// <summary>
        /// Creates a Wave File Reader based on an input stream
        /// </summary>
        /// <param name="inputStream">The input stream containing a WAV file including header</param>
        public WaveFileForwardOnlyReader(Stream inputStream) :
           this(inputStream, false, false)
        {
        }

        private WaveFileForwardOnlyReader(Stream inputStream, bool ownInput, bool storeAllChunks)
        {
            this.waveStream = inputStream;
            var chunkReader = new WaveFileChunkForwardOnlyReader(storeAllChunks);
            try
            {
                chunkReader.ReadWaveHeader(inputStream);
                this.waveFormat = chunkReader.WaveFormat;
                this.dataChunkLength = chunkReader.DataChunkLength;
                this.Chunks = chunkReader.RiffChunks;
            }
            catch
            {
                if (ownInput)
                {
                    inputStream.Dispose();
                }

                throw;
            }

            this.ownInput = ownInput;
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
        /// This is the length of audio data contained in this WAV file, in bytes
        /// (i.e. the byte length of the data chunk, not the length of the WAV file itself)
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override long Length
        {
            get
            {
                return dataChunkLength;
            }
        }

        /// <summary>
        /// Number of Samples (if possible to calculate)
        /// This currently does not take into account number of channels, so
        /// divide again by number of channels if you want the number of 
        /// audio 'frames'
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
                // n.b. if there is a fact chunk, you can use that to get the number of samples
                throw new InvalidOperationException("Sample count is calculated only for the standard encodings");
            }
        }

        /// <summary>
        /// Position in the WAV data chunk.
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get { return currentPosition; }
            set { throw new NotSupportedException("Reader doesn't support seeking."); }
        }

        /// <summary>
        /// Reads bytes from the Wave File
        /// <see cref="Stream.Read"/>
        /// </summary>
        public override int Read(byte[] array, int offset, int count)
        {
            if (count % waveFormat.BlockAlign != 0)
            {
                throw new ArgumentException(String.Format("Must read complete blocks: requested {0}, block align is {1}", count, this.WaveFormat.BlockAlign));
            }
            lock (lockObject)
            {
                // sometimes there is more junk at the end of the file past the data chunk
                if (Position + count > dataChunkLength)
                {
                    count = (int)(dataChunkLength - Position);
                }

                var readCount = waveStream.Read(array, offset, count);
                currentPosition += readCount;
                return readCount;
            }
        }

        /// <summary>
        /// Attempts to read the next sample or group of samples as floating point normalised into the range -1.0f to 1.0f
        /// </summary>
        /// <returns>An array of samples, 1 for mono, 2 for stereo etc. Null indicates end of file reached
        /// </returns>
        public float[] ReadNextSampleFrame()
        {
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                case WaveFormatEncoding.Extensible: // n.b. not necessarily PCM, should probably write more code to handle this case
                    break;
                default:
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
            var sampleFrame = new float[waveFormat.Channels];
            int bytesToRead = waveFormat.Channels * (waveFormat.BitsPerSample / 8);
            byte[] raw = new byte[bytesToRead];
            int bytesRead = Read(raw, 0, bytesToRead);
            if (bytesRead == 0) return null; // end of file
            if (bytesRead < bytesToRead) throw new InvalidDataException("Unexpected end of file");
            int offset = 0;
            for (int channel = 0; channel < waveFormat.Channels; channel++)
            {
                if (waveFormat.BitsPerSample == 16)
                {
                    sampleFrame[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                    offset += 2;
                }
                else if (waveFormat.BitsPerSample == 24)
                {
                    sampleFrame[channel] = (((sbyte)raw[offset + 2] << 16) | (raw[offset + 1] << 8) | raw[offset]) / 8388608f;
                    offset += 3;
                }
                else if (waveFormat.BitsPerSample == 32 && waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    sampleFrame[channel] = BitConverter.ToSingle(raw, offset);
                    offset += 4;
                }
                else if (waveFormat.BitsPerSample == 32)
                {
                    sampleFrame[channel] = BitConverter.ToInt32(raw, offset) / (Int32.MaxValue + 1f);
                    offset += 4;
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            return sampleFrame;
        }

        /// <summary>
        /// A forward only wave read that will also retrieve the data from the Riff Chunks that are found before the data chunk
        /// </summary>
        public class WithChunkData : WaveFileForwardOnlyReader
        {
            /// <summary>Supports opening a WAV file</summary>
            /// <remarks>The WAV file format is a real mess, but we will only
            /// support the basic WAV file format which actually covers the vast
            /// majority of WAV files out there. For more WAV file format information
            /// visit www.wotsit.org. If you have a WAV file that can't be read by
            /// this class, email it to the NAudio project and we will probably
            /// fix this reader to support it
            /// </remarks>
            public WithChunkData(string waveFile)
                : base(File.OpenRead(waveFile), true, false)
            {
            }

            /// <summary>
            /// Creates a Wave File Reader based on an input stream
            /// </summary>
            /// <param name="inputStream">The input stream containing a WAV file including header</param>
            public WithChunkData(Stream inputStream)
                : base(inputStream, false, true)
            {
            }

            /// <summary>
            /// Gets a list of the additional Chunks found in this file
            /// </summary>
            public IEnumerable<RiffChunkData> ExtraChunks
            {
                get
                {
                    return Chunks ?? new List<RiffChunkData>();
                }
            }
        }
    }
}
