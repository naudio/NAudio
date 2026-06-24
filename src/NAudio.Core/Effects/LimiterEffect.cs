using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Brick-wall peak limiter with look-ahead and optional true-peak (inter-sample)
    /// detection. The signal is delayed by the look-ahead time while a channel-linked
    /// peak detector sees the audio early, so gain is already reduced by the time a
    /// transient reaches the output — no overshoot, no audible pumping from a hard
    /// catch. With <see cref="TruePeak"/> enabled the detector measures the oversampled
    /// signal, so reconstruction (inter-sample) peaks are caught too. Gain recovers with
    /// a smooth release. Reports its look-ahead as
    /// <see cref="AudioEffect.LatencySamples"/> for delay compensation.
    /// </summary>
    public sealed class LimiterEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Ceiling", "dB", -24f, 0f, () => CeilingDb, v => CeilingDb = v),
            EffectParameter.Continuous("Release", "ms", 1f, 500f, () => ReleaseMs, v => ReleaseMs = v),
            EffectParameter.Continuous("Look-ahead", "ms", 0.1f, 20f, () => LookaheadMs, v => LookaheadMs = v),
            EffectParameter.Toggle("True Peak", () => TruePeak, v => TruePeak = v),
            EffectParameter.Meter("Gain Reduction", "dB", 0f, 24f, () => GainReductionDb)
        };

        private DelayLine[] delays = Array.Empty<DelayLine>();
        private Oversampler[] detectors = Array.Empty<Oversampler>();
        // Monotonic-increasing ring deque holding the required gain for each sample
        // in the look-ahead window; the front is the minimum over the window.
        private float[] dqGain = Array.Empty<float>();
        private long[] dqPos = Array.Empty<long>();
        private int dqHead;
        private int dqTail;
        private long position;
        private float smoothedGain = 1f;
        private float releaseCoefficient;
        private int lookaheadSamples = 1;
        private float ceilingDb = -0.3f;
        private float ceilingLinear = 0.966f;
        private float releaseMs = 50f;
        private float lookaheadMs = 5f;
        private bool truePeak = true;
        private int oversampleFactor = 4;

        /// <summary>Output ceiling in dBFS (peaks are held at or below this). Default -0.3 dB.</summary>
        public float CeilingDb
        {
            get => ceilingDb;
            set
            {
                ceilingDb = value;
                ceilingLinear = MathF.Pow(10f, value * (1f / 20f));
            }
        }

        /// <summary>Release time in milliseconds. Default 50 ms.</summary>
        public float ReleaseMs
        {
            get => releaseMs;
            set
            {
                releaseMs = value < 0f ? 0f : value;
                RecomputeRelease();
            }
        }

        /// <summary>Look-ahead time in milliseconds. Default 5 ms. Must be positive.</summary>
        public float LookaheadMs
        {
            get => lookaheadMs;
            set => lookaheadMs = value < 0f ? 0f : value;
        }

        /// <summary>
        /// When true, detect inter-sample (true) peaks on the oversampled signal so
        /// reconstruction overshoot cannot exceed the ceiling. Default true.
        /// </summary>
        public bool TruePeak
        {
            get => truePeak;
            set
            {
                truePeak = value;
                if (WaveFormat != null)
                    BuildDetectors();
            }
        }

        /// <summary>Oversampling factor for true-peak detection: 1, 2 or 4. Default 4.</summary>
        public int OversampleFactor
        {
            get => oversampleFactor;
            set
            {
                oversampleFactor = value >= 4 ? 4 : value >= 2 ? 2 : 1;
                if (WaveFormat != null)
                    BuildDetectors();
            }
        }

        /// <summary>The most recent gain reduction in dB (≥ 0), for metering.</summary>
        public float GainReductionDb { get; private set; }

        /// <inheritdoc />
        public override int LatencySamples => lookaheadSamples;

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            lookaheadSamples = Math.Max(1, (int)MathF.Round(lookaheadMs * 0.001f * format.SampleRate));
            delays = new DelayLine[format.Channels];
            for (var ch = 0; ch < format.Channels; ch++)
                delays[ch] = new DelayLine(lookaheadSamples + 1);
            BuildDetectors();
            // Window spans the look-ahead sample plus its L predecessors (L+1
            // entries); +1 more for the ring's reserved empty slot.
            var capacity = lookaheadSamples + 2;
            dqGain = new float[capacity];
            dqPos = new long[capacity];
            dqHead = 0;
            dqTail = 0;
            position = 0;
            smoothedGain = 1f;
            GainReductionDb = 0f;
            RecomputeRelease();
        }

        private void BuildDetectors()
        {
            if (!truePeak || oversampleFactor == 1)
            {
                detectors = Array.Empty<Oversampler>();
                return;
            }
            detectors = new Oversampler[Channels];
            for (var ch = 0; ch < Channels; ch++)
                detectors[ch] = new Oversampler(oversampleFactor, SampleRate);
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            var useTruePeak = detectors.Length == channels;
            var capacity = dqGain.Length;
            Span<float> work = stackalloc float[4];
            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var peak = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    var sample = MathF.Abs(buffer[i + ch]);
                    if (sample > peak)
                        peak = sample;
                    if (useTruePeak)
                    {
                        // Inter-sample peaks on top of — never instead of — the
                        // raw sample peak: a band-limited upsampler attenuates
                        // near-Nyquist content, so the oversampled magnitude can
                        // read below the actual sample and must not lower the
                        // detected peak.
                        detectors[ch].Upsample(buffer[i + ch], work);
                        for (var k = 0; k < oversampleFactor; k++)
                        {
                            var u = MathF.Abs(work[k]);
                            if (u > peak)
                                peak = u;
                        }
                    }
                }

                var requiredGain = peak > ceilingLinear ? ceilingLinear / peak : 1f;

                // Sliding-window minimum of the required gain over the look-ahead
                // window. Because the signal is delayed by the same window, the
                // gain is already at its lowest needed value by the time a loud
                // sample reaches the output — provably no overshoot, while a
                // releasing detector evaluated at input time (the old design)
                // could recover before the delayed transient emerged.
                while (dqHead != dqTail)
                {
                    var back = dqTail == 0 ? capacity - 1 : dqTail - 1;
                    if (dqGain[back] >= requiredGain)
                        dqTail = back; // dominated — drop it
                    else
                        break;
                }
                dqGain[dqTail] = requiredGain;
                dqPos[dqTail] = position;
                dqTail = dqTail + 1 == capacity ? 0 : dqTail + 1;

                while (dqPos[dqHead] < position - lookaheadSamples)
                    dqHead = dqHead + 1 == capacity ? 0 : dqHead + 1;

                var windowMinGain = dqGain[dqHead];

                // Instant attack is safe (the whole window's minimum is known
                // now); recover with a click-free release.
                if (windowMinGain < smoothedGain)
                    smoothedGain = windowMinGain;
                else
                    smoothedGain = windowMinGain + (smoothedGain - windowMinGain) * releaseCoefficient;

                GainReductionDb = smoothedGain < 1f ? -20f * MathF.Log10(smoothedGain) : 0f;

                for (var ch = 0; ch < channels; ch++)
                {
                    delays[ch].Write(buffer[i + ch]);
                    buffer[i + ch] = delays[ch].Read(lookaheadSamples + 1) * smoothedGain;
                }
                position++;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            foreach (var delay in delays)
                delay.Reset();
            foreach (var detector in detectors)
                detector.Reset();
            dqHead = 0;
            dqTail = 0;
            position = 0;
            smoothedGain = 1f;
            GainReductionDb = 0f;
        }

        private void RecomputeRelease()
        {
            if (WaveFormat == null)
                return;
            releaseCoefficient = MathF.Exp(-1f / (releaseMs * 0.001f * SampleRate));
        }
    }
}
