using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Simple class that raises an event on every sample
    /// </summary>
    public class NotifyingSampleProvider : ISampleProvider, ISampleNotifier
    {
        private ISampleProvider source;
        // try not to give the garbage collector anything to deal with when playing live audio
        private SampleEventArgs sampleArgs = new SampleEventArgs(0, 0);
        private int channels;

        /// <summary>
        /// Initializes a new instance of NotifyingSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public NotifyingSampleProvider(ISampleProvider source)
        {
            this.source = source;
            this.channels = this.WaveFormat.Channels;
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="sampleCount">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int sampleCount)
        {
            int samplesRead = source.Read(buffer, offset, sampleCount);
            if (Sample != null)
            {
                for (int n = 0; n < sampleCount; n += channels)
                {
                    sampleArgs.Left = buffer[offset + n];
                    sampleArgs.Right = channels > 1 ? buffer[offset + n + 1] : sampleArgs.Left;
                    Sample(this, sampleArgs);
                }
            }
            return samplesRead;
        }

        /// <summary>
        /// Sample notifier
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;
    }
}
