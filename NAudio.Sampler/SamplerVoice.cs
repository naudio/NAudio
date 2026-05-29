using System;
using NAudio.Dsp;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// A single playing note: one <see cref="SoundFontRegion"/>'s sample read at
    /// the pitch for the played key, shaped by a DAHDSR amplitude envelope, an
    /// optional static low-pass filter, and panned into the stereo output.
    /// Internal — owned and pooled by <see cref="SoundFontSampler"/>.
    /// </summary>
    internal sealed class SamplerVoice
    {
        private readonly float[] samplePool;
        private readonly int outputSampleRate;

        private InterpolatingSampleReader reader;
        private readonly DahdsrEnvelope ampEnvelope;
        private BiQuadFilter filter;

        private double baseIncrement;   // sourceRate / outputRate
        private double pitchRatio;      // from key vs root + tuning
        private float leftGain;
        private float rightGain;
        private float staticGain;       // attenuation * velocity

        /// <summary>Creates a voice bound to a shared sample pool and output rate.</summary>
        public SamplerVoice(float[] samplePool, int outputSampleRate)
        {
            this.samplePool = samplePool;
            this.outputSampleRate = outputSampleRate;
            ampEnvelope = new DahdsrEnvelope(outputSampleRate);
        }

        /// <summary>Whether this voice is currently producing sound.</summary>
        public bool IsActive { get; private set; }

        /// <summary>The MIDI channel that triggered this voice.</summary>
        public int Channel { get; private set; }

        /// <summary>The MIDI note that triggered this voice.</summary>
        public int Note { get; private set; }

        /// <summary>The region's exclusive (choke) class, or 0 for none.</summary>
        public int ExclusiveClass { get; private set; }

        /// <summary>Whether the note is being held (gate open, before note-off).</summary>
        public bool IsHeld { get; private set; }

        /// <summary>A monotonically increasing trigger order, used for voice stealing.</summary>
        public long StartOrder { get; private set; }

        /// <summary>The current envelope output level (0..1), used to pick the quietest voice to steal.</summary>
        public float Level => ampEnvelope.Output;

        /// <summary>
        /// Starts this voice playing a region for a given key and velocity.
        /// Returns false if the region's sample addressing is unusable.
        /// </summary>
        public bool Start(SoundFontRegion region, int channel, int note, int velocity, long order)
        {
            var gen = region.Generators;
            var sample = region.Sample;

            int start = (int)sample.Start + gen.StartAddressOffset;
            int end = (int)sample.End + gen.EndAddressOffset;
            int loopStart = (int)sample.StartLoop + gen.StartLoopAddressOffset;
            int loopEnd = (int)sample.EndLoop + gen.EndLoopAddressOffset;

            if (start < 0 || end > samplePool.Length || start >= end) return false;

            var loopMode = MapLoopMode(gen.SampleModes);
            if (loopMode != LoopMode.None &&
                (loopStart < start || loopEnd > end || loopStart >= loopEnd))
            {
                loopMode = LoopMode.None; // malformed loop points — play as one-shot
            }

            var source = new SampleSource(samplePool, (int)sample.SampleRate, loopMode,
                start, end,
                loopMode == LoopMode.None ? null : loopStart,
                loopMode == LoopMode.None ? null : loopEnd);
            reader = new InterpolatingSampleReader(source);

            // pitch: cents from played key vs root, plus tuning generators
            int effectiveKey = gen.KeyNumberOverride >= 0 ? gen.KeyNumberOverride : note;
            int rootKey = gen.OverridingRootKey >= 0 ? gen.OverridingRootKey : sample.OriginalPitch;
            double scaleTuning = gen[GeneratorEnum.ScaleTuning];
            double cents = (effectiveKey - rootKey) * scaleTuning
                + gen[GeneratorEnum.CoarseTune] * 100.0
                + gen[GeneratorEnum.FineTune]
                + sample.PitchCorrection;
            pitchRatio = SynthMath.CentsToRatio(cents);
            baseIncrement = (double)sample.SampleRate / outputSampleRate;

            // amplitude: initial attenuation (cB) and a provisional velocity curve
            int effectiveVelocity = gen.VelocityOverride >= 0 ? gen.VelocityOverride : velocity;
            double attenuationGain = SynthMath.AttenuationCentibelsToGain(gen[GeneratorEnum.InitialAttenuation]);
            float v = effectiveVelocity / 127f;
            staticGain = (float)(attenuationGain * v * v); // concave-ish; refined when the modulator engine lands

            SetPan(gen[GeneratorEnum.Pan]);
            ConfigureEnvelope(gen);
            ConfigureFilter(gen);

            Channel = channel;
            Note = note;
            ExclusiveClass = gen.ExclusiveClass;
            StartOrder = order;
            IsHeld = true;
            IsActive = true;
            ampEnvelope.Gate(true);
            return true;
        }

        /// <summary>Releases the note (note-off): begins the envelope release and the loop tail.</summary>
        public void Release()
        {
            if (!IsActive) return;
            IsHeld = false;
            ampEnvelope.Gate(false);
            reader.Release();
        }

        /// <summary>
        /// Chokes the voice with a short fade (for exclusive-class / all-sound-off),
        /// avoiding the click of a hard cut.
        /// </summary>
        public void Choke()
        {
            if (!IsActive) return;
            IsHeld = false;
            ampEnvelope.ReleaseSeconds = 0.005f;
            ampEnvelope.Gate(false);
            reader.Release();
        }

        /// <summary>
        /// Mixes this voice into an interleaved stereo buffer for a block of
        /// frames, applying the given channel pitch-bend ratio.
        /// </summary>
        public void Mix(Span<float> buffer, int frames, double pitchBendRatio)
        {
            if (!IsActive) return;
            double increment = baseIncrement * pitchRatio * pitchBendRatio;

            for (int f = 0; f < frames; f++)
            {
                float s = reader.Read(increment);
                if (reader.Ended)
                {
                    IsActive = false;
                    return;
                }
                if (filter != null) s = filter.Transform(s);

                float value = s * ampEnvelope.Process() * staticGain;
                buffer[f * 2] += value * leftGain;
                buffer[f * 2 + 1] += value * rightGain;

                if (ampEnvelope.IsFinished)
                {
                    IsActive = false;
                    return;
                }
            }
        }

        private void SetPan(int panGenerator)
        {
            // SF2 pan: -500 = hard left, +500 = hard right, in 0.1% units
            float pan = Math.Clamp(panGenerator / 500f, -1f, 1f);
            double angle = (pan + 1.0) * (Math.PI / 4.0); // equal-power
            leftGain = (float)Math.Cos(angle);
            rightGain = (float)Math.Sin(angle);
        }

        private void ConfigureEnvelope(SoundFontGenerators gen)
        {
            ampEnvelope.Reset();
            ampEnvelope.DelaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DelayVolumeEnvelope]);
            ampEnvelope.AttackSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.AttackVolumeEnvelope]);
            ampEnvelope.HoldSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.HoldVolumeEnvelope]);
            ampEnvelope.DecaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DecayVolumeEnvelope]);
            ampEnvelope.ReleaseSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.ReleaseVolumeEnvelope]);
            // sustainVolEnv is attenuation in centibels (0 = full level)
            double sustain = SynthMath.AttenuationCentibelsToGain(gen[GeneratorEnum.SustainVolumeEnvelope]);
            ampEnvelope.SustainLevel = (float)Math.Clamp(sustain, 0.0, 1.0);
        }

        private void ConfigureFilter(SoundFontGenerators gen)
        {
            double cutoffHz = SynthMath.AbsoluteCentsToHertz(gen[GeneratorEnum.InitialFilterCutoffFrequency]);
            double nyquist = outputSampleRate / 2.0;
            if (cutoffHz >= nyquist * 0.95)
            {
                filter = null; // effectively open — skip filtering
                return;
            }
            float q = (float)SynthMath.ResonanceCentibelsToQ(gen[GeneratorEnum.InitialFilterQ]);
            filter = BiQuadFilter.LowPassFilter(outputSampleRate, (float)Math.Max(20.0, cutoffHz), Math.Max(0.5f, q));
        }

        private static LoopMode MapLoopMode(SampleMode mode) => mode switch
        {
            SampleMode.LoopContinuously => LoopMode.Continuous,
            SampleMode.LoopAndContinue => LoopMode.UntilRelease,
            _ => LoopMode.None
        };
    }
}
