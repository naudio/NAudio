using System;
using NAudio.Dsp;

namespace NAudio.Wave
{
    /// <summary>
    /// A simple compressor
    /// </summary>
    public class SimpleCompressorEffect : ISampleSource
    {
        private readonly ISampleSource sourceStream;
        private readonly SimpleCompressor simpleCompressor;
        private readonly int channels;
        private readonly object lockObject = new object();

        /// <summary>
        /// Create a new simple compressor stream
        /// </summary>
        /// <param name="sourceStream">Source stream</param>
        public SimpleCompressorEffect(ISampleSource sourceStream)
        {
            this.sourceStream = sourceStream;
            channels = sourceStream.WaveFormat.Channels;
            simpleCompressor = new SimpleCompressor(5.0, 10.0, sourceStream.WaveFormat.SampleRate);
            simpleCompressor.Threshold = 16;
            simpleCompressor.Ratio = 6;
            simpleCompressor.MakeUpGain = 16;

        }

        /// <summary>
        /// Make-up Gain
        /// </summary>
        public double MakeUpGain
        {
            get => simpleCompressor.MakeUpGain;
            set
            {
                lock (lockObject)
                {
                    simpleCompressor.MakeUpGain = value;
                }
            }
        }

        /// <summary>
        /// Threshold
        /// </summary>
        public double Threshold
        {
            get => simpleCompressor.Threshold;
            set
            {
                lock (lockObject)
                {
                    simpleCompressor.Threshold = value;
                }
            }
        }

        /// <summary>
        /// Ratio
        /// </summary>
        public double Ratio
        {
            get => simpleCompressor.Ratio;
            set
            {
                lock (lockObject)
                {
                    simpleCompressor.Ratio = value;
                }
            }
        }

        /// <summary>
        /// Attack time
        /// </summary>
        public double Attack
        {
            get => simpleCompressor.Attack;
            set
            {
                lock (lockObject)
                {
                    simpleCompressor.Attack = value;
                }
            }
        }

        /// <summary>
        /// Release time
        /// </summary>
        public double Release
        {
            get => simpleCompressor.Release;
            set
            {
                lock (lockObject)
                {
                    simpleCompressor.Release = value;
                }
            }
        }

        /// <summary>
        /// Turns gain on or off
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the WaveFormat of this stream
        /// </summary>
        public WaveFormat WaveFormat => sourceStream.WaveFormat;


        /// <summary>
        /// Reads bytes from this stream
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <returns>Number of samples read</returns>
        public int Read(Span<float> buffer)
        {
            lock (lockObject)
            {
                int samplesRead = sourceStream.Read(buffer);
                if (Enabled)
                {
                    for (int sample = 0; sample < samplesRead; sample+=channels)
                    {
                        double in1 = buffer[sample];
                        double in2 = (channels == 1) ? 0 : buffer[sample+1];
                        simpleCompressor.Process(ref in1, ref in2);
                        buffer[sample] = (float)in1;
                        if (channels > 1)
                            buffer[sample + 1] = (float)in2;
                    }
                }
                return samplesRead;
            }
        }
    }
}
