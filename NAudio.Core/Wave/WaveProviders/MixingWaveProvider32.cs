using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveProvider that can mix together multiple 32 bit floating point input provider
    /// All channels must have the same number of inputs and same sample rate
    /// n.b. Work in Progress - not tested yet
    /// </summary>
    public class MixingWaveProvider32 : IAudioSource
    {
        private readonly List<IAudioSource> inputs;
        private WaveFormat waveFormat;
        private readonly int bytesPerSample;

        /// <summary>
        /// Creates a new MixingWaveProvider32
        /// </summary>
        public MixingWaveProvider32()
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            this.bytesPerSample = 4;
            this.inputs = new List<IAudioSource>();
        }

        /// <summary>
        /// Creates a new 32 bit MixingWaveProvider32
        /// </summary>
        /// <param name="inputs">inputs - must all have the same format.</param>
        /// <exception cref="ArgumentException">Thrown if the input streams are not 32 bit floating point,
        /// or if they have different formats to each other</exception>
        public MixingWaveProvider32(IEnumerable<IAudioSource> inputs)
            : this()
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));

            foreach (var input in inputs)
            {
                AddInputStream(input);
            }
        }

        /// <summary>
        /// Add a new input to the mixer
        /// </summary>
        /// <param name="waveProvider">The wave input to add</param>
        public void AddInputStream(IAudioSource waveProvider)
        {
            if (waveProvider == null)
                throw new ArgumentNullException(nameof(waveProvider));

            var inputFormat = waveProvider.WaveFormat;
            if (inputFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Must be IEEE floating point", nameof(waveProvider));
            if (inputFormat.BitsPerSample != 32)
                throw new ArgumentException("Only 32 bit audio currently supported", nameof(waveProvider));

            lock (inputs)
            {
                if (inputs.Count == 0)
                {
                    // first one - set the format
                    int sampleRate = inputFormat.SampleRate;
                    int channels = inputFormat.Channels;
                    this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                }
                else
                {
                    if (!inputFormat.Equals(waveFormat))
                        throw new ArgumentException("All incoming channels must have the same format", nameof(waveProvider));
                }

                this.inputs.Add(waveProvider);
            }
        }

        /// <summary>
        /// Remove an input from the mixer
        /// </summary>
        /// <param name="waveProvider">waveProvider to remove</param>
        public void RemoveInputStream(IAudioSource waveProvider)
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
        /// <returns>Number of bytes read.</returns>
        /// <exception cref="ArgumentException">Thrown if an invalid number of bytes requested</exception>
        public int Read(Span<byte> buffer)
        {
            if (buffer.Length % bytesPerSample != 0)
                throw new ArgumentException("Must read a whole number of samples", nameof(buffer));

            // blank the buffer
            buffer.Clear();
            int bytesRead = 0;

            // sum the channels in
            byte[] readBuffer = new byte[buffer.Length];
            lock (inputs)
            {
                foreach (var input in inputs)
                {
                    int readFromThisStream = input.Read(readBuffer.AsSpan(0, buffer.Length));
                    // don't worry if input stream returns less than we requested - may indicate we have got to the end
                    bytesRead = Math.Max(bytesRead, readFromThisStream);
                    if (readFromThisStream > 0)
                    {
                        Sum32BitAudio(buffer, readBuffer, readFromThisStream);
                    }
                }
            }
            return bytesRead;
        }

        /// <summary>
        /// Actually performs the mixing
        /// </summary>
        static void Sum32BitAudio(Span<byte> destBuffer, byte[] sourceBuffer, int bytesRead)
        {
            var destFloats = MemoryMarshal.Cast<byte, float>(destBuffer);
            var sourceFloats = MemoryMarshal.Cast<byte, float>(sourceBuffer.AsSpan(0, bytesRead));
            int samplesRead = bytesRead / 4;
            for (int n = 0; n < samplesRead; n++)
            {
                destFloats[n] += sourceFloats[n];
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
