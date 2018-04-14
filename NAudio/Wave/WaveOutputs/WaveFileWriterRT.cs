using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NAudio.Wave
{
    /// <summary>
    /// This class writes WAV data to a .wav file on disk
    /// </summary>
    public class WaveFileWriterRT : Stream
    {
        private Stream outStream;
        private readonly BinaryWriter writer;
        private long dataSizePos;
        private long factSampleCountPos;
        private long dataChunkSize;
        private readonly WaveFormat format;
        private string filename;

        // Protects WriteAsync and FlushAsync from overlapping
        private readonly Semaphore asyncOperationsLock = new Semaphore(1, 100);

        /// <summary>
        /// Creates a 16 bit Wave File from an ISampleProvider
        /// BEWARE: the source provider must not return data indefinitely
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="sourceProvider">The source sample provider</param>
        public static Task CreateWaveFile16Async(string filename, ISampleProvider sourceProvider)
        {
            return CreateWaveFileAsync(filename, new SampleToWaveProvider16(sourceProvider));
        }

        /// <summary>
        /// Creates a Wave file by reading all the data from a WaveProvider
        /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <param name="sourceProvider">The source WaveProvider</param>
        public static async Task CreateWaveFileAsync(string filename, IWaveProvider sourceProvider)
        {
            StorageFile fileOperation = await StorageFile.GetFileFromPathAsync(filename);
            Stream fileStream = await fileOperation.OpenStreamForWriteAsync();

            using (var writer = new WaveFileWriterRT(fileStream, sourceProvider.WaveFormat))
            {
                writer.filename = filename;
                long outputLength = 0;
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }
                    outputLength += bytesRead;
                    // Write will throw exception if WAV file becomes too large
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }

        /// <summary>
        /// WaveFileWriterRT that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriterRT(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            this.writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            this.writer.Write((int)0); // placeholder
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(this.writer);

            CreateFactChunk();
            WriteDataChunkHeader();
        }

        private void WriteDataChunkHeader()
        {
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            dataSizePos = this.outStream.Position;
            this.writer.Write((int)0); // placeholder
        }

        private void CreateFactChunk()
        {
            if (HasFactChunk())
            {
                this.writer.Write(System.Text.Encoding.UTF8.GetBytes("fact"));
                this.writer.Write((int)4);
                factSampleCountPos = this.outStream.Position;
                this.writer.Write((int)0); // number of samples
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
        public string Filename
        {
            get { return filename; }
        }

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length
        {
            get { return dataChunkSize; }
        }

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return format; }
        }

        /// <summary>
        /// Returns false: Cannot read from a WaveFileWriterRT
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true: Can write to a WaveFileWriterRT
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Returns false: Cannot seek within a WaveFileWriterRT
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Returns false: Cannot timeout within a WaveFileWriterRT
        /// </summary>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>
        /// CopyToAsync is not supported for a WaveFileWriterRT
        /// </summary>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Cannot copy from a WaveFileWriterRT");
        }

        /// <summary>
        /// Read is not supported for a WaveFileWriterRT
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriterRT");
        }

        /// <summary>
        /// ReadAsync is not supported for a WaveFileWriterRT
        /// </summary>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriterRT");
        }

        /// <summary>
        /// ReadByte is not supported for a WaveFileWriterRT
        /// </summary>
        public override int ReadByte()
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriterRT");
        }

        /// <summary>
        /// Seek is not supported for a WaveFileWriterRT
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriterRT");
        }
        
        /// <summary>
        /// SetLength is not supported for WaveFileWriterRT
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriterRT");
        }

        /// <summary>
        /// Gets the Position in the WaveFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get { return dataChunkSize; }
            set { throw new InvalidOperationException("Repositioning a WaveFileWriterRT is not supported"); }
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
                throw new ArgumentException("WAV file too large", "count");
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="buffer">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public override Task WriteAsync(byte[] buffer, int offset, int count, 
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    asyncOperationsLock.WaitOne();
                    Write(buffer, offset, count);
                }
                finally
                {
                    asyncOperationsLock.Release();
                }
            });
        }

        /// <summary>
        /// WriteByte is not supported for a WaveFileWriterRT
        /// <para>Use <see cref="Write(byte[], int, int)"/> instead</para>
        /// </summary>
        /// <param name="value">value to write</param>
        public override void WriteByte(byte value)
        {
            throw new NotImplementedException();
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
                byte[] value;
                for (int sample = 0; sample < count; sample++)
                {
                    value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)samples[sample + offset]);
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
        /// </summary>
        public override void Flush()
        {
            var pos = writer.BaseStream.Position;
            UpdateHeader(writer);
            writer.BaseStream.Position = pos;
        }

        /// <summary>
        /// Ensures data is written to disk
        /// </summary>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    asyncOperationsLock.WaitOne();
                    Flush();
                }
                finally
                {
                    asyncOperationsLock.Release();
                }
            });
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
                        asyncOperationsLock.Dispose();
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
        /// Finaliser - should only be called if the user forgot to close this WaveFileWriterRT
        /// </summary>
        ~WaveFileWriterRT()
        {
            System.Diagnostics.Debug.Assert(false, "WaveFileWriterRT was not disposed");
            Dispose(false);
        }

        #endregion
    }
}
