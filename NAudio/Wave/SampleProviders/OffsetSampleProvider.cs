using System;

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
        private readonly ISampleProvider _sourceProvider;
        private int _phase; // 0 = not started yet, 1 = delay, 2 = skip, 3 = take, 4 = lead_out, 5 = end
        private int _phasePos;
        private int _delayBySamples;
        private int _skipOverSamples;
        private int _takeSamples;
        private int _leadOutSamples;

        /// <summary>
        /// Number of samples of silence to insert before playing source
        /// </summary>
        public int DelayBySamples
        {
            get { return _delayBySamples; }
            set
            {
                if (_phase != 0)
                { 
                    throw new InvalidOperationException("Can't set DelayBySamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("DelayBySamples must be a multiple of WaveFormat.Channels");
                }
                _delayBySamples = value;
            }
        }

        /// <summary>
        /// Number of samples in source to discard
        /// </summary>
        public int SkipOverSamples
        {
            get { return _skipOverSamples; }
            set
            {
                if (_phase != 0)
                {
                    throw new InvalidOperationException("Can't set SkipOverSamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("SkipOverSamples must be a multiple of WaveFormat.Channels");
                }
                _skipOverSamples = value;
            }
        }

        /// <summary>
        /// Number of samples to read from source (if 0, then read it all)
        /// </summary>
        public int TakeSamples
        {
            get { return _takeSamples; }
            set
            {
                if (_phase != 0)
                {
                    throw new InvalidOperationException("Can't set TakeSamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("TakeSamples must be a multiple of WaveFormat.Channels");
                }
                _takeSamples = value;
            }
        }

        /// <summary>
        /// Number of samples of silence to insert after playing source
        /// </summary>
        public int LeadOutSamples
        {
            get { return _leadOutSamples; }
            set
            {
                if (_phase != 0)
                {
                    throw new InvalidOperationException("Can't set LeadOutSamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("LeadOutSamples must be a multiple of WaveFormat.Channels");
                }
                _leadOutSamples = value;
            }
        }

        /// <summary>
        /// Creates a new instance of offsetSampleProvider
        /// </summary>
        /// <param name="sourceProvider">The Source Sample Provider to read from</param>
        public OffsetSampleProvider(ISampleProvider sourceProvider)
        {
            _sourceProvider = sourceProvider;
        }

        /// <summary>
        /// The WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return _sourceProvider.WaveFormat; }
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

            if (_phase == 0) // not started yet
            {
                _phase++;
            }

            if (_phase == 1) // delay
            {
                int delaySamples = Math.Min(count, DelayBySamples - _phasePos);
                for (int n = 0; n < delaySamples; n++)
                {
                    buffer[offset + n] = 0;
                }
                _phasePos += delaySamples;
                samplesRead += delaySamples;
                if (_phasePos >= DelayBySamples)
                {
                    _phase++;
                    _phasePos = 0;
                }
            }

            if (_phase == 2) // skip
            {
                if (SkipOverSamples > 0)
                {
                    var skipBuffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
                    // skip everything
                    int samplesSkipped = 0;
                    while (samplesSkipped < SkipOverSamples)
                    {
                        int samplesRequired = Math.Min(SkipOverSamples - samplesSkipped, skipBuffer.Length);
                        var read = _sourceProvider.Read(skipBuffer, 0, samplesRequired);
                        if (read == 0) // source has ended while still in skip
                        {
                            break;
                        }
                        samplesSkipped += read;
                    }
                }
                _phase++;
                _phasePos = 0;
            }

            if (_phase == 3) // take
            {
                int samplesRequired = count - samplesRead;
                if (TakeSamples != 0)
                    samplesRequired = Math.Min(samplesRequired, TakeSamples - _phasePos);
                int read = _sourceProvider.Read(buffer, offset + samplesRead, samplesRequired);
                _phasePos += read;
                samplesRead += read;
                if (read < samplesRequired)
                {
                    _phase++;
                    _phasePos = 0;
                }
            }

            if (_phase == 4) // lead out
            {
                int samplesRequired = Math.Min(count - samplesRead, LeadOutSamples - _phasePos);
                for (int n = 0; n < samplesRequired; n++)
                {
                    buffer[offset + samplesRead + n] = 0;
                }
                _phasePos += samplesRequired;
                samplesRead += samplesRequired;
                if (_phasePos >= LeadOutSamples)
                {
                    _phase = 4;
                    _phasePos = 0;
                }
            }

            return samplesRead;
        }
    }
}
