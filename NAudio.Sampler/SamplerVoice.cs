using System;
using NAudio.Dsp;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// A single playing note: one <see cref="SamplerRegion"/>'s sample read at
    /// the pitch for the played key, shaped by a DAHDSR amplitude envelope, a
    /// resonant low-pass filter, two LFOs (modulation + vibrato) and a
    /// modulation envelope, and panned into the stereo output. Internal — owned
    /// and pooled by <see cref="SoundFontSampler"/>. The voice is format-neutral:
    /// it reads only the <see cref="SamplerRegion"/>, so SoundFont and SFZ play
    /// through the same engine.
    ///
    /// Continuous modulation (LFO/mod-env → pitch, filter cutoff and volume) is
    /// computed at a control rate (every <see cref="ControlBlock"/> samples) so
    /// the per-sample loop stays cheap; the modulation sources themselves advance
    /// every sample so their phase stays accurate. The SF2 modulator list is
    /// evaluated here too (against the region's <see cref="ModulatorSet"/>).
    /// </summary>
    internal sealed class SamplerVoice
    {
        // control-rate block: modulation-derived increments/coefficients are
        // recomputed this often (~1.5 ms at 44.1 kHz), the sources advance per sample
        private const int ControlBlock = 64;

        private readonly int outputSampleRate;
        private readonly double nyquist;

        private InterpolatingSampleReader reader;       // mono / left channel
        private InterpolatingSampleReader readerRight;  // right channel (stereo only), advanced in lockstep
        private bool stereo;
        private readonly DahdsrEnvelope ampEnvelope;
        private readonly DahdsrEnvelope modEnvelope;
        private readonly Lfo modLfo;
        private readonly Lfo vibratoLfo;
        private BiQuadFilter filter;        // mono / left channel
        private BiQuadFilter filterRight;   // right channel (stereo only)

        private double baseIncrement;   // sourceRate / outputRate
        private double pitchRatio;      // from key vs root + tuning
        private float leftGain;
        private float rightGain;
        private float staticGain;       // gain from initial attenuation (incl. modulators)

        // base modulation routing amounts and destinations (from generators); the
        // modulator engine adds its per-control-block deltas on top of these
        private double baseModLfoToPitch;   // cents
        private double baseVibLfoToPitch;   // cents
        private double baseModEnvToPitch;   // cents
        private double baseModLfoToFilter;  // cents
        private double baseModEnvToFilter;  // cents
        private double baseModLfoToVolume;  // centibels
        private SamplerFilterType filterType; // low/high/band pass
        private double baseFilterCents;     // initial filter cutoff (absolute cents)
        private double baseAttenuationCb;   // initial attenuation (centibels)
        private double velocityAttenuationCb; // velocity->amplitude tracking (centibels, SFZ amp_veltrack)
        private double basePan;             // pan generator (0.1% units, ±500)
        private double baseReverbSend;      // reverb send (0.1% units, 0..1000)
        private double baseChorusSend;      // chorus send (0.1% units, 0..1000)
        private float filterQ;
        private bool filterActive;

        // the resolved SF2 modulator list and the fixed note state it reads
        private ModulatorSet modulators;
        private int velocity;
        private int key;
        // per-control-block modulator output, summed by GeneratorEnum destination
        private readonly double[] modulation = new double[(int)GeneratorEnum.UnusedEnd + 1];

        // latest modulation-source values (bipolar -1..1 for LFOs, 0..1 for env)
        private float modLfoValue;
        private float vibratoLfoValue;
        private float modEnvValue;

        /// <summary>Creates a voice for the given output rate.</summary>
        public SamplerVoice(int outputSampleRate)
        {
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

        /// <summary>The region's choke group, or 0 for none.</summary>
        public int Group { get; private set; }

        /// <summary>Whether this voice ignores note-off and plays to the end (one-shot).</summary>
        public bool IgnoreNoteOff { get; private set; }

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
        public bool Start(SamplerRegion region, MidiChannelState channelState,
            int channel, int note, int velocity, long order)
        {
            var gen = region.Generators;
            var sample = region.Sample;

            int start = sample.Start + gen.StartAddressOffset;
            int end = sample.End + gen.EndAddressOffset;
            int loopStart = sample.LoopStart + gen.StartLoopAddressOffset;
            int loopEnd = sample.LoopEnd + gen.EndLoopAddressOffset;

            if (start < 0 || end > sample.Data.Length || start >= end) return false;

            var loopMode = MapLoopMode(gen.SampleModes);
            if (loopMode != LoopMode.None &&
                (loopStart < start || loopEnd > end || loopStart >= loopEnd))
            {
                loopMode = LoopMode.None; // malformed loop points — play as one-shot
            }

            int? loopStartOrNull = loopMode == LoopMode.None ? null : loopStart;
            int? loopEndOrNull = loopMode == LoopMode.None ? null : loopEnd;
            reader = new InterpolatingSampleReader(
                new SampleSource(sample.Data, sample.SampleRate, loopMode, start, end, loopStartOrNull, loopEndOrNull));

            // a stereo sample runs a second reader over the right channel in
            // lockstep (same bounds, loop and increment), so the core reader stays mono
            stereo = sample.IsStereo && sample.DataRight.Length >= end;
            readerRight = stereo
                ? new InterpolatingSampleReader(
                    new SampleSource(sample.DataRight, sample.SampleRate, loopMode, start, end, loopStartOrNull, loopEndOrNull))
                : null;

            // pitch: cents from played key vs root, plus tuning generators
            int effectiveKey = gen.KeyNumberOverride >= 0 ? gen.KeyNumberOverride : note;
            int rootKey = gen.OverridingRootKey >= 0 ? gen.OverridingRootKey : sample.RootKey;
            double scaleTuning = gen[GeneratorEnum.ScaleTuning];
            double cents = (effectiveKey - rootKey) * scaleTuning
                + gen[GeneratorEnum.CoarseTune] * 100.0
                + gen[GeneratorEnum.FineTune]
                + sample.PitchCorrectionCents;
            pitchRatio = SynthMath.CentsToRatio(cents);
            baseIncrement = (double)sample.SampleRate / outputSampleRate;

            // the velocity/key the modulators see (overrides take precedence)
            int effectiveVelocity = gen.VelocityOverride >= 0 ? gen.VelocityOverride : velocity;
            this.modulators = region.Modulators;
            this.velocity = effectiveVelocity;
            this.key = effectiveKey;
            this.filterType = region.FilterType;
            velocityAttenuationCb = VelocityAttenuation(region.VelocityTrackingPercent, effectiveVelocity);

            // base (generator) values; the SF2 modulators add to these. The
            // velocity->attenuation and velocity->filter default modulators
            // (replacing the old provisional v*v curve) are applied in UpdateModulation.
            baseAttenuationCb = gen[GeneratorEnum.InitialAttenuation];
            basePan = gen[GeneratorEnum.Pan];

            ConfigureAmpEnvelope(gen);
            ConfigureModEnvelope(gen);
            ConfigureLfos(gen);
            ConfigureModulationAmounts(gen);

            modLfoValue = 0f;
            vibratoLfoValue = 0f;
            modEnvValue = 0f;

            // evaluate the modulators once up front so the initial gain, pan and
            // filter state (and the decision to engage the filter) reflect the
            // note-on velocity and the channel's current controllers
            UpdateModulation(channelState);
            ConfigureFilter(gen);

            Channel = channel;
            Note = note;
            Group = region.Group;
            IgnoreNoteOff = region.IgnoreNoteOff;
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
            readerRight?.Release();
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
            readerRight?.Release();
        }

        /// <summary>
        /// Mixes this voice into an interleaved stereo buffer for a block of
        /// frames, reading the channel's live controllers (pitch-bend and the SF2
        /// modulator sources) at control rate. A portion of the voice's signal,
        /// set by the reverb/chorus send levels, is also added to the
        /// <paramref name="reverbSend"/> and <paramref name="chorusSend"/> buffers
        /// (the same length as <paramref name="buffer"/>).
        /// </summary>
        public void Mix(Span<float> buffer, Span<float> reverbSend, Span<float> chorusSend,
            int frames, MidiChannelState channel)
        {
            if (!IsActive) return;

            double pitchBendRatio = channel.PitchBendRatio;
            int pos = 0;
            int remaining = frames;
            while (remaining > 0)
            {
                int sub = Math.Min(ControlBlock, remaining);

                // re-evaluate the SF2 modulators and recompute modulation-derived
                // parameters at control rate
                UpdateModulation(channel);

                // reverb/chorus send levels: 0.1% units (0..1000) -> 0..1 gain
                float reverbSendGain = SendGain(baseReverbSend + modulation[(int)GeneratorEnum.ReverbEffectsSend]);
                float chorusSendGain = SendGain(baseChorusSend + modulation[(int)GeneratorEnum.ChorusEffectsSend]);

                double vibToPitch = baseVibLfoToPitch + modulation[(int)GeneratorEnum.VibratoLFOToPitch];
                double modLfoPitch = baseModLfoToPitch + modulation[(int)GeneratorEnum.ModulationLFOToPitch];
                double modEnvPitch = baseModEnvToPitch + modulation[(int)GeneratorEnum.ModulationEnvelopeToPitch];
                double pitchCents = modLfoPitch * modLfoValue
                    + vibToPitch * vibratoLfoValue
                    + modEnvPitch * modEnvValue;
                double increment = baseIncrement * pitchRatio * pitchBendRatio
                    * SynthMath.CentsToRatio(pitchCents);

                double volCb = (baseModLfoToVolume + modulation[(int)GeneratorEnum.ModulationLFOToVolume]) * modLfoValue;
                float volGain = volCb != 0.0 ? (float)SynthMath.CentibelsToGain(volCb) : 1f;

                if (filterActive)
                {
                    double modLfoFilter = baseModLfoToFilter + modulation[(int)GeneratorEnum.ModulationLFOToFilterCutoffFrequency];
                    double modEnvFilter = baseModEnvToFilter + modulation[(int)GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency];
                    double fc = baseFilterCents
                        + modulation[(int)GeneratorEnum.InitialFilterCutoffFrequency]
                        + modEnvFilter * modEnvValue
                        + modLfoFilter * modLfoValue;
                    double hz = Math.Clamp(SynthMath.AbsoluteCentsToHertz(fc), 20.0, nyquist * 0.95);
                    RetuneFilter(filter, (float)hz);
                    if (stereo) RetuneFilter(filterRight, (float)hz);
                }

                for (int i = 0; i < sub; i++)
                {
                    float sL = reader.Read(increment);
                    if (reader.Ended) { IsActive = false; return; }
                    float sR = stereo ? readerRight.Read(increment) : sL;
                    if (filterActive)
                    {
                        sL = filter.Transform(sL);
                        sR = stereo ? filterRight.Transform(sR) : sL;
                    }

                    float envGain = ampEnvelope.Process() * staticGain * volGain;
                    float left = sL * envGain * leftGain;
                    float right = sR * envGain * rightGain;
                    buffer[pos * 2] += left;
                    buffer[pos * 2 + 1] += right;
                    if (reverbSendGain > 0f)
                    {
                        reverbSend[pos * 2] += left * reverbSendGain;
                        reverbSend[pos * 2 + 1] += right * reverbSendGain;
                    }
                    if (chorusSendGain > 0f)
                    {
                        chorusSend[pos * 2] += left * chorusSendGain;
                        chorusSend[pos * 2 + 1] += right * chorusSendGain;
                    }
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

        /// <summary>
        /// Re-evaluates the SF2 modulator list against the channel's live
        /// controllers and the voice's note state, then applies the immediate
        /// (per-sample-loop) results: the initial-attenuation gain and the pan.
        /// The pitch/filter/volume routing deltas are read straight from
        /// <see cref="modulation"/> by <see cref="Mix"/>.
        /// </summary>
        private void UpdateModulation(MidiChannelState channel)
        {
            Array.Clear(modulation, 0, modulation.Length);
            modulators?.Accumulate(channel, velocity, key, modulation);

            // attenuation only attenuates: clamp so a modulator can't push the
            // voice above unity gain. velocityAttenuationCb is the SFZ velocity
            // tracking; for SoundFont it is 0 (velocity drives the modulator instead).
            double attenuation = baseAttenuationCb + velocityAttenuationCb
                + modulation[(int)GeneratorEnum.InitialAttenuation];
            if (attenuation < 0.0) attenuation = 0.0;
            staticGain = (float)SynthMath.AttenuationCentibelsToGain(attenuation);

            SetPan(basePan + modulation[(int)GeneratorEnum.Pan]);
        }

        /// <summary>
        /// Velocity-to-amplitude attenuation in centibels for an SFZ
        /// <c>amp_veltrack</c> percentage: 0 at full velocity, increasing as
        /// velocity drops (the same ~40·log10 law SoundFont's velocity→attenuation
        /// default modulator uses). Returns 0 when tracking is disabled.
        /// </summary>
        private static double VelocityAttenuation(float percent, int velocity)
        {
            if (percent == 0f) return 0.0;
            int v = velocity < 1 ? 1 : velocity;
            return 4.0 * percent * Math.Log10(127.0 / v);
        }

        private static float SendGain(double tenthsOfPercent)
        {
            float g = (float)(tenthsOfPercent / 1000.0);
            return g < 0f ? 0f : g > 1f ? 1f : g;
        }

        private void SetPan(double panValue)
        {
            // SF2 pan: -500 = hard left, +500 = hard right, in 0.1% units
            float pan = Math.Clamp((float)(panValue / 500.0), -1f, 1f);
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
            baseModLfoToPitch = gen[GeneratorEnum.ModulationLFOToPitch];
            baseVibLfoToPitch = gen[GeneratorEnum.VibratoLFOToPitch];
            baseModEnvToPitch = gen[GeneratorEnum.ModulationEnvelopeToPitch];
            baseModLfoToFilter = gen[GeneratorEnum.ModulationLFOToFilterCutoffFrequency];
            baseModEnvToFilter = gen[GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency];
            baseModLfoToVolume = gen[GeneratorEnum.ModulationLFOToVolume];
            baseFilterCents = gen[GeneratorEnum.InitialFilterCutoffFrequency];
            baseReverbSend = gen[GeneratorEnum.ReverbEffectsSend];
            baseChorusSend = gen[GeneratorEnum.ChorusEffectsSend];
        }

        private void ConfigureFilter(SoundFontGenerators gen)
        {
            filterQ = Math.Max(0.5f, (float)SynthMath.ResonanceCentibelsToQ(gen[GeneratorEnum.InitialFilterQ]));

            // effective cutoff includes the modulators evaluated at note-on — most
            // importantly the velocity->filter default modulator, which lowers the
            // cutoff for velocities below maximum
            double effectiveCents = baseFilterCents + modulation[(int)GeneratorEnum.InitialFilterCutoffFrequency];
            double effectiveHz = SynthMath.AbsoluteCentsToHertz(effectiveCents);
            // engage the filter if the (modulated) cutoff is audible, or if an LFO
            // or the mod-envelope can bring it down into range later
            filterActive = effectiveHz < nyquist * 0.95 || baseModLfoToFilter != 0.0 || baseModEnvToFilter != 0.0;

            if (filterActive)
            {
                double hz = Math.Clamp(effectiveHz, 20.0, nyquist * 0.95);
                // fresh filter resets state (safe at note start); a stereo voice
                // filters each channel with its own state
                filter = CreateFilter((float)hz);
                filterRight = stereo ? CreateFilter((float)hz) : null;
            }
            else
            {
                filter = null;
                filterRight = null;
            }
        }

        private BiQuadFilter CreateFilter(float hz) => filterType switch
        {
            SamplerFilterType.HighPass => BiQuadFilter.HighPassFilter(outputSampleRate, hz, filterQ),
            SamplerFilterType.BandPass => BiQuadFilter.BandPassFilterConstantPeakGain(outputSampleRate, hz, filterQ),
            _ => BiQuadFilter.LowPassFilter(outputSampleRate, hz, filterQ)
        };

        private void RetuneFilter(BiQuadFilter f, float hz)
        {
            switch (filterType)
            {
                case SamplerFilterType.HighPass: f.UpdateHighPassFilter(outputSampleRate, hz, filterQ); break;
                case SamplerFilterType.BandPass: f.UpdateBandPassFilter(outputSampleRate, hz, filterQ); break;
                default: f.UpdateLowPassFilter(outputSampleRate, hz, filterQ); break;
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
