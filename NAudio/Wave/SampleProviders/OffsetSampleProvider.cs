using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Allows you to:
    /// 1. insert a pre-delay of silence before the source begins
    /// 2. optionally skip over a certain amount of the source
    /// 3. optionally take only a set amount 
    /// 4. 
    /// </summary>
    public class OffsetSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;

        /// <summary>
        /// Number of samples of silence to insert before playing source
        /// </summary>
        public int DelayBySamples { get; set; }

        /// <summary>
        /// Number of samples in source to discard
        /// </summary>
        public int SkipOverSamples { get; set; }

        /// <summary>
        /// Number of samples to read from source (if 0, then read it all)
        /// </summary>
        public int TakeSamples { get; set; }

        /// <summary>
        /// Number of samples of silence to insert after playing source
        /// </summary>
        public int LeadOutSamples { get; set; }

        private int phase; // 0 = delay, 1 = skip, 2 = take, 3 = lead_out, 4 = end
        private int phasePos;

        /// <summary>
        /// Creates a new instance of offsetSampleProvider
        /// </summary>
        /// <param name="sourceProvider">The Source Sample Provider to read from</param>
        public OffsetSampleProvider(ISampleProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
        }

        /// <summary>
        /// The WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return this.sourceProvider.WaveFormat; }
        }

        /// <summary>
        /// Reads from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset within sample buffer to read to</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;
            if (phase == 0) // delay
            {
                int delaySamples = Math.Min(count, DelayBySamples - phasePos);
                for (int n = 0; n < delaySamples; n++)
                {
                    buffer[offset + n] = 0;
                }
                phasePos += delaySamples;
                samplesRead += delaySamples;
                if (phasePos >= DelayBySamples)
                {
                    phase++;
                    phasePos = 0;
                }
            }

            if (phase == 1) // skip
            {
                if (SkipOverSamples > 0)
                {
                    var skipBuffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
                    // skip everything
                    int samplesSkipped = 0;
                    while (samplesSkipped < SkipOverSamples)
                    {
                        int samplesRequired = Math.Min(SkipOverSamples - samplesSkipped, skipBuffer.Length);
                        var read = sourceProvider.Read(skipBuffer, 0, samplesRequired);
                        if (read == 0) // source has ended while still in skip
                        {
                            break;
                        }
                        samplesSkipped += read;
                    }
                }
                phase++;
                phasePos = 0;
            }

            if (phase == 2) // take
            {
                int samplesRequired = count - samplesRead;
                if (TakeSamples != 0)
                    samplesRequired = Math.Min(samplesRequired, TakeSamples - phasePos);
                int read = sourceProvider.Read(buffer, offset + samplesRead, samplesRequired);
                phasePos += read;
                samplesRead += read;
                if (read < samplesRequired)
                {
                    phase++;
                    phasePos = 0;
                }
            }

            if (phase == 3) // lead out
            {
                int samplesRequired = Math.Min(count - samplesRead, LeadOutSamples - phasePos);
                for (int n = 0; n < samplesRequired; n++)
                {
                    buffer[offset + samplesRead + n] = 0;
                }
                phasePos += samplesRequired;
                samplesRead += samplesRequired;
                if (phasePos >= LeadOutSamples)
                {
                    phase = 4;
                    phasePos = 0;
                }
            }

            return samplesRead;
        }
    }
}
