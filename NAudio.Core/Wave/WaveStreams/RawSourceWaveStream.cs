using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that simply passes on data from its source stream
    /// (e.g. a MemoryStream)
    /// </summary>
    public class RawSourceWaveStream : WaveStream
    {
        private readonly Stream sourceStream;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Initialises a new instance of RawSourceWaveStream
        /// </summary>
        /// <param name="sourceStream">The source stream containing raw audio</param>
        /// <param name="waveFormat">The waveformat of the audio in the source stream</param>
        public RawSourceWaveStream(Stream sourceStream, WaveFormat waveFormat)
        {
            this.sourceStream = sourceStream;
            this.waveFormat = waveFormat;
        }
        
        /// <summary>
        /// Initialises a new instance of RawSourceWaveStream
        /// </summary>
        /// <param name="byteStream">The buffer containing raw audio</param>
        /// <param name="offset">Offset in the source buffer to read from</param>
        /// <param name="count">Number of bytes to read in the buffer</param>
        /// <param name="waveFormat">The waveformat of the audio in the source stream</param>
        public RawSourceWaveStream(byte[] byteStream, int offset, int count, WaveFormat waveFormat)
        {
            sourceStream = new MemoryStream(byteStream, offset, count);
            this.waveFormat = waveFormat;
        }

        /// <summary>
        /// The WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// The length in bytes of this stream (if supported)
        /// </summary>
        public override long Length => sourceStream.Length;

        /// <summary>
        /// The current position in this stream
        /// </summary>
        public override long Position
        {
            get
            {
                return sourceStream.Position;
            }
            set
            {
                sourceStream.Position = value - (value % waveFormat.BlockAlign);
            }
        }

        /// <summary>
        /// Reads data from the stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return sourceStream.Read(buffer, offset, count);
            }
            catch (EndOfStreamException)
            {
                return 0;
            }
        }
    }
}

