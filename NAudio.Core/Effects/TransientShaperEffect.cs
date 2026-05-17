using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Transient shaper. A fast and a slow envelope follower track the signal; their
    /// difference distinguishes the attack (onset) of a sound from its sustain (body).
    /// <see cref="AttackDb"/> boosts or cuts the attack portion and
    /// <see cref="SustainDb"/> the sustain portion, independent of input level.
    /// Detection is channel-linked so the stereo image is preserved.
    /// </summary>
    public sealed class TransientShaperEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Attack", "dB", -20f, 20f, () => AttackDb, v => AttackDb = v),
            EffectParameter.Continuous("Sustain", "dB", -20f, 20f, () => SustainDb, v => SustainDb = v),
            EffectParameter.Continuous("Fast", "ms", 0.1f, 20f, () => FastMs, v => FastMs = v),
            EffectParameter.Continuous("Slow", "ms", 10f, 500f, () => SlowMs, v => SlowMs = v)
        };

        private EnvelopeFollower fast;
        private EnvelopeFollower slow;

        /// <summary>Attack (transient) gain in dB; positive sharpens, negative softens. Default 0.</summary>
        public float AttackDb { get; set; }

        /// <summary>Sustain (body) gain in dB; positive lengthens, negative tightens. Default 0.</summary>
        public float SustainDb { get; set; }

        /// <summary>Fast-envelope time constant in ms. Default 2 ms.</summary>
        public float FastMs { get; set; } = 2f;

        /// <summary>Slow-envelope time constant in ms. Default 80 ms.</summary>
        public float SlowMs { get; set; } = 80f;

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            fast = new EnvelopeFollower(FastMs, FastMs, format.SampleRate);
            slow = new EnvelopeFollower(SlowMs, SlowMs, format.SampleRate);
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            fast.AttackMilliseconds = fast.ReleaseMilliseconds = FastMs;
            slow.AttackMilliseconds = slow.ReleaseMilliseconds = SlowMs;

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var peak = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    var a = MathF.Abs(buffer[i + ch]);
                    if (a > peak)
                        peak = a;
                }

                var f = fast.ProcessRectified(peak);
                var s = slow.ProcessRectified(peak);

                var fDb = 20f * MathF.Log10(f < 1e-9f ? 1e-9f : f);
                var sDb = 20f * MathF.Log10(s < 1e-9f ? 1e-9f : s);
                var diff = fDb - sDb; // > 0 during the attack, < 0 during the decay

                // Scale the trim by how strong the transient/decay is (over ~12 dB).
                float gainDb;
                if (diff > 0f)
                    gainDb = AttackDb * MathF.Min(1f, diff / 12f);
                else
                    gainDb = SustainDb * MathF.Min(1f, -diff / 12f);

                var gain = MathF.Pow(10f, gainDb * (1f / 20f));
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] *= gain;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            fast?.Reset();
            slow?.Reset();
        }
    }
}
