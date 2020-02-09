using NAudio.Dsp;

namespace NAudio.Wave
{
    /// <summary>
    /// A simple compressor
    /// </summary>
    public class SimpleCompressorEffect : ISampleProvider
    {
        private readonly ISampleProvider sourceStream;
        private readonly SimpleCompressor simpleCompressor;
        private readonly int channels;
        private readonly object lockObject = new object();

        /// <summary>
        /// Create a new simple compressor stream
        /// </summary>
        /// <param name="sourceStream">Source stream</param>
        public SimpleCompressorEffect(ISampleProvider sourceStream)
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
        /// <param name="array">Buffer to read into</param>
        /// <param name="offset">Offset in array to read into</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Number of bytes read</returns>
        public int Read(float[] array, int offset, int count)
        {
            lock (lockObject)
            {
                int samplesRead = sourceStream.Read(array, offset, count);
                if (Enabled)
                {
                    for (int sample = 0; sample < samplesRead; sample+=channels)
                    {
                        double in1 = array[offset+sample];
                        double in2 = (channels == 1) ? 0 : array[offset+sample+1];
                        simpleCompressor.Process(ref in1, ref in2);
                        array[offset + sample] = (float)in1;
                        if (channels > 1)
                            array[offset + sample + 1] = (float)in2;
                    }
                }
                return samplesRead;
            }
        }
    }
}

