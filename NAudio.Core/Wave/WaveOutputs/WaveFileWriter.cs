using System;
using System.IO;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// This class writes WAV data to a .wav file on disk
    /// </summary>
    public class WaveFileWriter : Stream
    {
        private Stream outStream;
        private readonly BinaryWriter writer;
        private long dataSizePos;
        private long factSampleCountPos;
        private long dataChunkSize;
        private readonly WaveFormat format;
        private readonly string filename;

        /// <summary>
        /// Creates a 16 bit Wave File from an ISampleProvider
        /// BEWARE: the source provider must not return data indefinitely
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="sourceProvider">The source sample provider</param>
        public static void CreateWaveFile16(string filename, ISampleProvider sourceProvider)
        {
            CreateWaveFile(filename, new SampleToWaveProvider16(sourceProvider));
        }

        /// <summary>
        /// Creates a Wave file by reading all the data from a WaveProvider
        /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <param name="sourceProvider">The source WaveProvider</param>
        public static void CreateWaveFile(string filename, IWaveProvider sourceProvider)
        {
            using (var writer = new WaveFileWriter(filename, sourceProvider.WaveFormat))
            {
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }
                    // Write will throw exception if WAV file becomes too large
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
        
        /// <summary>
        /// Writes to a stream by reading all the data from a WaveProvider
        /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="outStream">The stream the method will output to</param>
        /// <param name="sourceProvider">The source WaveProvider</param>
        public static void WriteWavFileToStream(Stream outStream, IWaveProvider sourceProvider)
        {
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(outStream), sourceProvider.WaveFormat)) 
            {
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while(true) 
                {
                    var bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) 
                    {
                        // end of source provider
                        outStream.Flush();
                        break;
                    }

                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
        
        /// <summary>
        /// WaveFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriter(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write((int)0); // placeholder
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);

            CreateFactChunk();
            WriteDataChunkHeader();
        }

        /// <summary>
        /// Creates a new WaveFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public WaveFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format)
        {
            this.filename = filename;
        }

        private void WriteDataChunkHeader()
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            dataSizePos = outStream.Position;
            writer.Write((int)0); // placeholder
        }

        private void CreateFactChunk()
        {
            if (HasFactChunk())
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fact"));
                writer.Write((int)4);
                factSampleCountPos = outStream.Position;
                writer.Write((int)0); // number of samples
            }
        }

        private bool HasFactChunk()
        {
            return format.Encoding != WaveFormatEncoding.Pcm && 
                format.BitsPerSample != 0;
        }

        /// <summary>
        /// The wave file name or null if not applicable
        /// </summary>
        public string Filename => filename;

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length => dataChunkSize;

        /// <summary>
        /// Total time (calculated from Length and average bytes per second)
        /// </summary>
        public TimeSpan TotalTime => TimeSpan.FromSeconds((double)Length / WaveFormat.AverageBytesPerSecond);

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat => format;

        /// <summary>
        /// Returns false: Cannot read from a WaveFileWriter
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Returns true: Can write to a WaveFileWriter
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Returns false: Cannot seek within a WaveFileWriter
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Read is not supported for a WaveFileWriter
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriter");
        }

        /// <summary>
        /// Seek is not supported for a WaveFileWriter
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriter");
        }

        /// <summary>
        /// SetLength is not supported for WaveFileWriter
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriter");
        }

        /// <summary>
        /// Gets the Position in the WaveFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get => dataChunkSize;
            set => throw new InvalidOperationException("Repositioning a WaveFileWriter is not supported");
        }

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        [Obsolete("Use Write instead")]
        public void WriteData(byte[] data, int offset, int count)
        {
            Write(data, offset, count);
        }

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] data, int offset, int count)
        {
            if (outStream.Length + count > UInt32.MaxValue)
                throw new ArgumentException("WAV file too large", nameof(count));
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        private readonly byte[] value24 = new byte[3]; // keep this around to save us creating it every time
        
        /// <summary>
        /// Writes a single sample to the Wave file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write((Int16)(Int16.MaxValue * sample));
                dataChunkSize += 2;
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
                value24[0] = value[1];
                value24[1] = value[2];
                value24[2] = value[3];
                writer.Write(value24);
                dataChunkSize += 3;
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                writer.Write(UInt16.MaxValue * (Int32)sample);
                dataChunkSize += 4;
            }
            else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                writer.Write(sample);
                dataChunkSize += 4;
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes 32 bit floating point samples to the Wave file
        /// They will be converted to the appropriate bit depth depending on the WaveFormat of the WAV file
        /// </summary>
        /// <param name="samples">The buffer containing the floating point samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of floating point samples to write</param>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        [Obsolete("Use WriteSamples instead")]
        public void WriteData(short[] samples, int offset, int count)
        {
            WriteSamples(samples, offset, count);
        }


        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteSamples(short[] samples, int offset, int count)
        {
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(samples[sample + offset]);
                }
                dataChunkSize += (count * 2);
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    var value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)samples[sample + offset]);
                    value24[0] = value[1];
                    value24[1] = value[2];
                    value24[2] = value[3];
                    writer.Write(value24);
                }
                dataChunkSize += (count * 3);
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(UInt16.MaxValue * (Int32)samples[sample + offset]);
                }
                dataChunkSize += (count * 4);
            }
            // IEEE float data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write((float)samples[sample + offset] / (float)(Int16.MaxValue + 1));
                }
                dataChunkSize += (count * 4);
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Ensures data is written to disk
        /// Also updates header, so that WAV file will be valid up to the point currently written
        /// </summary>
        public override void Flush()
        {
            var pos = writer.BaseStream.Position;
            UpdateHeader(writer);
            writer.BaseStream.Position = pos;
        }

        #region IDisposable Members

        /// <summary>
        /// Actually performs the close,making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (outStream != null)
                {
                    try
                    {
                        UpdateHeader(writer);
                    }
                    finally
                    {
                        // in a finally block as we don't want the FileStream to run its disposer in
                        // the GC thread if the code above caused an IOException (e.g. due to disk full)
                        outStream.Dispose(); // will close the underlying base stream
                        outStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the header with file size information
        /// </summary>
        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            writer.Flush();
            UpdateRiffChunk(writer);
            UpdateFactChunk(writer);
            UpdateDataChunk(writer);
        }

        private void UpdateDataChunk(BinaryWriter writer)
        {
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write((UInt32)dataChunkSize);
        }

        private void UpdateRiffChunk(BinaryWriter writer)
        {
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((UInt32)(outStream.Length - 8));
        }

        private void UpdateFactChunk(BinaryWriter writer)
        {
            if (HasFactChunk())
            {
                int bitsPerSample = (format.BitsPerSample * format.Channels);
                if (bitsPerSample != 0)
                {
                    writer.Seek((int)factSampleCountPos, SeekOrigin.Begin);
                    
                    writer.Write((int)((dataChunkSize * 8) / bitsPerSample));
                }
            }
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this WaveFileWriter
        /// </summary>
        ~WaveFileWriter()
        {
            System.Diagnostics.Debug.Assert(false, "WaveFileWriter was not disposed");
            Dispose(false);
        }

        #endregion
    }
}
