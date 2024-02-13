using NAudio.Wave;
using System;

namespace NAudio.Extras
{

    /// <summary>
    /// Used by AudioPlaybackEngine
    /// </summary>
    public class AutoDisposeFileReader : ISampleProvider
    {
        private readonly ISampleProvider reader;
        private bool isDisposed;

        /// <summary>
        /// Creates a new file reader that disposes the source reader when it finishes
        /// </summary>
        public AutoDisposeFileReader(ISampleProvider reader)
        {
            this.reader = reader;
            WaveFormat = reader.WaveFormat;
        }

        /// <summary>
        /// Reads samples from this file reader
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            if (isDisposed)
                return 0;
            int read = reader.Read(buffer, offset, count);
            if (read == 0)
            {
                if (reader is IDisposable d)
                {
                    d.Dispose();
                }
                isDisposed = true;
            }
            return read;
        }

        /// <summary>
        /// The WaveFormat of this file reader
        /// </summary>
        public WaveFormat WaveFormat { get; }
    }
}