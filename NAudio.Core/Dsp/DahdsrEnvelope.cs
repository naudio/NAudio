using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Six-stage DAHDSR (Delay, Attack, Hold, Decay, Sustain, Release) envelope
    /// generator. This generalises the four-stage <see cref="EnvelopeGenerator"/>
    /// with the extra Delay and Hold stages that SoundFont and SFZ instruments
    /// require, and a seconds-based API (stage durations are set in seconds rather
    /// than raw rates). The output is a linear amplitude/modulation multiplier in
    /// the range [0, 1].
    ///
    /// The attack is convex (an exponential approach toward an over-shoot target),
    /// matching the SoundFont volume-envelope attack shape. Decay and release are
    /// constant-rate ramps following the SoundFont "time for a 100% change"
    /// semantic (SF2.04 §8.1.2): the stage time sets the ramp's <i>rate</i>, and
    /// the level the ramp runs to (the sustain level for decay, silence for
    /// release) <i>truncates</i> it, so a smaller change completes proportionally
    /// sooner. The ramp shape is selectable via <see cref="DecayReleaseShape"/>:
    /// exponential in amplitude (a constant fall in dB — the volume-envelope
    /// shape, gens 36/38) or linear in value (the modulation-envelope shape,
    /// gens 28/30). Designed for allocation-free real-time use.
    /// </summary>
    public class DahdsrEnvelope
    {
        /// <summary>
        /// Stages of the DAHDSR envelope.
        /// </summary>
        public enum EnvelopeStage
        {
            /// <summary>Idle (before the gate opens, output 0).</summary>
            Idle = 0,
            /// <summary>Delay (output held at 0 for the delay time).</summary>
            Delay,
            /// <summary>Attack (rising toward 1).</summary>
            Attack,
            /// <summary>Hold (output held at 1 for the hold time).</summary>
            Hold,
            /// <summary>Decay (falling toward the sustain level).</summary>
            Decay,
            /// <summary>Sustain (held at the sustain level until the gate closes).</summary>
            Sustain,
            /// <summary>Release (falling toward 0 after the gate closes).</summary>
            Release,
            /// <summary>Finished (release complete, output 0).</summary>
            Finished
        }

        /// <summary>
        /// The shape of the decay and release ramps (see <see cref="DecayReleaseShape"/>).
        /// </summary>
        public enum RampShape
        {
            /// <summary>
            /// Exponential in amplitude — a constant fall in dB per unit time,
            /// the SoundFont volume-envelope shape (SF2.04 §8.1.2 gens 36/38).
            /// </summary>
            Exponential = 0,
            /// <summary>
            /// Linear in value — the SoundFont modulation-envelope shape
            /// (SF2.04 §8.1.2 gens 28/30).
            /// </summary>
            Linear
        }

        private readonly float sampleRate;

        private float delaySeconds;
        private float attackSeconds;
        private float holdSeconds;
        private float decaySeconds;
        private float sustainLevel = 1f;
        private float releaseSeconds;

        private EnvelopeStage stage = EnvelopeStage.Idle;
        private float output;
        private int stageSampleCounter;

        // attack overshoot target ratio gives the convex attack curve
        private const float TargetRatioAttack = 0.3f;
        // the SF2 convention treats -100 dB as silence: the full decay/release
        // travel is 100 dB, and output below this floor is finished/at-sustain
        private const float SilenceFloor = 0.00001f; // -100 dB
        private const float FullScaleDecibels = 100f;

        private float attackCoef;
        private float attackBase;
        private float decayFactor;   // per-sample amplitude ratio (exponential shape)
        private float decayStep;     // per-sample fall (linear shape)
        private float releaseFactor;
        private float releaseStep;

        /// <summary>
        /// Creates a DAHDSR envelope for the given sample rate.
        /// </summary>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        public DahdsrEnvelope(float sampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            this.sampleRate = sampleRate;
            RecalculateAttack();
            RecalculateDecay();
            RecalculateRelease();
        }

        /// <summary>Delay time in seconds (output held at 0 before the attack begins).</summary>
        public float DelaySeconds
        {
            get => delaySeconds;
            set => delaySeconds = Math.Max(0f, value);
        }

        /// <summary>Attack time in seconds (time to rise from 0 to 1).</summary>
        public float AttackSeconds
        {
            get => attackSeconds;
            set { attackSeconds = Math.Max(0f, value); RecalculateAttack(); }
        }

        /// <summary>Hold time in seconds (output held at 1 after the attack completes).</summary>
        public float HoldSeconds
        {
            get => holdSeconds;
            set => holdSeconds = Math.Max(0f, value);
        }

        /// <summary>
        /// Decay time in seconds. Per the SoundFont semantic (SF2.04 §8.1.2 gens
        /// 36/28) this is the time for a 100% change — full scale down to -100 dB
        /// (exponential shape) or to zero (linear shape) — and sets the ramp's
        /// <i>rate</i>; the sustain level truncates the ramp, so a high sustain is
        /// reached proportionally sooner (e.g. a 2 s decay to a -1 dB sustain
        /// completes in 2 s x 1/100 = 20 ms).
        /// </summary>
        public float DecaySeconds
        {
            get => decaySeconds;
            set { decaySeconds = Math.Max(0f, value); RecalculateDecay(); }
        }

        /// <summary>Sustain level in the range [0, 1].</summary>
        public float SustainLevel
        {
            get => sustainLevel;
            set => sustainLevel = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Release time in seconds. Like the decay this is the time for a 100%
        /// change (SF2.04 §8.1.2 gens 38/30), so it sets the ramp's <i>rate</i>
        /// and releasing from below full scale completes proportionally sooner —
        /// e.g. releasing from a -20 dB sustain takes (80/100) x ReleaseSeconds
        /// with the exponential shape.
        /// </summary>
        public float ReleaseSeconds
        {
            get => releaseSeconds;
            set { releaseSeconds = Math.Max(0f, value); RecalculateRelease(); }
        }

        /// <summary>
        /// The shape of the decay and release ramps: exponential in amplitude
        /// (constant dB rate — the SF2 volume-envelope shape, the default) or
        /// linear in value (the SF2 modulation-envelope shape).
        /// </summary>
        public RampShape DecayReleaseShape { get; set; } = RampShape.Exponential;

        /// <summary>The current envelope stage.</summary>
        public EnvelopeStage Stage => stage;

        /// <summary>The most recent output value.</summary>
        public float Output => output;

        /// <summary>True once the envelope has completed its release and gone idle.</summary>
        public bool IsFinished => stage == EnvelopeStage.Finished || stage == EnvelopeStage.Idle;

        /// <summary>
        /// Opens (note on) or closes (note off) the gate. Opening restarts the
        /// envelope from its delay stage; closing moves to the release stage
        /// unless the envelope is already idle/finished.
        /// </summary>
        public void Gate(bool on)
        {
            if (on)
            {
                stage = EnvelopeStage.Delay;
                stageSampleCounter = SecondsToSamples(delaySeconds);
                // a zero delay is consumed within the first Process call (see the
                // stage loop there), so the attack starts on the very first sample
            }
            else if (stage != EnvelopeStage.Idle && stage != EnvelopeStage.Finished)
            {
                stage = EnvelopeStage.Release;
            }
        }

        /// <summary>
        /// Resets the envelope to idle with an output of 0.
        /// </summary>
        public void Reset()
        {
            stage = EnvelopeStage.Idle;
            output = 0f;
            stageSampleCounter = 0;
        }

        /// <summary>
        /// Advances the envelope by one sample and returns the new output value.
        /// </summary>
        public float Process()
        {
            // Loop so that zero-duration stages (e.g. a zero delay before a zero
            // attack) advance within a single call rather than each costing a
            // sample. Only the Delay->Attack and Hold->Decay transitions loop;
            // every other branch returns, so this runs at most a few iterations.
            while (true)
            {
                switch (stage)
                {
                    case EnvelopeStage.Idle:
                    case EnvelopeStage.Finished:
                        return output;
                    case EnvelopeStage.Delay:
                        if (stageSampleCounter > 0)
                        {
                            stageSampleCounter--;
                            output = 0f;
                            return output;
                        }
                        stage = EnvelopeStage.Attack;
                        continue;
                    case EnvelopeStage.Attack:
                        if (attackSeconds <= 0f)
                        {
                            output = 1f;
                            EnterHold();
                        }
                        else
                        {
                            output = attackBase + output * attackCoef;
                            if (output >= 1f)
                            {
                                output = 1f;
                                EnterHold();
                            }
                        }
                        return output;
                    case EnvelopeStage.Hold:
                        if (stageSampleCounter > 0)
                        {
                            stageSampleCounter--;
                            output = 1f;
                            return output;
                        }
                        stage = EnvelopeStage.Decay;
                        continue;
                    case EnvelopeStage.Decay:
                        if (decaySeconds <= 0f)
                        {
                            output = sustainLevel;
                            stage = EnvelopeStage.Sustain;
                        }
                        else
                        {
                            // constant-rate ramp truncated by the sustain level
                            // (SF2.04 §8.1.2 gens 36/28); a sustain below the
                            // -100 dB floor ends the ramp at the floor
                            output = DecayReleaseShape == RampShape.Linear
                                ? output - decayStep
                                : output * decayFactor;
                            if (output <= sustainLevel || output <= SilenceFloor)
                            {
                                output = sustainLevel;
                                stage = EnvelopeStage.Sustain;
                            }
                        }
                        return output;
                    case EnvelopeStage.Sustain:
                        output = sustainLevel;
                        return output;
                    default: // Release
                        if (releaseSeconds <= 0f)
                        {
                            output = 0f;
                            stage = EnvelopeStage.Finished;
                        }
                        else
                        {
                            // constant-rate ramp from the current level toward
                            // silence (SF2.04 §8.1.2 gens 38/30)
                            output = DecayReleaseShape == RampShape.Linear
                                ? output - releaseStep
                                : output * releaseFactor;
                            if (output <= SilenceFloor)
                            {
                                output = 0f;
                                stage = EnvelopeStage.Finished;
                            }
                        }
                        return output;
                }
            }
        }

        private void EnterHold()
        {
            stage = EnvelopeStage.Hold;
            stageSampleCounter = SecondsToSamples(holdSeconds);
        }

        private int SecondsToSamples(float seconds)
        {
            return (int)(seconds * sampleRate);
        }

        private void RecalculateAttack()
        {
            float rate = Math.Max(1f, attackSeconds * sampleRate);
            attackCoef = CalcCoef(rate, TargetRatioAttack);
            attackBase = (1f + TargetRatioAttack) * (1f - attackCoef);
        }

        private void RecalculateDecay()
        {
            float samples = Math.Max(1f, decaySeconds * sampleRate);
            // exponential: a constant FullScaleDecibels fall over decaySeconds
            decayFactor = (float)Math.Pow(10.0, -FullScaleDecibels / 20.0 / samples);
            // linear: a full-scale (1.0) fall over decaySeconds
            decayStep = 1f / samples;
        }

        private void RecalculateRelease()
        {
            float samples = Math.Max(1f, releaseSeconds * sampleRate);
            releaseFactor = (float)Math.Pow(10.0, -FullScaleDecibels / 20.0 / samples);
            releaseStep = 1f / samples;
        }

        private static float CalcCoef(float rate, float targetRatio)
        {
            return (float)Math.Exp(-Math.Log((1.0 + targetRatio) / targetRatio) / rate);
        }
    }
}
