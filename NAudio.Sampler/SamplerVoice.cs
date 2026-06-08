using System;
using NAudio.Dsp;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// A single playing note: one <see cref="SoundFontRegion"/>'s sample read at
    /// the pitch for the played key, shaped by a DAHDSR amplitude envelope, a
    /// resonant low-pass filter, two LFOs (modulation + vibrato) and a
    /// modulation envelope, and panned into the stereo output. Internal — owned
    /// and pooled by <see cref="SoundFontSampler"/>.
    ///
    /// Continuous modulation (LFO/mod-env → pitch, filter cutoff and volume) is
    /// computed at a control rate (every <see cref="ControlBlock"/> samples) so
    /// the per-sample loop stays cheap; the modulation sources themselves advance
    /// every sample so their phase stays accurate. The SF2 modulator <em>list</em>
    /// (file-defined and the other default modulators that map MIDI controllers
    /// to destinations) is a later step.
    /// </summary>
    internal sealed class SamplerVoice
    {
        // control-rate block: modulation-derived increments/coefficients are
        // recomputed this often (~1.5 ms at 44.1 kHz), the sources advance per sample
        private const int ControlBlock = 64;

        private readonly float[] samplePool;
        private readonly int outputSampleRate;
        private readonly double nyquist;

        private InterpolatingSampleReader reader;
        private readonly DahdsrEnvelope ampEnvelope;
        private readonly DahdsrEnvelope modEnvelope;
        private readonly Lfo modLfo;
        private readonly Lfo vibratoLfo;
        private BiQuadFilter filter;

        private double baseIncrement;   // sourceRate / outputRate
        private double pitchRatio;      // from key vs root + tuning
        private float leftGain;
        private float rightGain;
        private float staticGain;       // attenuation * velocity

        // modulation routing amounts (from generators)
        private double modLfoToPitch;       // cents
        private double vibLfoToPitch;       // cents
        private double modEnvToPitch;       // cents
        private double modLfoToFilter;      // cents
        private double modEnvToFilter;      // cents
        private double modLfoToVolume;      // centibels
        private double baseFilterCents;
        private float filterQ;
        private bool filterActive;

        // latest modulation-source values (bipolar -1..1 for LFOs, 0..1 for env)
        private float modLfoValue;
        private float vibratoLfoValue;
        private float modEnvValue;

        /// <summary>Creates a voice bound to a shared sample pool and output rate.</summary>
        public SamplerVoice(float[] samplePool, int outputSampleRate)
        {
            this.samplePool = samplePool;
            this.outputSampleRate = outputSampleRate;
            nyquist = outputSampleRate / 2.0;
            ampEnvelope = new DahdsrEnvelope(outputSampleRate);
            modEnvelope = new DahdsrEnvelope(outputSampleRate);
            modLfo = new Lfo(outputSampleRate) { Waveform = LfoWaveform.Triangle };
            vibratoLfo = new Lfo(outputSampleRate) { Waveform = LfoWaveform.Triangle };
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

            // amplitude: initial attenuation (cB) and a provisional velocity curve.
            // This v*v curve approximates the SF2 default velocity->attenuation
            // modulator; it is replaced by the real modulator when the modulator
            // list lands.
            int effectiveVelocity = gen.VelocityOverride >= 0 ? gen.VelocityOverride : velocity;
            double attenuationGain = SynthMath.AttenuationCentibelsToGain(gen[GeneratorEnum.InitialAttenuation]);
            float v = effectiveVelocity / 127f;
            staticGain = (float)(attenuationGain * v * v);

            SetPan(gen[GeneratorEnum.Pan]);
            ConfigureAmpEnvelope(gen);
            ConfigureModEnvelope(gen);
            ConfigureLfos(gen);
            ConfigureModulationAmounts(gen);
            ConfigureFilter(gen);

            modLfoValue = 0f;
            vibratoLfoValue = 0f;
            modEnvValue = 0f;

            Channel = channel;
            Note = note;
            ExclusiveClass = gen.ExclusiveClass;
            StartOrder = order;
            IsHeld = true;
            IsActive = true;
            ampEnvelope.Gate(true);
            modEnvelope.Gate(true);
            return true;
        }

        /// <summary>Releases the note (note-off): begins the envelope release and the loop tail.</summary>
        public void Release()
        {
            if (!IsActive) return;
            IsHeld = false;
            ampEnvelope.Gate(false);
            modEnvelope.Gate(false);
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
            modEnvelope.Gate(false);
            reader.Release();
        }

        /// <summary>
        /// Mixes this voice into an interleaved stereo buffer for a block of
        /// frames, applying the given channel pitch-bend ratio.
        /// </summary>
        public void Mix(Span<float> buffer, int frames, double pitchBendRatio)
        {
            if (!IsActive) return;

            int pos = 0;
            int remaining = frames;
            while (remaining > 0)
            {
                int sub = Math.Min(ControlBlock, remaining);

                // recompute modulation-derived parameters at control rate
                double pitchCents = modLfoToPitch * modLfoValue
                    + vibLfoToPitch * vibratoLfoValue
                    + modEnvToPitch * modEnvValue;
                double increment = baseIncrement * pitchRatio * pitchBendRatio
                    * SynthMath.CentsToRatio(pitchCents);

                float volGain = modLfoToVolume != 0.0
                    ? (float)SynthMath.CentibelsToGain(modLfoToVolume * modLfoValue)
                    : 1f;

                if (filterActive)
                {
                    double fc = baseFilterCents
                        + modEnvToFilter * modEnvValue
                        + modLfoToFilter * modLfoValue;
                    double hz = Math.Clamp(SynthMath.AbsoluteCentsToHertz(fc), 20.0, nyquist * 0.95);
                    filter.UpdateLowPassFilter(outputSampleRate, (float)hz, filterQ);
                }

                for (int i = 0; i < sub; i++)
                {
                    float s = reader.Read(increment);
                    if (reader.Ended) { IsActive = false; return; }
                    if (filterActive) s = filter.Transform(s);

                    float value = s * ampEnvelope.Process() * staticGain * volGain;
                    buffer[pos * 2] += value * leftGain;
                    buffer[pos * 2 + 1] += value * rightGain;
                    pos++;

                    // advance the modulation sources every sample (keeps phase accurate)
                    modLfoValue = modLfo.Process();
                    vibratoLfoValue = vibratoLfo.Process();
                    modEnvValue = modEnvelope.Process();

                    if (ampEnvelope.IsFinished) { IsActive = false; return; }
                }

                remaining -= sub;
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

        private void ConfigureAmpEnvelope(SoundFontGenerators gen)
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

        private void ConfigureModEnvelope(SoundFontGenerators gen)
        {
            modEnvelope.Reset();
            modEnvelope.DelaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DelayModulationEnvelope]);
            modEnvelope.AttackSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.AttackModulationEnvelope]);
            modEnvelope.HoldSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.HoldModulationEnvelope]);
            modEnvelope.DecaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DecayModulationEnvelope]);
            modEnvelope.ReleaseSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.ReleaseModulationEnvelope]);
            // sustainModEnv is in 0.1% units of full scale, decreasing from full
            double permille = gen[GeneratorEnum.SustainModulationEnvelope];
            modEnvelope.SustainLevel = (float)Math.Clamp(1.0 - permille / 1000.0, 0.0, 1.0);
        }

        private void ConfigureLfos(SoundFontGenerators gen)
        {
            modLfo.FrequencyHz = (float)SynthMath.AbsoluteCentsToHertz(gen[GeneratorEnum.FrequencyModulationLFO]);
            modLfo.DelaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DelayModulationLFO]);
            modLfo.Reset();

            vibratoLfo.FrequencyHz = (float)SynthMath.AbsoluteCentsToHertz(gen[GeneratorEnum.FrequencyVibratoLFO]);
            vibratoLfo.DelaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DelayVibratoLFO]);
            vibratoLfo.Reset();
        }

        private void ConfigureModulationAmounts(SoundFontGenerators gen)
        {
            modLfoToPitch = gen[GeneratorEnum.ModulationLFOToPitch];
            vibLfoToPitch = gen[GeneratorEnum.VibratoLFOToPitch];
            modEnvToPitch = gen[GeneratorEnum.ModulationEnvelopeToPitch];
            modLfoToFilter = gen[GeneratorEnum.ModulationLFOToFilterCutoffFrequency];
            modEnvToFilter = gen[GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency];
            modLfoToVolume = gen[GeneratorEnum.ModulationLFOToVolume];
        }

        private void ConfigureFilter(SoundFontGenerators gen)
        {
            baseFilterCents = gen[GeneratorEnum.InitialFilterCutoffFrequency];
            filterQ = Math.Max(0.5f, (float)SynthMath.ResonanceCentibelsToQ(gen[GeneratorEnum.InitialFilterQ]));

            double baseHz = SynthMath.AbsoluteCentsToHertz(baseFilterCents);
            // engage the filter if the base cutoff is audible, or if anything
            // modulates the cutoff (which could bring it down into range)
            filterActive = baseHz < nyquist * 0.95 || modLfoToFilter != 0.0 || modEnvToFilter != 0.0;

            if (filterActive)
            {
                double hz = Math.Clamp(baseHz, 20.0, nyquist * 0.95);
                // fresh filter: SetLowPassFilter resets state (safe at note start)
                filter = BiQuadFilter.LowPassFilter(outputSampleRate, (float)hz, filterQ);
            }
            else
            {
                filter = null;
            }
        }

        private static LoopMode MapLoopMode(SampleMode mode) => mode switch
        {
            SampleMode.LoopContinuously => LoopMode.Continuous,
            SampleMode.LoopAndContinue => LoopMode.UntilRelease,
            _ => LoopMode.None
        };
    }
}
