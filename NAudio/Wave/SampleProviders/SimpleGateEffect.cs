using NAudio.Dsp;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// A simple gate
    /// </summary>
    public class SimpleGateEffect : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly SimpleGate simpleGate;
        private readonly int channels;
        private readonly object lockObject = new object();

        /// <summary>
        /// Create a new simple gate effect
        /// </summary>
        /// <param name="sourceProvider">Source sample provider</param>
        public SimpleGateEffect(ISampleProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
            channels = sourceProvider.WaveFormat.Channels;
            simpleGate = new SimpleGate(10.0, 100.0, sourceProvider.WaveFormat.SampleRate);
            simpleGate.Threshold = -50;
        }

        /// <summary>
        /// Threshold
        /// </summary>
        public double Threshold
        {
            get => simpleGate.Threshold;
            set
            {
                lock (lockObject)
                {
                    simpleGate.Threshold = value;
                }
            }
        }

        /// <summary>
        /// Attack time
        /// </summary>
        public double Attack
        {
            get => simpleGate.Attack;
            set
            {
                lock (lockObject)
                {
                    simpleGate.Attack = value;
                }
            }
        }

        /// <summary>
        /// Release time
        /// </summary>
        public double Release
        {
            get => simpleGate.Release;
            set
            {
                lock (lockObject)
                {
                    simpleGate.Release = value;
                }
            }
        }

        /// <summary>
        /// Turns gate on or off
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the WaveFormat of this stream
        /// </summary>
        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

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
                int samplesRead = sourceProvider.Read(array, offset, count);
                if (Enabled)
                {
                    for (int sample = 0; sample < samplesRead; sample += channels)
                    {
                        double in1 = array[offset + sample];
                        double in2 = (channels == 1) ? 0 : array[offset + sample + 1];
                        simpleGate.Process(ref in1, ref in2);
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
