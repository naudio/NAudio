using System;
using System.IO;

namespace NAudio.Wave
{
    /// <summary>
    /// This class writes WAV data to a .wav file on disk
    /// </summary>
    public class WaveFileWriter : IDisposable
    {
        private Stream outStream;
        private BinaryWriter writer;
        private long dataSizePos;
        private long factSampleCountPos;
        private int dataChunkSize = 0;
        private WaveFormat format;
        private bool overwriting;
        private string filename;

        /// <summary>
        /// Creates a Wave file by reading all the data from a WaveStream
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <param name="stream">The source WaveStream</param>
        public static void CreateWaveFile(string filename, WaveStream stream)
        {
            using (WaveFileWriter writer = new WaveFileWriter(filename, stream.WaveFormat))
            {
                byte[] buffer = new byte[stream.GetReadSize(4000)];
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    writer.WriteData(buffer, 0, bytesRead);
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
            this.writer = new BinaryWriter(outStream, System.Text.Encoding.ASCII);
            this.writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            this.writer.Write((int)0); // placeholder
            this.writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            this.format = format;

            format.Serialize(this.writer);

            CreateFactChunk();

            WriteDataChunkHeader();
        }

        /// <summary>
        /// Creates a new WaveFileWriter, simply overwriting the samples on an existing file
        /// </summary>
        /// <param name="filename">The filename</param>
        [Obsolete("Not planning to keep supporting this, should create derived WaveFileWriter for this type of behaviour if needed")]
        public WaveFileWriter(string filename)
        {
            this.filename = filename;
            this.outStream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            this.writer = new BinaryWriter(outStream);
            int dataChunkLength; // 
            long dataChunkPosition;
            WaveFileReader.ReadWaveHeader(outStream, out format, out dataChunkPosition, out dataChunkLength, null);
            dataSizePos = dataChunkPosition - 4;
            overwriting = true;
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
            this.writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            dataSizePos = this.outStream.Position;
            this.writer.Write((int)0); // placeholder
        }

        private void CreateFactChunk()
        {
            if (this.format.Encoding != WaveFormatEncoding.Pcm)
            {
                this.writer.Write(System.Text.Encoding.ASCII.GetBytes("fact"));
                this.writer.Write((int)4);
                factSampleCountPos = this.outStream.Position;
                this.writer.Write((int)0); // number of samples
            }
        }



        /// <summary>
        /// The wave file name
        /// </summary>
        public string Filename
        {
            get
            {
                return filename;
            }
        }

        /// <summary>
        /// Number of bytes of audio
        /// </summary>
        public long Length
        {
            get
            {
                return dataChunkSize;
            }
        }

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat
        {
            get
            {
                return format;
            }
        }

        /// <summary>
        /// Writes bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public void WriteData(byte[] data, int offset, int count)
        {
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        private byte[] value24 = new byte[3]; // keep this around to save us creating it every time
        
        /// <summary>
        /// Writes a single sample to the Wave file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write((Int16)(Int16.MaxValue * sample));
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
                value24[0] = value[1];
                value24[1] = value[2];
                value24[2] = value[3];
                writer.Write(value24);
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                writer.Write(UInt16.MaxValue * (Int32)sample);
            }
            else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                writer.Write(sample);
            }
            else
            {
                throw new ApplicationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="data">The buffer containing the wave data</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteData(short[] data, int offset, int count)
        {
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {                
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(data[sample + offset]);
                }
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                byte[] value;
                for (int sample = 0; sample < count; sample++)
                {
                    value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)data[sample + offset]);
                    value24[0] = value[1];
                    value24[1] = value[2];
                    value24[2] = value[3];
                    writer.Write(value24);
                }
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(UInt16.MaxValue * (Int32)data[sample + offset]);
                }
            }
            // IEEE float data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write((float)data[sample + offset] / (float)(Int16.MaxValue + 1));
                }
            }
            else
            {
                throw new ApplicationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes float samples to the Wave file
        /// </summary>
        /// <param name="data">The buffer containing the wave data</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of float samples to write</param>
        [Obsolete("Use the WriteSample method instead")]
        public void WriteData(float[][] data, int offset, int count)
        {
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    for (int channel = 0; channel < WaveFormat.Channels; channel++)
                    {
                        writer.Write((Int16)(Int16.MaxValue * data[channel][sample + offset]));
                    }
                }
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                byte[] value;
                byte[] value24 = new byte[3];
                for (int sample = 0; sample < count; sample++)
                {
                    for (int channel = 0; channel < WaveFormat.Channels; channel++)
                    {
                        value = BitConverter.GetBytes((Int32)(Int32.MaxValue * data[channel][sample + offset]));
                        value24[0] = value[1];
                        value24[1] = value[2];
                        value24[2] = value[3];
                        writer.Write(value24);
                    }
                }
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    for (int channel = 0; channel < WaveFormat.Channels; channel++)
                    {
                        writer.Write((Int32)(Int32.MaxValue * data[channel][sample + offset]));
                    }
                }
            }
            // IEEE float data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    for (int channel = 0; channel < WaveFormat.Channels; channel++)
                    {
                        writer.Write(data[channel][sample + offset]);
                    }
                }
            }
            else
            {
                throw new ApplicationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Ensures data is written to disk
        /// </summary>
        public void Flush()
        {
            outStream.Flush();
        }

        #region IDisposable Members

        /// <summary>
        /// Closes this WaveFile (calls <see>Dispose</see>)
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Closes this WaveFile
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Actually performs the close,making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (outStream != null)
                {
                    try
                    {
                        if (!overwriting)
                        {
                            UpdateHeader(writer);
                        }
                    }
                    finally
                    {
                        // in a finally block as we don't want the FileStream to run its disposer in
                        // the GC thread if the code above caused an IOException (e.g. due to disk full)
                        outStream.Close(); // will close the underlying base stream
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
            // in overwrite mode, we will not change the length set at the start
            // irrespective of whether we actually wrote less or more
            outStream.Flush();
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((int)(outStream.Length - 8));
            if (format.Encoding != WaveFormatEncoding.Pcm)
            {
                writer.Seek((int)factSampleCountPos, SeekOrigin.Begin);
                writer.Write((int)((dataChunkSize * 8) / format.BitsPerSample));
            }
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write((int)(dataChunkSize));
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
