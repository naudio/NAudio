using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// An ordered chain of <see cref="IAudioEffect"/>s exposed as a single
    /// <see cref="ISampleProvider"/>. Effects are applied in the order they were added.
    /// </summary>
    /// <remarks>
    /// The chain may be edited (<see cref="Add"/>, <see cref="Insert"/>,
    /// <see cref="RemoveAt"/>, <see cref="Move"/>) from one thread while another thread
    /// calls <see cref="Read"/>: each edit publishes a new immutable array with a single
    /// atomic write, so a concurrent <see cref="Read"/> always sees either the pre- or
    /// post-edit chain, never a partial state, and never takes a lock. Edits are serialized
    /// against each other by an internal lock, so multiple editor threads are also safe.
    /// New effects are configured for the source format on the editing thread before they
    /// are published, so configuration never lands on the audio thread.
    /// </remarks>
    public sealed class EffectChain : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly object gate = new object(); // serializes edits; never held during Read
        private volatile IAudioEffect[] effects = Array.Empty<IAudioEffect>();

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
            Insert(effects.Length, effect);
            return this;
        }

        /// <summary>
        /// Inserts an effect at <paramref name="index"/>, configuring it with the source
        /// format. Existing effects keep their state (they are not reconfigured).
        /// </summary>
        public void Insert(int index, IAudioEffect effect)
        {
            ArgumentNullException.ThrowIfNull(effect);
            effect.Configure(source.WaveFormat); // off the audio thread, before publish
            lock (gate)
            {
                if ((uint)index > (uint)effects.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                var next = new IAudioEffect[effects.Length + 1];
                Array.Copy(effects, 0, next, 0, index);
                next[index] = effect;
                Array.Copy(effects, index, next, index + 1, effects.Length - index);
                effects = next; // atomic publish
            }
        }

        /// <summary>
        /// Removes the effect at <paramref name="index"/> from the chain. The removed
        /// effect simply stops being processed; its own state is left untouched.
        /// </summary>
        public void RemoveAt(int index)
        {
            lock (gate)
            {
                if ((uint)index >= (uint)effects.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                var next = new IAudioEffect[effects.Length - 1];
                Array.Copy(effects, 0, next, 0, index);
                Array.Copy(effects, index + 1, next, index, next.Length - index);
                effects = next; // atomic publish
            }
        }

        /// <summary>
        /// Moves the effect at <paramref name="oldIndex"/> to <paramref name="newIndex"/>,
        /// reordering the chain without reconfiguring or resetting any effect.
        /// </summary>
        public void Move(int oldIndex, int newIndex)
        {
            lock (gate)
            {
                var current = effects;
                if ((uint)oldIndex >= (uint)current.Length)
                    throw new ArgumentOutOfRangeException(nameof(oldIndex));
                if ((uint)newIndex >= (uint)current.Length)
                    throw new ArgumentOutOfRangeException(nameof(newIndex));
                if (oldIndex == newIndex)
                    return;
                var next = (IAudioEffect[])current.Clone();
                var moved = next[oldIndex];
                if (oldIndex < newIndex)
                    Array.Copy(next, oldIndex + 1, next, oldIndex, newIndex - oldIndex);
                else
                    Array.Copy(next, newIndex, next, newIndex + 1, oldIndex - newIndex);
                next[newIndex] = moved;
                effects = next; // atomic publish
            }
        }

        /// <summary>
        /// A point-in-time snapshot of the effects in the chain, in processing order.
        /// Does not reflect edits made after it is read.
        /// </summary>
        public IReadOnlyList<IAudioEffect> Effects => effects;

        /// <summary>
        /// Total added latency of the chain, in samples per channel.
        /// </summary>
        public int LatencySamples
        {
            get
            {
                var chain = effects;
                var total = 0;
                for (var i = 0; i < chain.Length; i++)
                    total += chain[i].LatencySamples;
                return total;
            }
        }

        /// <summary>
        /// Resets every effect in the chain, clearing their internal state (delay lines,
        /// filter history, reverb tails) without changing the chain or its configuration.
        /// Use when reusing the chain on a new signal, e.g. after seeking the source.
        /// </summary>
        public void Reset()
        {
            var chain = effects;
            for (var i = 0; i < chain.Length; i++)
                chain[i].Reset();
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
            var chain = effects; // single volatile read; lock-free
            for (var i = 0; i < chain.Length; i++)
                chain[i].Process(block);
            return samplesRead;
        }
    }
}
