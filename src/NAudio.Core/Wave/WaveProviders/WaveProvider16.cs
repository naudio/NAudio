using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Base class for creating a 16 bit wave provider
    /// </summary>
    public abstract class WaveProvider16 : IWaveProvider
    {
        private WaveFormat waveFormat;

        /// <summary>
        /// Initializes a new instance of the WaveProvider16 class
        /// defaulting to 44.1kHz mono
        /// </summary>
        public WaveProvider16()
            : this(44100, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WaveProvider16 class with the specified
        /// sample rate and number of channels
        /// </summary>
        public WaveProvider16(int sampleRate, int channels)
        {
            SetWaveFormat(sampleRate, channels);
        }

        /// <summary>
        /// Allows you to specify the sample rate and channels for this WaveProvider
        /// (should be initialised before you pass it to a wave player)
        /// </summary>
        public void SetWaveFormat(int sampleRate, int channels)
        {
            this.waveFormat = new WaveFormat(sampleRate, 16, channels);
        }

        /// <summary>
        /// Implements the Read method of IWaveProvider by delegating to the abstract
        /// Read method taking a short span
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            var shortSpan = MemoryMarshal.Cast<byte, short>(buffer);
            int samplesRequired = buffer.Length / 2;
            int samplesRead = Read(shortSpan.Slice(0, samplesRequired));
            return samplesRead * 2;
        }

        /// <summary>
        /// Method to override in derived classes
        /// Supply the requested number of samples into the buffer
        /// </summary>
        public abstract int Read(Span<short> buffer);

        /// <summary>
        /// The Wave Format
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }
    }
}
