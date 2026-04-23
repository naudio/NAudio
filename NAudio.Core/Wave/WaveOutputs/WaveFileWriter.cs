using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Whether an extra RIFF chunk is written before the <c>data</c> chunk (inside the
    /// RIFF header section) or after it (appended once data has been finalised).
    /// </summary>
    public enum ChunkPosition
    {
        /// <summary>Written before the <c>data</c> chunk (e.g. <c>bext</c>, <c>iXML</c>).</summary>
        BeforeData,
        /// <summary>Written after the <c>data</c> chunk (e.g. <c>cue </c>, <c>LIST/adtl</c>).</summary>
        AfterData
    }

    /// <summary>
    /// Writes a single RIFF chunk into a <see cref="WaveFileWriter"/>. Implementations are
    /// typically stateless; see <see cref="WaveFileWriterExtensions"/> for the built-in
    /// <c>Write*</c> helpers.
    /// </summary>
    public interface IWaveChunkWriter
    {
        /// <summary>The four-character chunk identifier.</summary>
        string ChunkId { get; }

        /// <summary>Whether this chunk should be written before or after the data chunk.</summary>
        ChunkPosition Position { get; }

        /// <summary>Writes the chunk payload (excluding id + size header, excluding word-align padding).</summary>
        void WriteData(BinaryWriter writer);
    }

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
        private readonly bool enableRf64;
        private readonly long rf64PromotionThreshold;
        private long junkChunkPos = -1;
        private bool headerFinalized;
        private bool isDisposed;
        private readonly List<BufferedChunk> beforeDataChunks = new List<BufferedChunk>();
        private readonly List<BufferedChunk> afterDataChunks = new List<BufferedChunk>();
        private CueList bufferedCues;

        private readonly struct BufferedChunk
        {
            public BufferedChunk(string id, byte[] data) { Id = id; Data = data; }
            public string Id { get; }
            public byte[] Data { get; }
        }

        /// <summary>
        /// Creates a 16 bit Wave File from an ISampleProvider.
        /// BEWARE: the source must not return data indefinitely.
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="source">The source sample provider</param>
        public static void CreateWaveFile16(string filename, ISampleProvider source)
        {
            CreateWaveFile(filename, new SampleToWaveProvider16(source));
        }

        /// <summary>
        /// Creates a Wave file by reading all the data from an IWaveProvider.
        /// BEWARE: the source MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="filename">The filename to use</param>
        /// <param name="source">The source audio</param>
        public static void CreateWaveFile(string filename, IWaveProvider source)
        {
            using (var writer = new WaveFileWriter(filename, source.WaveFormat))
            {
                var buffer = new byte[source.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int bytesRead = source.Read(buffer.AsSpan());
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    writer.Write(buffer.AsSpan(0, bytesRead));
                }
            }
        }

        /// <summary>
        /// Writes to a stream by reading all the data from an IWaveProvider.
        /// BEWARE: the source MUST return 0 from its Read method when it is finished,
        /// or the Wave File will grow indefinitely.
        /// </summary>
        /// <param name="outStream">The stream the method will output to</param>
        /// <param name="source">The source audio</param>
        public static void WriteWavFileToStream(Stream outStream, IWaveProvider source)
        {
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(outStream), source.WaveFormat))
            {
                var buffer = new byte[source.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    var bytesRead = source.Read(buffer.AsSpan());
                    if (bytesRead == 0)
                    {
                        outStream.Flush();
                        break;
                    }
                    writer.Write(buffer.AsSpan(0, bytesRead));
                }
            }
        }

        /// <summary>
        /// Creates a WaveFileWriter that writes to a stream.
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriter(Stream outStream, WaveFormat format)
            : this(outStream, format, options: null)
        {
        }

        /// <summary>
        /// Creates a WaveFileWriter that writes to a stream with the given configuration.
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        /// <param name="options">Writer configuration; <c>null</c> uses defaults.</param>
        public WaveFileWriter(Stream outStream, WaveFormat format, WaveFileWriterOptions options)
        {
            options ??= new WaveFileWriterOptions();
            this.outStream = outStream;
            this.format = format;
            this.enableRf64 = options.EnableRf64;
            this.rf64PromotionThreshold = options.Rf64PromotionThreshold;
            writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);

            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write((int)0); // placeholder
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            if (this.enableRf64)
            {
                // ds64 must immediately follow the RIFF/WAVE header per EBU Tech 3306.
                // Reserve a JUNK chunk of the same size; at close time, if the file exceeds
                // the RF64 promotion threshold, this slot is overwritten with a real ds64 chunk.
                junkChunkPos = outStream.Position;
                writer.Write(System.Text.Encoding.UTF8.GetBytes("JUNK"));
                writer.Write((int)28);
                writer.Write(new byte[28]);
            }

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);
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
        /// Creates a new WaveFileWriter with the given configuration.
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        /// <param name="options">Writer configuration; <c>null</c> uses defaults.</param>
        public WaveFileWriter(string filename, WaveFormat format, WaveFileWriterOptions options)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format, options)
        {
            this.filename = filename;
        }

        /// <summary>
        /// Adds a raw RIFF chunk to be written. Before-data chunks must be added before any
        /// audio is written; after-data chunks are buffered until the writer is closed.
        /// </summary>
        /// <param name="chunkId">Four-character chunk identifier (e.g. <c>"bext"</c>).</param>
        /// <param name="data">Chunk payload. Word-alignment padding is handled by the writer.</param>
        /// <param name="position">Where in the file the chunk should be placed.</param>
        public void AddChunk(string chunkId, byte[] data, ChunkPosition position)
        {
            ThrowIfDisposed();
            if (chunkId == null) throw new ArgumentNullException(nameof(chunkId));
            if (chunkId.Length != 4) throw new ArgumentException("Chunk id must be exactly four characters", nameof(chunkId));
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (position == ChunkPosition.BeforeData)
            {
                if (headerFinalized)
                {
                    throw new InvalidOperationException("Cannot add a BeforeData chunk after audio has been written");
                }
                beforeDataChunks.Add(new BufferedChunk(chunkId, data));
            }
            else
            {
                afterDataChunks.Add(new BufferedChunk(chunkId, data));
            }
        }

        /// <summary>
        /// Adds a chunk via an <see cref="IWaveChunkWriter"/> implementation. The writer's
        /// <see cref="IWaveChunkWriter.Position"/> decides placement.
        /// </summary>
        public void AddChunk(IWaveChunkWriter chunk)
        {
            if (chunk == null) throw new ArgumentNullException(nameof(chunk));
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                chunk.WriteData(bw);
            }
            AddChunk(chunk.ChunkId, ms.ToArray(), chunk.Position);
        }

        /// <summary>
        /// Adds a cue point with a label. The cues are written as a pair of <c>cue </c> and
        /// <c>LIST/adtl</c> chunks after the data chunk at close time.
        /// </summary>
        /// <param name="position">Sample position of the cue point.</param>
        /// <param name="label">Text label (stored as UTF-8).</param>
        public void AddCue(int position, string label)
        {
            ThrowIfDisposed();
            if (bufferedCues == null)
            {
                bufferedCues = new CueList();
            }
            bufferedCues.Add(new Cue(position, label));
        }

        private void EnsureHeaderFinalized()
        {
            if (headerFinalized) return;

            foreach (var chunk in beforeDataChunks)
            {
                WriteFramedChunk(chunk);
            }

            if (HasFactChunk())
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fact"));
                writer.Write((int)4);
                factSampleCountPos = outStream.Position;
                writer.Write((int)0);
            }

            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            dataSizePos = outStream.Position;
            writer.Write((int)0);
            headerFinalized = true;
        }

        private void WriteFramedChunk(BufferedChunk chunk)
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes(chunk.Id));
            writer.Write(chunk.Data.Length);
            writer.Write(chunk.Data);
            if ((chunk.Data.Length & 1) == 1) writer.Write((byte)0);
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

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriter");
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriter");
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriter");
        }

        /// <inheritdoc />
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
            ThrowIfDisposed();
            EnsureHeaderFinalized();
            if (!enableRf64 && (long)dataChunkSize + count > UInt32.MaxValue)
                throw new ArgumentException("WAV file too large - enable RF64 for files larger than 4 GB", nameof(count));
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        /// <summary>
        /// Appends bytes to the WaveFile from a span (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the span containing the wave data</param>
        public override void Write(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();
            EnsureHeaderFinalized();
            if (!enableRf64 && (long)dataChunkSize + data.Length > UInt32.MaxValue)
                throw new ArgumentException("WAV file too large - enable RF64 for files larger than 4 GB");
            outStream.Write(data);
            dataChunkSize += data.Length;
        }

        private readonly byte[] value24 = new byte[3]; // keep this around to save us creating it every time

        /// <summary>
        /// Writes a single sample to the Wave file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            ThrowIfDisposed();
            EnsureHeaderFinalized();
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
            ThrowIfDisposed();
            EnsureHeaderFinalized();
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
            ThrowIfDisposed();
            EnsureHeaderFinalized();
            var pos = outStream.Position;
            UpdateHeaderForSnapshot();
            outStream.Position = pos;
        }

        private void UpdateHeaderForSnapshot()
        {
            // Non-destructive header update for Flush(): update fact/data/RIFF sizes so the
            // partial file can be opened, without emitting the after-data chunks (which are
            // final and should only be written at close time).
            writer.Flush();
            UpdateRiffChunk();
            UpdateFactChunk();
            UpdateDataChunk();
        }

        #region IDisposable Members

        /// <summary>
        /// Actually performs the close, making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see cref="IDisposable.Dispose"/></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                if (outStream != null)
                {
                    try
                    {
                        EnsureHeaderFinalized();
                        FinalizeFile();
                    }
                    finally
                    {
                        outStream.Dispose();
                        outStream = null;
                        isDisposed = true;
                    }
                }
            }
            base.Dispose(disposing);
        }

        private void FinalizeFile()
        {
            writer.Flush();

            // Pad the data chunk to word alignment before anything appends after it.
            if ((dataChunkSize & 1) == 1)
            {
                writer.Write((byte)0);
            }

            // Emit buffered AddCue content (if any) ahead of explicitly-added AfterData chunks.
            if (bufferedCues != null && bufferedCues.Count > 0)
            {
                WriteFramedChunk(new BufferedChunk("cue ", bufferedCues.SerializeCueChunkData()));
                WriteFramedChunk(new BufferedChunk("LIST", bufferedCues.SerializeAdtlListChunkData()));
            }

            foreach (var chunk in afterDataChunks)
            {
                WriteFramedChunk(chunk);
            }

            // Decide whether to promote to RF64.
            if (enableRf64 && ShouldPromoteToRf64())
            {
                PromoteToRf64();
            }
            else
            {
                UpdateRiffChunk();
                UpdateDataChunk();
            }
            UpdateFactChunk();
            writer.Flush();
        }

        private bool ShouldPromoteToRf64()
        {
            long totalLength = outStream.Length;
            return dataChunkSize > rf64PromotionThreshold
                || (totalLength - 8) > rf64PromotionThreshold;
        }

        private void PromoteToRf64()
        {
            long totalLength = outStream.Length;

            // overwrite RIFF -> RF64 and set the top-level RIFF size to 0xFFFFFFFF
            outStream.Position = 0;
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RF64"));
            writer.Write(unchecked((int)0xFFFFFFFF));
            // WAVE is at offset 8 and is unchanged

            // overwrite JUNK placeholder with ds64 chunk
            outStream.Position = junkChunkPos;
            writer.Write(System.Text.Encoding.UTF8.GetBytes("ds64"));
            writer.Write((int)28);
            writer.Write((long)(totalLength - 8));  // RIFF size (64-bit)
            writer.Write((long)dataChunkSize);      // data chunk size (64-bit)
            long sampleCount = format.BlockAlign > 0 ? dataChunkSize / format.BlockAlign : 0;
            writer.Write(sampleCount);              // sample count (64-bit)
            writer.Write((int)0);                   // table length

            // data chunk size field stays 0xFFFFFFFF per RF64 convention
            outStream.Position = dataSizePos;
            writer.Write(unchecked((int)0xFFFFFFFF));
        }

        private void UpdateDataChunk()
        {
            outStream.Position = dataSizePos;
            writer.Write((UInt32)dataChunkSize);
        }

        private void UpdateRiffChunk()
        {
            outStream.Position = 4;
            writer.Write((UInt32)(outStream.Length - 8));
        }

        private void UpdateFactChunk()
        {
            if (HasFactChunk() && factSampleCountPos > 0)
            {
                int bitsPerSample = format.BitsPerSample * format.Channels;
                if (bitsPerSample != 0)
                {
                    outStream.Position = factSampleCountPos;
                    writer.Write((int)((dataChunkSize * 8) / bitsPerSample));
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(WaveFileWriter));
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
