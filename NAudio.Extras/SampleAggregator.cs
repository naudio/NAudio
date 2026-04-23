using System;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Demo sample provider that performs FFTs
    /// </summary>
    public class SampleAggregator : ISampleProvider
    {
        /// <summary>
        /// Raised to indicate the maximum volume level in this period
        /// </summary>
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;
        private float maxValue;
        private float minValue;
        /// <summary>
        /// Notification count, number of samples between MaximumCalculated events
        /// </summary>
        public int NotificationCount { get; set; }
        int count;

        /// <summary>
        /// Raised to indicate that a block of samples has had an FFT performed on it
        /// </summary>
        public event EventHandler<FftEventArgs> FftCalculated;

        /// <summary>
        /// If true, performs an FFT on each block of samples
        /// </summary>
        public bool PerformFFT { get; set; }

        private readonly Complex[] fftBuffer;       // full-size N-bin result, kept for event-API back-compat
        private readonly Complex[] halfSpectrum;    // N/2+1-bin output from FftProcessor
        private readonly float[] sampleBuffer;      // raw time-domain samples; windowing happens inside FftProcessor
        private readonly FftEventArgs fftArgs;
        private readonly FftProcessor fftProcessor;
        private int fftPos;
        private readonly int fftLength;
        private readonly ISampleProvider source;

        private readonly int channels;

        /// <summary>
        /// Creates a new SampleAggregator
        /// </summary>
        /// <param name="source">source sample provider</param>
        /// <param name="fftLength">FFT length, must be a power of 2</param>
        public SampleAggregator(ISampleProvider source, int fftLength = 1024)
        {
            channels = source.WaveFormat.Channels;
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two");
            }
            this.fftLength = fftLength;
            sampleBuffer = new float[fftLength];
            halfSpectrum = new Complex[fftLength / 2 + 1];
            fftBuffer = new Complex[fftLength];
            fftProcessor = new FftProcessor(fftLength, FftWindowType.Hamming);
            fftArgs = new FftEventArgs(fftBuffer);
            this.source = source;
        }

        static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Reset the volume calculation
        /// </summary>
        public void Reset()
        {
            count = 0;
            maxValue = minValue = 0;
        }

        private void Add(float value)
        {
            if (PerformFFT && FftCalculated != null)
            {
                sampleBuffer[fftPos] = value;
                fftPos++;
                if (fftPos >= fftLength)
                {
                    fftPos = 0;
                    fftProcessor.RealForward(sampleBuffer, halfSpectrum);
                    // Copy the half-spectrum into the full-size result buffer and fill in the
                    // conjugate-symmetric upper half so consumers that read the full N-bin array
                    // continue to see the same data they used to get from a full complex FFT.
                    for (int k = 0; k <= fftLength / 2; k++)
                    {
                        fftBuffer[k] = halfSpectrum[k];
                    }
                    for (int k = 1; k < fftLength / 2; k++)
                    {
                        fftBuffer[fftLength - k].X = halfSpectrum[k].X;
                        fftBuffer[fftLength - k].Y = -halfSpectrum[k].Y;
                    }
                    FftCalculated(this, fftArgs);
                }
            }

            maxValue = Math.Max(maxValue, value);
            minValue = Math.Min(minValue, value);
            count++;
            if (count >= NotificationCount && NotificationCount > 0)
            {
                MaximumCalculated?.Invoke(this, new MaxSampleEventArgs(minValue, maxValue));
                Reset();
            }
        }

        /// <summary>
        /// Gets the WaveFormat of this Sample Provider
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        public int Read(Span<float> buffer)
        {
            var samplesRead = source.Read(buffer);

            for (int n = 0; n < samplesRead; n+=channels)
            {
                Add(buffer[n]);
            }
            return samplesRead;
        }
    }

    /// <summary>
    /// Max sample event args
    /// </summary>
    public class MaxSampleEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new MaxSampleEventArgs
        /// </summary>
        [DebuggerStepThrough]
        public MaxSampleEventArgs(float minValue, float maxValue)
        {
            MaxSample = maxValue;
            MinSample = minValue;
        }
        /// <summary>
        /// Maximum sample value in this period
        /// </summary>
        public float MaxSample { get; private set; }
        /// <summary>
        /// Minimum sample value in this period
        /// </summary>
        public float MinSample { get; private set; }
    }

    /// <summary>
    /// FFT Event Args
    /// </summary>
    public class FftEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new FFTEventArgs
        /// </summary>
        [DebuggerStepThrough]
        public FftEventArgs(Complex[] result)
        {
            Result = result;
        }
        /// <summary>
        /// Result of FFT
        /// </summary>
        public Complex[] Result { get; private set; }
    }
}
