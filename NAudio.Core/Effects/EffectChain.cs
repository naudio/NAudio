using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// An ordered chain of <see cref="IAudioEffect"/>s exposed as a single
    /// <see cref="ISampleProvider"/>. Effects are applied in the order they were added.
    /// Build the chain before playback; it is not safe to add effects concurrently with
    /// <see cref="Read"/>.
    /// </summary>
    public sealed class EffectChain : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly List<IAudioEffect> effects = new List<IAudioEffect>();

        /// <summary>
        /// Creates a chain that reads from <paramref name="source"/>.
        /// </summary>
        public EffectChain(ISampleProvider source)
        {
            ArgumentNullException.ThrowIfNull(source);
            this.source = source;
        }

        /// <summary>
        /// Appends an effect to the end of the chain, configuring it with the source
        /// format, and returns this chain so calls can be fluently chained.
        /// </summary>
        public EffectChain Add(IAudioEffect effect)
        {
            ArgumentNullException.ThrowIfNull(effect);
            effect.Configure(source.WaveFormat);
            effects.Add(effect);
            return this;
        }

        /// <summary>
        /// The effects in the chain, in processing order.
        /// </summary>
        public IReadOnlyList<IAudioEffect> Effects => effects;

        /// <summary>
        /// Total added latency of the chain, in samples per channel.
        /// </summary>
        public int LatencySamples
        {
            get
            {
                var total = 0;
                foreach (var effect in effects)
                    total += effect.LatencySamples;
                return total;
            }
        }

        /// <inheritdoc />
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <inheritdoc />
        public int Read(Span<float> buffer)
        {
            var samplesRead = source.Read(buffer);
            if (samplesRead <= 0)
                return samplesRead;
            var block = buffer.Slice(0, samplesRead);
            foreach (var effect in effects)
                effect.Process(block);
            return samplesRead;
        }
    }
}
