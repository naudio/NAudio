using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Very simple sample provider supporting adjustable gain
    /// </summary>
    public class VolumeSampleProvider : ISampleSource
    {
        private readonly ISampleSource source;

        /// <summary>
        /// Initializes a new instance of VolumeSampleProvider
        /// </summary>
        /// <param name="source">Source sample source</param>
        public VolumeSampleProvider(ISampleSource source)
        {
            this.source = source;
            Volume = 1.0f;
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads samples from this sample provider into a span
        /// </summary>
        public int Read(Span<float> buffer)
        {
            int samplesRead = source.Read(buffer);
            if (Volume != 1f)
            {
                for (int n = 0; n < samplesRead; n++)
                {
                    buffer[n] *= Volume;
                }
            }
            return samplesRead;
        }

        /// <summary>
        /// Allows adjusting the volume, 1.0f = full volume
        /// </summary>
        public float Volume { get; set; }
    }
}
