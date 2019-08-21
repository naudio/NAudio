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
        /// Gets the WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            lock (lockObject)
            {
                int samplesRead = sourceProvider.Read(buffer, offset, count);
                if (Enabled)
                {
                    for (int sample = 0; sample < samplesRead; sample += channels)
                    {
                        double in1 = buffer[offset + sample];
                        double in2 = (channels == 1) ? 0 : buffer[offset + sample + 1];
                        simpleGate.Process(ref in1, ref in2);
                        buffer[offset + sample] = (float)in1;
                        if (channels > 1)
                            buffer[offset + sample + 1] = (float)in2;
                    }
                }
                return samplesRead;
            }
        }
    }
}
