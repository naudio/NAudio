using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Sample Provider to concatenate multiple sample providers together
    /// </summary>
    public class ConcatenatingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider[] providers;
        private int currentProviderIndex;

        /// <summary>
        /// Creates a new ConcatenatingSampleProvider
        /// </summary>
        /// <param name="providers">The source providers to play one after the other. Must all share the same sample rate and channel count</param>
        public ConcatenatingSampleProvider(IEnumerable<ISampleProvider> providers)
        {
            if (providers == null) throw new ArgumentNullException(nameof(providers));
            this.providers = providers.ToArray();
            if (this.providers.Length == 0) throw new ArgumentException("Must provide at least one input", nameof(providers));
            if (this.providers.Any(p => p.WaveFormat.Channels != WaveFormat.Channels)) throw new ArgumentException("All inputs must have the same channel count", nameof(providers));
            if (this.providers.Any(p => p.WaveFormat.SampleRate != WaveFormat.SampleRate)) throw new ArgumentException("All inputs must have the same sample rate", nameof(providers));
        }

        /// <summary>
        /// The WaveFormat of this Sample Provider
        /// </summary>
        public WaveFormat WaveFormat => providers[0].WaveFormat;

        /// <summary>
        /// Read Samples from this sample provider
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = 0;
            while (read < count && currentProviderIndex < providers.Length)
            {
                var needed = count - read;
                var readThisTime = providers[currentProviderIndex].Read(buffer, read, needed);
                read += readThisTime;
                if (readThisTime == 0) currentProviderIndex++;
            }
            return read;
        }
    }
}