using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveProvider that can mix together multiple 32 bit floating point input provider
    /// All channels must have the same number of inputs and same sample rate
    /// n.b. Work in Progress - not tested yet
    /// </summary>
    public class MixingWaveProvider32 : IWaveProvider
    {
        private List<IWaveProvider> inputs;
        private WaveFormat waveFormat;
        private int bytesPerSample;

        /// <summary>
        /// Creates a new MixingWaveProvider32
        /// </summary>
        public MixingWaveProvider32()
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            this.bytesPerSample = 4;
            this.inputs = new List<IWaveProvider>();
        }

        /// <summary>
        /// Creates a new 32 bit MixingWaveProvider32
        /// </summary>
        /// <param name="inputs">inputs - must all have the same format.</param>
        /// <exception cref="ArgumentException">Thrown if the input streams are not 32 bit floating point,
        /// or if they have different formats to each other</exception>
        public MixingWaveProvider32(IEnumerable<IWaveProvider> inputs)
            : this()
        {
            foreach (var input in inputs)
            {
                AddInputStream(input);
            }
        }

        /// <summary>
        /// Add a new input to the mixer
        /// </summary>
        /// <param name="waveProvider">The wave input to add</param>
        public void AddInputStream(IWaveProvider waveProvider)
        {
            if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Must be IEEE floating point", "waveProvider.WaveFormat");
            if (waveProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Only 32 bit audio currently supported", "waveProvider.WaveFormat");

            if (inputs.Count == 0)
            {
                // first one - set the format
                int sampleRate = waveProvider.WaveFormat.SampleRate;
                int channels = waveProvider.WaveFormat.Channels;
                this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }
            else
            {
                if (!waveProvider.WaveFormat.Equals(waveFormat))
                    throw new ArgumentException("All incoming channels must have the same format", "waveProvider.WaveFormat");
            }

            lock (inputs)
            {
                this.inputs.Add(waveProvider);
            }
        }

        /// <summary>
        /// Remove an input from the mixer
        /// </summary>
        /// <param name="waveProvider">waveProvider to remove</param>
        public void RemoveInputStream(IWaveProvider waveProvider)
        {
            lock (inputs)
            {
                this.inputs.Remove(waveProvider);
            }
        }

        /// <summary>
        /// The number of inputs to this mixer
        /// </summary>
        public int InputCount
        {
            get { return this.inputs.Count; }
        }

        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="offset">offset into buffer</param>
        /// <param name="count">number of bytes required</param>
        /// <returns>Number of bytes read.</returns>
        /// <exception cref="ArgumentException">Thrown if an invalid number of bytes requested</exception>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (count % bytesPerSample != 0)
                throw new ArgumentException("Must read an whole number of samples", "count");

            // blank the buffer
            Array.Clear(buffer, offset, count);
            int bytesRead = 0;

            // sum the channels in
            byte[] readBuffer = new byte[count];
            lock (inputs)
            {
                foreach (var input in inputs)
                {
                    int readFromThisStream = input.Read(readBuffer, 0, count);
                    // don't worry if input stream returns less than we requested - may indicate we have got to the end
                    bytesRead = Math.Max(bytesRead, readFromThisStream);
                    if (readFromThisStream > 0)
                    {
                        Sum32BitAudio(buffer, offset, readBuffer, readFromThisStream);
                    }
                }
            }
            return bytesRead;
        }

        /// <summary>
        /// Actually performs the mixing
        /// </summary>
        static unsafe void Sum32BitAudio(byte[] destBuffer, int offset, byte[] sourceBuffer, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                      pSourceBuffer = &sourceBuffer[0])
            {
                float* pfDestBuffer = (float*)pDestBuffer;
                float* pfReadBuffer = (float*)pSourceBuffer;
                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    pfDestBuffer[n] += pfReadBuffer[n];
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return this.waveFormat; }
        }
    }
}
