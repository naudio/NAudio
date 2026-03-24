using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Silence producing wave provider
    /// Useful for playing silence when doing a WASAPI Loopback Capture
    /// </summary>
    public class SilenceProvider : IAudioSource
    {
        /// <summary>
        /// Creates a new silence producing wave provider
        /// </summary>
        /// <param name="wf">Desired WaveFormat (should be PCM / IEE float</param>
        public SilenceProvider(WaveFormat wf) { WaveFormat = wf; }

        /// <summary>
        /// Read silence from into the buffer
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            buffer.Clear();
            return buffer.Length;
        }

        /// <summary>
        /// WaveFormat of this silence producing wave provider
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }
    }
}
