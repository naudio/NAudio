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
            BinaryWriter w = new BinaryWriter(outStream, System.Text.Encoding.ASCII);
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write((int)0); // placeholder
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            this.format = format;

            format.Serialize(w);

            CreateFactChunk(outStream, format, w);

            WriteDataChunkHeader(outStream, w);
        }

        private void WriteDataChunkHeader(Stream outStream, BinaryWriter w)
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            dataSizePos = outStream.Position;
            w.Write((int)0); // placeholder
        }

        private void CreateFactChunk(Stream outStream, WaveFormat format, BinaryWriter w)
        {
            if (format.Encoding != WaveFormatEncoding.Pcm)
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("fact"));
                w.Write((int)4);
                factSampleCountPos = outStream.Position;
                w.Write((int)0); // number of samples
            }
        }

        /// <summary>
        /// Creates a new WaveFileWriter, simply overwriting the samples on an existing file
        /// </summary>
        /// <param name="filename">The filename</param>
        public WaveFileWriter(string filename)
        {
            this.filename = filename;
            outStream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
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
        /// Writes bytes to the WaveFile
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public void WriteData(byte[] data, int offset, int count)
        {
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="data">The buffer containing the wave data</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteData(short[] data, int offset, int count)
        {
            BinaryWriter w = new BinaryWriter(outStream);
            for (int n = 0; n < count; n++)
                w.Write(data[n + offset]);
            dataChunkSize += (count * 2);
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="data">The buffer containing the wave data</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteData16(float[][] data, int offset, int count)
        {
            BinaryWriter w = new BinaryWriter(outStream);
            for (int n = 0; n < count; n++)
                for (int c = 0; c < data.Length; c++)
                    w.Write((short)(32768.0f * data[c][n + offset]));
            dataChunkSize += (count * 2);
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="data">The buffer containing the wave data</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteData16(double[][] data, int offset, int count)
        {
            BinaryWriter w = new BinaryWriter(outStream);
            for (int n = 0; n < count; n++)
                for (int c = 0; c < data.Length; c++)
                    w.Write((short)(32768.0 * data[c][n + offset]));
            dataChunkSize += (count * 2);
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
                            // in overwrite mode, we will not change the length set at the start
                            // irrespective of whether we actually wrote less or more
                            outStream.Flush();
                            BinaryWriter w = new BinaryWriter(outStream, System.Text.Encoding.ASCII);
                            w.Seek(4, SeekOrigin.Begin);
                            w.Write((int)(outStream.Length - 8));
                            if (format.Encoding != WaveFormatEncoding.Pcm)
                            {
                                w.Seek((int)factSampleCountPos, SeekOrigin.Begin);
                                w.Write((int)((dataChunkSize * 8) / format.BitsPerSample));
                            }
                            w.Seek((int)dataSizePos, SeekOrigin.Begin);
                            w.Write((int)(dataChunkSize));
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
