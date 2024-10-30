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

        private readonly Complex[] fftBuffer;
        private readonly FftEventArgs fftArgs;
        private int fftPos;
        private readonly int fftLength;
        private readonly int m;
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
            m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            fftBuffer = new Complex[fftLength];
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
                fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
                fftBuffer[fftPos].Y = 0;
                fftPos++;
                if (fftPos >= fftBuffer.Length)
                {
                    fftPos = 0;
                    // 1024 = 2^10
                    FastFourierTransform.FFT(true, m, fftBuffer);
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
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n+=channels)
            {
                Add(buffer[n+offset]);
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
