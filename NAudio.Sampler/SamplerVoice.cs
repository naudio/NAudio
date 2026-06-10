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

        // SF2.04 §8.1.2 (gens 21/23): when its delay expires an LFO "begins its
        // upward ramp from zero" — for the triangle waveform (which is +1 at
        // phase 0) the zero-and-rising point is phase 0.75
        private const float TriangleZeroRisingPhase = 0.75f;

        // sane LFO frequency bounds: the SF2 frequency generators top out around
        // 110 Hz (4500 absolute cents); clamping keeps a malformed generator from
        // producing a per-sample phase increment >= 1, which would push the
        // triangle outside [-1, 1] and run away
        private const double MinLfoHz = 0.001;
        private const double MaxLfoHz = 100.0;

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
        private BiQuadFilter[] eqLeft;      // per-region peaking-EQ bands (null = flat)
        private BiQuadFilter[] eqRight;     // EQ bands for the right channel (stereo only)

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
        private float velocityGain = 1f;    // velocity->amplitude tracking gain (SFZ amp_veltrack)
        private float regionGain = 1f;      // static per-note gain (SFZ key/velocity crossfade, region volume boost)
        private double basePan;             // pan generator (0.1% units, ±500)
        private double baseReverbSend;      // reverb send (0.1% units, 0..1000)
        private double baseChorusSend;      // chorus send (0.1% units, 0..1000)
        private float filterQ;
        private bool filterActive;
        private float filterGainComp = 1f;  // resonance gain compensation (SF2 §8.1.2 gen 8)

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

        // when a one-shot sample reaches its end mid-signal, ramp the last output
        // down to zero over a few ms rather than hard-cutting (which clicks if the
        // end falls on a non-zero sample, e.g. an edited End marker)
        private const float DeclickThreshold = 0.001f; // ~-60 dBFS: below this a hard stop is inaudible
        private bool declicking;
        private float declickGain;
        private float declickStep;
        private float declickL;
        private float declickR;

        // when a still-sounding voice is stolen (Start on an active voice), its
        // last output is captured here and faded out over the de-click time,
        // summed on top of the new note — re-seating instantly would step the
        // output from the victim's current value to the new note's envelope-zero
        // start, an audible pop under polyphony pressure
        private float stealFadeL;
        private float stealFadeR;
        private float stealFadeGain;

        /// <summary>Creates a voice for the given output rate.</summary>
        public SamplerVoice(int outputSampleRate)
        {
            this.outputSampleRate = outputSampleRate;
            nyquist = outputSampleRate / 2.0;
            // the volume envelope keeps the default exponential (constant-dB)
            // decay/release; the modulation envelope ramps linearly in value
            // (SF2.04 §8.1.2 gens 28/30 vs 36/38)
            ampEnvelope = new DahdsrEnvelope(outputSampleRate);
            modEnvelope = new DahdsrEnvelope(outputSampleRate)
            {
                DecayReleaseShape = DahdsrEnvelope.RampShape.Linear
            };
            modLfo = new Lfo(outputSampleRate) { Waveform = LfoWaveform.Triangle, StartPhase = TriangleZeroRisingPhase };
            vibratoLfo = new Lfo(outputSampleRate) { Waveform = LfoWaveform.Triangle, StartPhase = TriangleZeroRisingPhase };
            declickStep = 1f / Math.Max(1, (int)(outputSampleRate * 0.005)); // ~5 ms de-click
        }

        /// <summary>Whether this voice is currently producing sound.</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// The reader's current source-sample read position (for UI playback
        /// indicators). The audio thread updates it every sample; a cross-thread
        /// read may be momentarily stale, which is harmless for display.
        /// </summary>
        internal double SamplePosition => reader == null ? 0.0 : reader.Position;

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

        /// <summary>
        /// The voice's current audible level — the envelope output scaled by the
        /// static gain (attenuation, region/crossfade gain, velocity tracking) —
        /// used to pick the quietest voice to steal. Ranking by envelope output
        /// alone would let a heavily attenuated voice survive a steal while an
        /// audible one is cut.
        /// </summary>
        public float Level => ampEnvelope.Output * staticGain;

        /// <summary>
        /// Starts this voice playing a region for a given key and velocity.
        /// Returns false if the region's sample addressing is unusable.
        /// </summary>
        public bool Start(SamplerRegion region, MidiChannelState channelState,
            int channel, int note, int velocity, long order, float regionGain = 1f)
        {
            // the engine's per-note gain (crossfades, rt_decay) times the region's
            // static gain (e.g. an SFZ volume boost, which can exceed unity)
            this.regionGain = regionGain * region.GainLinear;
            var gen = region.Generators;
            var sample = region.Sample;

            int start = sample.Start + gen.StartAddressOffset;
            int end = sample.End + gen.EndAddressOffset;
            int loopStart = sample.LoopStart + gen.StartLoopAddressOffset;
            int loopEnd = sample.LoopEnd + gen.EndLoopAddressOffset;

            if (start < 0 || end > sample.Data.Length || start >= end) return false;

            // Stealing a voice that is still sounding: capture its current output
            // and ramp it to zero over the de-click time (~5 ms), summed on top of
            // the new note in Mix. The new note starts at envelope zero, so the
            // sum stays bounded. An in-progress steal fade is folded in so a
            // rapid double-steal cannot pop either.
            if (IsActive)
            {
                float current = declicking ? Math.Max(0f, declickGain) : 1f;
                float prior = Math.Max(0f, stealFadeGain);
                stealFadeL = declickL * current + stealFadeL * prior;
                stealFadeR = declickR * current + stealFadeR * prior;
                stealFadeGain = 1f - declickStep;
            }

            var loopMode = MapLoopMode(gen.SampleModes);
            if (loopMode != LoopMode.None &&
                (loopStart < start || loopEnd > end || loopStart >= loopEnd))
            {
                loopMode = LoopMode.None; // malformed loop points — play as one-shot
            }

            int? loopStartOrNull = loopMode == LoopMode.None ? null : loopStart;
            int? loopEndOrNull = loopMode == LoopMode.None ? null : loopEnd;
            int crossfade = loopMode == LoopMode.None ? 0 : sample.CrossfadeSamples;
            reader = new InterpolatingSampleReader(
                new SampleSource(sample.Data, sample.SampleRate, loopMode, start, end, loopStartOrNull, loopEndOrNull, crossfade));

            // a stereo sample runs a second reader over the right channel in
            // lockstep (same bounds, loop and increment), so the core reader stays mono
            stereo = sample.IsStereo && sample.DataRight.Length >= end;
            readerRight = stereo
                ? new InterpolatingSampleReader(
                    new SampleSource(sample.DataRight, sample.SampleRate, loopMode, start, end, loopStartOrNull, loopEndOrNull, crossfade))
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
            velocityGain = VelocityGain(region.VelocityTrackingPercent, effectiveVelocity);

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
            ConfigureEq(region.EqBands);

            Channel = channel;
            Note = note;
            Group = region.Group;
            IgnoreNoteOff = region.IgnoreNoteOff;
            StartOrder = order;
            IsHeld = true;
            declicking = false;
            declickL = 0f;
            declickR = 0f;

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
                    float left, right;
                    bool finished = false; // this voice's own note has fully ended

                    if (declicking)
                    {
                        // fading the last output to zero after a one-shot reached End
                        if (declickGain > 0f)
                        {
                            left = declickL * declickGain;
                            right = declickR * declickGain;
                            declickGain -= declickStep;
                        }
                        else { left = 0f; right = 0f; }
                        if (declickGain <= 0f) finished = true;
                    }
                    else
                    {
                        float env = ampEnvelope.Process();
                        if (ampEnvelope.Stage == DahdsrEnvelope.EnvelopeStage.Delay)
                        {
                            // SF2.04 §8.1.2 gen 33: the volume-envelope delay
                            // postpones the sample, it must not consume it — output
                            // silence and leave the readers untouched so the attack
                            // transient still starts from the waveform's first
                            // sample (FluidSynth behaves the same way). The
                            // modulation sources still advance to keep time.
                            left = 0f;
                            right = 0f;
                            modLfoValue = modLfo.Process();
                            vibratoLfoValue = vibratoLfo.Process();
                            modEnvValue = modEnvelope.Process();
                        }
                        else
                        {
                            float sL = reader.Read(increment);
                            if (filterActive) sL = filter.Transform(sL);
                            if (eqLeft != null)
                                for (int b = 0; b < eqLeft.Length; b++) sL = eqLeft[b].Transform(sL);

                            float sR;
                            if (stereo)
                            {
                                // lockstep: same bounds and increment, so the right
                                // reader ends on the same sample as the left
                                sR = readerRight.Read(increment);
                                if (filterActive) sR = filterRight.Transform(sR);
                                if (eqRight != null)
                                    for (int b = 0; b < eqRight.Length; b++) sR = eqRight[b].Transform(sR);
                            }
                            else sR = sL;

                            float envGain = env * staticGain * volGain;
                            left = sL * envGain * leftGain;
                            right = sR * envGain * rightGain;
                            declickL = left;   // remember the last output for a future de-click
                            declickR = right;

                            // advance the modulation sources every sample (keeps phase accurate)
                            modLfoValue = modLfo.Process();
                            vibratoLfoValue = vibratoLfo.Process();
                            modEnvValue = modEnvelope.Process();

                            if (reader.Ended)
                            {
                                // One-shot reached End: the value just read is the
                                // last real sample and is emitted this iteration
                                // (discarding it would drop the final sample of
                                // every one-shot). If the note ends on a
                                // non-trivial value (e.g. an edited End
                                // mid-waveform), ramp the last output down over a
                                // few ms instead of cutting hard (which clicks); a
                                // sample that already ends near zero (most
                                // well-formed content) just stops, with no added tail.
                                declicking = true;
                                bool nearZero = Math.Abs(left) <= DeclickThreshold && Math.Abs(right) <= DeclickThreshold;
                                declickGain = nearZero ? 0f : 1f - declickStep;
                                if (nearZero) finished = true;
                            }
                            else if (ampEnvelope.IsFinished) finished = true;
                        }
                    }

                    // a stolen voice's previous output rides on top of the new
                    // note while it fades out (see Start)
                    if (stealFadeGain > 0f)
                    {
                        left += stealFadeL * stealFadeGain;
                        right += stealFadeR * stealFadeGain;
                        stealFadeGain -= declickStep;
                        if (stealFadeGain > 0f) finished = false; // the fade keeps the voice alive
                    }

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

                    if (finished) { IsActive = false; return; }
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
            // voice above unity gain (SF2 spec). Gains that may legitimately
            // exceed unity — the SFZ velocity tracking (negative amp_veltrack
            // boosts low velocities) and the region/crossfade gain (volume
            // boost) — are multiplied in outside the clamp.
            double attenuation = baseAttenuationCb
                + modulation[(int)GeneratorEnum.InitialAttenuation];
            if (attenuation < 0.0) attenuation = 0.0;
            staticGain = (float)SynthMath.AttenuationCentibelsToGain(attenuation)
                * regionGain * velocityGain * filterGainComp;

            SetPan(basePan + modulation[(int)GeneratorEnum.Pan]);
        }

        /// <summary>
        /// The velocity-to-amplitude gain factor for an SFZ <c>amp_veltrack</c>
        /// percentage. SFZ interpolates in the GAIN domain between no tracking
        /// and the full velocity-squared curve:
        /// <c>gain = 1 − p·(1 − (v/127)²)</c> with <c>p = percent/100</c> in
        /// [−1, 1]. At p=1 this is exactly the (v/127)² law; a partial p tracks
        /// proportionally in gain (p=0.5, v=1 ≈ −6 dB, not −42 dB); a negative p
        /// boosts low velocities above unity (quieter the harder you play) —
        /// which is why this is a multiplicative factor rather than part of the
        /// clamped attenuation sum. Returns 1 when tracking is disabled (the
        /// SoundFont path: velocity rides the modulator list instead).
        /// </summary>
        private static float VelocityGain(float percent, int velocity)
        {
            if (percent == 0f) return 1f;
            double p = Math.Clamp(percent / 100.0, -1.0, 1.0);
            double x = Math.Clamp(velocity, 0, 127) / 127.0;
            double gain = 1.0 - p * (1.0 - x * x);
            return gain < 0.0 ? 0f : (float)gain;
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
            ampEnvelope.HoldSeconds = (float)(SynthMath.TimecentsToSeconds(gen[GeneratorEnum.HoldVolumeEnvelope])
                * KeyNumberTimeFactor(gen[GeneratorEnum.KeyNumberToVolumeEnvelopeHold]));
            ampEnvelope.DecaySeconds = (float)(SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DecayVolumeEnvelope])
                * KeyNumberTimeFactor(gen[GeneratorEnum.KeyNumberToVolumeEnvelopeDecay]));
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
            modEnvelope.HoldSeconds = (float)(SynthMath.TimecentsToSeconds(gen[GeneratorEnum.HoldModulationEnvelope])
                * KeyNumberTimeFactor(gen[GeneratorEnum.KeyNumberToModulationEnvelopeHold]));
            modEnvelope.DecaySeconds = (float)(SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DecayModulationEnvelope])
                * KeyNumberTimeFactor(gen[GeneratorEnum.KeyNumberToModulationEnvelopeDecay]));
            modEnvelope.ReleaseSeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.ReleaseModulationEnvelope]);
            // sustainModEnv is in 0.1% units of full scale, decreasing from full
            double permille = gen[GeneratorEnum.SustainModulationEnvelope];
            modEnvelope.SustainLevel = (float)Math.Clamp(1.0 - permille / 1000.0, 0.0, 1.0);
        }

        /// <summary>
        /// The hold/decay time scale factor for a keynumTo*Env Hold/Decay
        /// generator (SF2.04 §8.1.2 gens 31/32/39/40):
        /// 2^(amount × (60 − keynum) / 1200). The amount is in timecents per key
        /// number with key 60 neutral, so a positive amount lengthens hold/decay
        /// for lower keys (the piano "bass notes ring longer" behaviour). Uses
        /// the effective key (the keynum generator override if set, else the
        /// played note), which <see cref="Start"/> stores before configuring the
        /// envelopes.
        /// </summary>
        private double KeyNumberTimeFactor(double timecentsPerKey) =>
            timecentsPerKey == 0.0 ? 1.0 : SynthMath.CentsToRatio(timecentsPerKey * (60 - key));

        private void ConfigureLfos(SoundFontGenerators gen)
        {
            // frequencies are clamped to a sane LFO range so a malformed
            // freqModLfo/freqVibLfo generator can't run the oscillator away
            modLfo.FrequencyHz = (float)Math.Clamp(
                SynthMath.AbsoluteCentsToHertz(gen[GeneratorEnum.FrequencyModulationLFO]), MinLfoHz, MaxLfoHz);
            modLfo.DelaySeconds = (float)SynthMath.TimecentsToSeconds(gen[GeneratorEnum.DelayModulationLFO]);
            modLfo.Reset();

            vibratoLfo.FrequencyHz = (float)Math.Clamp(
                SynthMath.AbsoluteCentsToHertz(gen[GeneratorEnum.FrequencyVibratoLFO]), MinLfoHz, MaxLfoHz);
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
            // initialFilterQ is clamped to the spec range 0..960 cB (a malformed
            // value would otherwise reach float infinity, making alpha = 0 and a
            // filter that rings forever); 0 cB maps to the flat Butterworth
            // response (Q ~0.707) — see SynthMath.ResonanceCentibelsToQ
            double resonanceCb = Math.Clamp((double)gen[GeneratorEnum.InitialFilterQ], 0.0, 960.0);
            filterQ = (float)SynthMath.ResonanceCentibelsToQ(resonanceCb);

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
                // SF2.04 §8.1.2 gen 8: the filter gain at DC is reduced by half
                // the resonance dB (100 cB resonance -> DC 5 dB below unity), so
                // raising the resonance doesn't raise the perceived level. Folded
                // into the static gain (UpdateModulation re-reads it each block).
                filterGainComp = (float)Math.Pow(10.0, -(resonanceCb / 10.0) / 2.0 / 20.0);
                double hz = Math.Clamp(effectiveHz, 20.0, nyquist * 0.95);
                // fresh filter resets state (safe at note start); a stereo voice
                // filters each channel with its own state
                filter = CreateFilter((float)hz);
                filterRight = stereo ? CreateFilter((float)hz) : null;
            }
            else
            {
                filterGainComp = 1f;
                filter = null;
                filterRight = null;
            }
        }

        private void ConfigureEq(System.Collections.Generic.IReadOnlyList<SamplerEqBand> bands)
        {
            if (bands == null || bands.Count == 0) { eqLeft = null; eqRight = null; return; }

            eqLeft = new BiQuadFilter[bands.Count];
            eqRight = stereo ? new BiQuadFilter[bands.Count] : null;
            for (int i = 0; i < bands.Count; i++)
            {
                var band = bands[i];
                float hz = (float)Math.Clamp(band.FrequencyHz, 20.0, nyquist * 0.95);
                eqLeft[i] = BiQuadFilter.PeakingEQ(outputSampleRate, hz, band.Q, band.GainDb);
                if (stereo) eqRight[i] = BiQuadFilter.PeakingEQ(outputSampleRate, hz, band.Q, band.GainDb);
            }
        }

        private BiQuadFilter CreateFilter(float hz) => filterType switch
        {
            SamplerFilterType.HighPass => BiQuadFilter.HighPassFilter(outputSampleRate, hz, filterQ),
            SamplerFilterType.BandPass => BiQuadFilter.BandPassFilterConstantPeakGain(outputSampleRate, hz, filterQ),
            SamplerFilterType.BandReject => BiQuadFilter.NotchFilter(outputSampleRate, hz, filterQ),
            _ => BiQuadFilter.LowPassFilter(outputSampleRate, hz, filterQ)
        };

        private void RetuneFilter(BiQuadFilter f, float hz)
        {
            switch (filterType)
            {
                case SamplerFilterType.HighPass: f.UpdateHighPassFilter(outputSampleRate, hz, filterQ); break;
                case SamplerFilterType.BandPass: f.UpdateBandPassFilter(outputSampleRate, hz, filterQ); break;
                case SamplerFilterType.BandReject: f.UpdateNotchFilter(outputSampleRate, hz, filterQ); break;
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
