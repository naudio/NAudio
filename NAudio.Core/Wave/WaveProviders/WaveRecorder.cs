using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Utility class to intercept audio from an IAudioSource and
    /// save it to disk
    /// </summary>
    public class WaveRecorder : IAudioSource, IDisposable
    {
        private WaveFileWriter writer;
        private IAudioSource source;

        /// <summary>
        /// Constructs a new WaveRecorder
        /// </summary>
        /// <param name="destination">The location to write the WAV file to</param>
        /// <param name="source">The Source Wave Provider</param>
        public WaveRecorder(IAudioSource source, string destination)
        {
            this.source = source;
            this.writer = new WaveFileWriter(destination, source.WaveFormat);
        }

        /// <summary>
        /// Read simply returns what the source returns, but writes to disk along the way
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            int bytesRead = source.Read(buffer);
            writer.Write(buffer.Slice(0, bytesRead));
            return bytesRead;
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }

        /// <summary>
        /// Closes the WAV file
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }
    }
}
