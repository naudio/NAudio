using System;
using NAudio.Dsp;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// Which send buses a voice wrote into during a <see cref="SamplerVoice.Mix"/>
    /// call. The engine ORs these across voices to know whether each bus received
    /// input this block (so an idle bus can skip its effect without scanning the
    /// send buffers).
    /// </summary>
    [Flags]
    internal enum VoiceSendActivity : byte
    {
        /// <summary>No send written.</summary>
        None = 0,
        /// <summary>The reverb send received signal.</summary>
        Reverb = 1,
        /// <summary>The chorus send received signal.</summary>
        Chorus = 2
    }

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
    /// the per-sample loop stays cheap; the modulation sources are advanced to
    /// the block boundary in one <c>Advance(n)</c> call per source (bit-identical
    /// to per-sample stepping). The control phase carries across <see cref="Mix"/>
    /// calls, so segmented (event-dense) and monolithic renders are identical.
    /// The SF2 modulator list is evaluated here too (against the region's
    /// <see cref="ModulatorSet"/>).
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

        // SFZ defines at most three EQ bands (eq1/eq2/eq3) per region, so the
        // per-voice EQ pool is sized for that maximum and reused across notes
        private const int MaxEqBands = 3;

        // Readers, filters and EQ bands are persistent per voice and re-seated /
        // retuned at Start rather than re-allocated, so note-on is allocation-free
        // in steady state. The readers are created lazily on the first Start
        // (they need a SampleSource); the filters are created up front.
        private InterpolatingSampleReader reader;       // mono / left channel
        private InterpolatingSampleReader readerRight;  // right channel (stereo only), advanced in lockstep
        private bool stereo;
        private readonly DahdsrEnvelope ampEnvelope;
        private readonly DahdsrEnvelope modEnvelope;
        private readonly Lfo modLfo;
        private readonly Lfo vibratoLfo;
        private readonly BiQuadFilter filter;        // mono / left channel (used only while filterActive)
        private readonly BiQuadFilter filterRight;   // right channel (stereo only)
        private readonly BiQuadFilter[] eqLeft = new BiQuadFilter[MaxEqBands];
        private readonly BiQuadFilter[] eqRight = new BiQuadFilter[MaxEqBands];
        private int eqBandCount;            // active EQ bands this note (0 = flat)

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
        // the note-fixed (velocity/key/constant) modulator contributions,
        // evaluated once at Start; each refresh copies this baseline and
        // re-accumulates only the channel-dependent modulators on top
        private readonly double[] staticModulation = new double[(int)GeneratorEnum.UnusedEnd + 1];
        // change-gating for the modulator refresh: re-evaluate only when the
        // channel state stamp moved (or a refresh was explicitly requested)
        private int lastChannelVersion;
        private bool modulationRefreshPending;
        private double lastPanValue = double.NaN;     // skip the pan cos/sin while unchanged
        private double lastFilterCents = double.NaN;  // last cutoff the filters were tuned to
        private double lastPitchCents = double.NaN;   // caches the 2^x in the increment
        private double pitchModRatio = 1.0;
        private bool filterModulated; // the cutoff/Q can change after note-on

        // latest modulation-source values (bipolar -1..1 for LFOs, 0..1 for env)
        private float modLfoValue;
        private float vibratoLfoValue;
        private float modEnvValue;

        // Control-rate phase, carried across Mix calls: parameters re-evaluate
        // every ControlBlock *voice* samples regardless of how the engine
        // segmented its reads, so an event-dense (segmented) render is
        // bit-identical to a monolithic one. samplesSinceEval counts rendered
        // samples since the modulation sources last advanced (<= ControlBlock).
        private int controlCountdown;   // samples until the next evaluation (0 = evaluate now)
        private int samplesSinceEval;
        // the modulation-derived parameters from the last evaluation, valid for
        // the rest of the current control block (fields because a block can span
        // Mix calls)
        private double currentIncrement;
        private float currentVolGain;
        private float currentReverbSendGain;
        private float currentChorusSendGain;

        // scratch for the block reader/filter fast path (one control block per channel)
        private readonly float[] blockLeft = new float[ControlBlock];
        private readonly float[] blockRight = new float[ControlBlock];

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

            // placeholder tunings; every Start retunes (and state-resets) before use
            filter = BiQuadFilter.LowPassFilter(outputSampleRate, 1000f, 0.7071f);
            filterRight = BiQuadFilter.LowPassFilter(outputSampleRate, 1000f, 0.7071f);
            for (int i = 0; i < MaxEqBands; i++)
            {
                eqLeft[i] = BiQuadFilter.PeakingEQ(outputSampleRate, 1000f, 1f, 0f);
                eqRight[i] = BiQuadFilter.PeakingEQ(outputSampleRate, 1000f, 1f, 0f);
            }
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

        /// <summary>
        /// The region this voice is playing — read by the engine for the choke
        /// behaviour (SFZ <c>off_mode</c>) and the per-region <c>polyphony</c>
        /// cap (counted by reference identity). Null only before the first Start.
        /// </summary>
        public SamplerRegion Region { get; private set; }

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

            // the region caches its SampleSource pair (the addressing is immutable
            // after projection), so steady-state note-on allocates nothing
            if (!region.TryGetSampleSources(out var sourceLeft, out var sourceRight)) return false;

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

            if (reader == null) reader = new InterpolatingSampleReader(sourceLeft);
            else reader.Reset(sourceLeft);

            // a stereo sample runs a second reader over the right channel in
            // lockstep (same bounds, loop and increment), so the core reader stays
            // mono; for a mono note the right reader (if any) is simply unused
            stereo = sourceRight != null;
            if (stereo)
            {
                if (readerRight == null) readerRight = new InterpolatingSampleReader(sourceRight);
                else readerRight.Reset(sourceRight);
            }

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
            velocityGain = VelocityGain(region.VelocityTrackingPercent, effectiveVelocity, region.VelocityCurve);

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
            lastPanValue = double.NaN;   // a NaN cache never equals, forcing the first computation
            lastPitchCents = double.NaN;

            // the note-fixed modulator contributions (velocity/key/constants) are
            // evaluated once into the baseline; refreshes only re-run the
            // channel-dependent modulators on top of it
            Array.Clear(staticModulation, 0, staticModulation.Length);
            modulators?.AccumulateStatic(channelState, effectiveVelocity, effectiveKey, staticModulation);

            // evaluate the modulators once up front so the initial gain, pan and
            // filter state (and the decision to engage the filter) reflect the
            // note-on velocity and the channel's current controllers
            modulationRefreshPending = true;
            UpdateModulation(channelState);
            ConfigureFilter(gen);
            ConfigureEq(region.EqBands);
            // ConfigureFilter set filterGainComp after the gain above was derived
            // (matching the old per-block recompute, where the first control block
            // corrected it); request a refresh so the first evaluation re-derives
            // the static gain with the fresh compensation
            modulationRefreshPending = true;

            Channel = channel;
            Note = note;
            Group = region.Group;
            Region = region;
            IgnoreNoteOff = region.IgnoreNoteOff;
            StartOrder = order;
            IsHeld = true;
            declicking = false;
            declickL = 0f;
            declickR = 0f;
            controlCountdown = 0;  // the first sample of a fresh note evaluates immediately
            samplesSinceEval = 0;

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
            if (stereo) readerRight.Release();
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
            if (stereo) readerRight.Release();
        }

        /// <summary>
        /// Mixes this voice into an interleaved stereo buffer for a block of
        /// frames, reading the channel's live controllers (pitch-bend and the SF2
        /// modulator sources) at control rate. A portion of the voice's signal,
        /// set by the reverb/chorus send levels, is also added to the
        /// <paramref name="reverbSend"/> and <paramref name="chorusSend"/> buffers
        /// (the same length as <paramref name="buffer"/>). Returns which send
        /// buses the voice wrote into, so the engine can idle-skip unused ones.
        /// </summary>
        public VoiceSendActivity Mix(Span<float> buffer, Span<float> reverbSend, Span<float> chorusSend,
            int frames, MidiChannelState channel)
        {
            var activity = VoiceSendActivity.None;
            if (!IsActive) return activity;

            int pos = 0;
            int remaining = frames;
            while (remaining > 0)
            {
                if (controlCountdown == 0)
                {
                    EvaluateControl(channel);
                    // reaped at the block boundary (inaudible-sustain check)
                    if (!IsActive) return activity;
                }

                int sub = Math.Min(controlCountdown, remaining);
                if (currentReverbSendGain > 0f) activity |= VoiceSendActivity.Reverb;
                if (currentChorusSendGain > 0f) activity |= VoiceSendActivity.Chorus;
                bool alive = RenderSpan(buffer, reverbSend, chorusSend, ref pos, sub);
                controlCountdown -= sub;
                samplesSinceEval += sub;
                remaining -= sub;
                if (!alive) { IsActive = false; return activity; }
            }
            return activity;
        }

        // The DahdsrEnvelope treats -100 dB as silence; a voice whose volume
        // envelope sits in Sustain at or below this can never become audible
        // again on its own (the sustain level is the ceiling until note-off), so
        // it is reaped rather than rendered forever. Deliberately based on the
        // envelope alone — never on staticGain or controller values, so an
        // expression-silenced (CC11 = 0) note survives and swells back up.
        private const float SustainSilenceFloor = 1e-5f;

        /// <summary>
        /// The control-rate evaluation, run every <see cref="ControlBlock"/> voice
        /// samples: advances the modulation sources to "now", re-evaluates the SF2
        /// modulators against the channel's live controllers, and recomputes the
        /// modulation-derived parameters (pitch increment, tremolo gain, send
        /// gains, filter tuning) used until the next evaluation. Also reaps a
        /// voice whose volume envelope has decayed to effective silence in
        /// Sustain (e.g. a looped region with a fully attenuated sustain level),
        /// which would otherwise render inaudibly until note-off.
        /// </summary>
        private void EvaluateControl(MidiChannelState channel)
        {
            if (ampEnvelope.Stage == DahdsrEnvelope.EnvelopeStage.Sustain
                && ampEnvelope.Output < SustainSilenceFloor
                && !declicking && stealFadeGain <= 0f)
            {
                IsActive = false;
                return;
            }

            // one Advance(n) per source replaces n Process() calls; P1 guarantees
            // the resulting value and state are bit-identical to per-sample
            // stepping, so the values read below match the old per-sample path.
            // The sources advance for every rendered sample — including the
            // volume-envelope delay, during which the readers stay untouched —
            // preserving their wall-clock phase.
            if (samplesSinceEval > 0)
            {
                modLfoValue = modLfo.Advance(samplesSinceEval);
                vibratoLfoValue = vibratoLfo.Advance(samplesSinceEval);
                modEnvValue = modEnvelope.Advance(samplesSinceEval);
                samplesSinceEval = 0;
            }

            UpdateModulation(channel);

            // reverb/chorus send levels: 0.1% units (0..1000) -> 0..1 gain
            currentReverbSendGain = SendGain(baseReverbSend + modulation[(int)GeneratorEnum.ReverbEffectsSend]);
            currentChorusSendGain = SendGain(baseChorusSend + modulation[(int)GeneratorEnum.ChorusEffectsSend]);

            double vibToPitch = baseVibLfoToPitch + modulation[(int)GeneratorEnum.VibratoLFOToPitch];
            double modLfoPitch = baseModLfoToPitch + modulation[(int)GeneratorEnum.ModulationLFOToPitch];
            double modEnvPitch = baseModEnvToPitch + modulation[(int)GeneratorEnum.ModulationEnvelopeToPitch];
            double pitchCents = modLfoPitch * modLfoValue
                + vibToPitch * vibratoLfoValue
                + modEnvPitch * modEnvValue;
            // the 2^x is the expensive part; recompute it only when the summed
            // modulation moved (an unmodulated voice keeps pitchCents == 0)
            if (pitchCents != lastPitchCents)
            {
                lastPitchCents = pitchCents;
                pitchModRatio = SynthMath.CentsToRatio(pitchCents);
            }
            currentIncrement = baseIncrement * pitchRatio * channel.PitchBendRatio * pitchModRatio;

            double volCb = (baseModLfoToVolume + modulation[(int)GeneratorEnum.ModulationLFOToVolume]) * modLfoValue;
            currentVolGain = volCb != 0.0 ? (float)SynthMath.CentibelsToGain(volCb) : 1f;

            // retune the filter only when something can still move the cutoff
            // (LFO/mod-env routings or a channel-driven modulator) AND it moved by
            // an audible amount; a half-cent is far below hearing, and skipping
            // the retune leaves the previous (identical-sounding) coefficients
            if (filterActive && filterModulated)
            {
                double modLfoFilter = baseModLfoToFilter + modulation[(int)GeneratorEnum.ModulationLFOToFilterCutoffFrequency];
                double modEnvFilter = baseModEnvToFilter + modulation[(int)GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency];
                double fc = baseFilterCents
                    + modulation[(int)GeneratorEnum.InitialFilterCutoffFrequency]
                    + modEnvFilter * modEnvValue
                    + modLfoFilter * modLfoValue;
                if (Math.Abs(fc - lastFilterCents) >= 0.5)
                {
                    lastFilterCents = fc;
                    double hz = Math.Clamp(SynthMath.AbsoluteCentsToHertz(fc), 20.0, nyquist * 0.95);
                    RetuneFilter(filter, (float)hz);
                    if (stereo) RetuneFilter(filterRight, (float)hz);
                }
            }

            controlCountdown = ControlBlock;
        }

        /// <summary>
        /// Renders <paramref name="count"/> voice samples (all within one control
        /// block, so the evaluated parameters are constant) into the mix and send
        /// buffers. Returns false when the voice's note fully ended within the
        /// span. The audio portion runs through the block reader/filter fast
        /// paths, which are bit-identical to the per-sample equivalents.
        /// </summary>
        private bool RenderSpan(Span<float> buffer, Span<float> reverbSend, Span<float> chorusSend,
            ref int pos, int count)
        {
            double increment = currentIncrement;
            float volGain = currentVolGain;
            float reverbSendGain = currentReverbSendGain;
            float chorusSendGain = currentChorusSendGain;

            int i = 0;
            while (i < count)
            {
                if (declicking)
                {
                    // fading the last output to zero after a one-shot reached End
                    float left, right;
                    bool finished = false;
                    if (declickGain > 0f)
                    {
                        left = declickL * declickGain;
                        right = declickR * declickGain;
                        declickGain -= declickStep;
                    }
                    else { left = 0f; right = 0f; }
                    if (declickGain <= 0f) finished = true;
                    if (Emit(buffer, reverbSend, chorusSend, ref pos, left, right,
                        reverbSendGain, chorusSendGain, finished)) return false;
                    i++;
                    continue;
                }

                float env = ampEnvelope.Process();
                if (ampEnvelope.Stage == DahdsrEnvelope.EnvelopeStage.Delay)
                {
                    // SF2.04 §8.1.2 gen 33: the volume-envelope delay postpones the
                    // sample, it must not consume it — output silence and leave the
                    // readers untouched so the attack transient still starts from
                    // the waveform's first sample (FluidSynth behaves the same
                    // way). The modulation sources still keep time: every rendered
                    // sample, including these, counts toward samplesSinceEval.
                    if (Emit(buffer, reverbSend, chorusSend, ref pos, 0f, 0f,
                        reverbSendGain, chorusSendGain, false)) return false;
                    i++;
                    continue;
                }

                // Audio path. The envelope has left Delay and the gate cannot
                // change inside a Mix call, so the rest of the span reads through
                // the block fast paths: the reader's block Read and the biquad's
                // block Transform are bit-identical to their per-sample forms, and
                // the envelope/gain arithmetic below is unchanged.
                int n = count - i;
                int got = reader.Read(blockLeft.AsSpan(0, n), increment);
                if (got == 0)
                {
                    // unreachable in practice (Ended is discovered on the call that
                    // returns the final sample, which starts the de-click); treat a
                    // pre-ended reader as an immediate silent stop
                    declicking = true;
                    declickGain = 0f;
                    continue;
                }
                if (stereo)
                {
                    // lockstep: same bounds, loop and increment => same count; the
                    // defensive clear keeps any (impossible) shortfall identical to
                    // the per-sample path, where an ended reader reads as 0
                    int gotR = readerRight.Read(blockRight.AsSpan(0, got), increment);
                    if (gotR < got) blockRight.AsSpan(gotR, got - gotR).Clear();
                }

                if (filterActive)
                {
                    filter.Transform(blockLeft.AsSpan(0, got), blockLeft.AsSpan(0, got));
                    if (stereo) filterRight.Transform(blockRight.AsSpan(0, got), blockRight.AsSpan(0, got));
                }
                for (int b = 0; b < eqBandCount; b++)
                {
                    eqLeft[b].Transform(blockLeft.AsSpan(0, got), blockLeft.AsSpan(0, got));
                    if (stereo) eqRight[b].Transform(blockRight.AsSpan(0, got), blockRight.AsSpan(0, got));
                }

                // a block read can only have ended at its final sample (an earlier
                // end would have cut the block short)
                bool endedInBlock = reader.Ended;

                for (int j = 0; j < got; j++)
                {
                    float e = j == 0 ? env : ampEnvelope.Process();
                    float sL = blockLeft[j];
                    float sR = stereo ? blockRight[j] : sL;
                    float envGain = e * staticGain * volGain;
                    float left = sL * envGain * leftGain;
                    float right = sR * envGain * rightGain;
                    declickL = left;   // remember the last output for a future de-click
                    declickR = right;

                    bool finished = false;
                    if (endedInBlock && j == got - 1)
                    {
                        // One-shot reached End: the value just read is the last
                        // real sample and is emitted this iteration (discarding it
                        // would drop the final sample of every one-shot). If the
                        // note ends on a non-trivial value (e.g. an edited End
                        // mid-waveform), ramp the last output down over a few ms
                        // instead of cutting hard (which clicks); a sample that
                        // already ends near zero (most well-formed content) just
                        // stops, with no added tail.
                        declicking = true;
                        bool nearZero = Math.Abs(left) <= DeclickThreshold && Math.Abs(right) <= DeclickThreshold;
                        declickGain = nearZero ? 0f : 1f - declickStep;
                        if (nearZero) finished = true;
                    }
                    else if (ampEnvelope.IsFinished) finished = true;

                    if (Emit(buffer, reverbSend, chorusSend, ref pos, left, right,
                        reverbSendGain, chorusSendGain, finished)) return false;
                }
                i += got;
            }
            return true;
        }

        /// <summary>
        /// Adds one output sample (plus any steal-fade remnant) into the mix and
        /// send buffers. Returns true when the voice has fully finished at this
        /// sample (a live steal fade keeps it alive).
        /// </summary>
        private bool Emit(Span<float> buffer, Span<float> reverbSend, Span<float> chorusSend,
            ref int pos, float left, float right, float reverbSendGain, float chorusSendGain, bool finished)
        {
            // a stolen voice's previous output rides on top of the new note while
            // it fades out (see Start)
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
            return finished;
        }

        /// <summary>
        /// Re-evaluates the SF2 modulator list against the channel's live
        /// controllers and the voice's note state, then applies the immediate
        /// (per-sample-loop) results: the initial-attenuation gain and the pan.
        /// The pitch/filter/volume routing deltas are read straight from
        /// <see cref="modulation"/> by <see cref="EvaluateControl"/>. Skipped
        /// entirely while the channel state is unchanged (its outputs are pure
        /// functions of the channel state and the note-fixed baseline).
        /// </summary>
        private void UpdateModulation(MidiChannelState channel)
        {
            int channelVersion = channel.Version;
            if (!modulationRefreshPending)
            {
                // with no channel-dependent modulators the set is note-fixed and
                // can never change after the Start-time evaluation
                if (modulators == null || !modulators.HasDynamicModulators) return;
                if (channelVersion == lastChannelVersion) return;
            }
            modulationRefreshPending = false;
            lastChannelVersion = channelVersion;

            Array.Copy(staticModulation, modulation, modulation.Length);
            modulators?.AccumulateDynamic(channel, velocity, key, modulation);

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
        /// and the full velocity curve:
        /// <c>gain = 1 − p·(1 − curve(v))</c> with <c>p = percent/100</c> in
        /// [−1, 1], where <c>curve(v)</c> is the region's resolved
        /// <c>amp_velcurve_N</c> curve when defined and the default <c>(v/127)²</c>
        /// otherwise. At p=1 this is exactly the curve; a partial p tracks
        /// proportionally in gain (p=0.5, v=1 ≈ −6 dB, not −42 dB); a negative p
        /// boosts low velocities above unity (quieter the harder you play) —
        /// which is why this is a multiplicative factor rather than part of the
        /// clamped attenuation sum. Returns 1 when tracking is disabled
        /// (<c>amp_veltrack=0</c> means velocity has no effect, curve or not —
        /// also the SoundFont path, where velocity rides the modulator list
        /// instead).
        /// </summary>
        private static float VelocityGain(float percent, int velocity, float[] curve)
        {
            if (percent == 0f) return 1f;
            double p = Math.Clamp(percent / 100.0, -1.0, 1.0);
            int v = Math.Clamp(velocity, 0, 127);
            double c = curve != null ? curve[v] : (v / 127.0) * (v / 127.0);
            double gain = 1.0 - p * (1.0 - c);
            return gain < 0.0 ? 0f : (float)gain;
        }

        private static float SendGain(double tenthsOfPercent)
        {
            float g = (float)(tenthsOfPercent / 1000.0);
            return g < 0f ? 0f : g > 1f ? 1f : g;
        }

        private void SetPan(double panValue)
        {
            // the cos/sin pair only needs recomputing when the pan moved (the
            // NaN sentinel set at Start never compares equal)
            if (panValue == lastPanValue) return;
            lastPanValue = panValue;

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

        // SF2.04 §8.1.3: initialFilterCutoffFrequency's default of 13500 cents
        // means "filter open"; a base cutoff at or beyond this (less rounding
        // headroom) passes the audible band untouched, so the filter is bypassed
        // unless something can still pull the cutoff down
        private const double OpenFilterCents = 13490.0;

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

            // whether anything can move the cutoff after note-on: LFO/mod-env
            // routings from the generators, or a channel-driven (CC/bend/pressure)
            // modulator targeting the filter
            filterModulated = baseModLfoToFilter != 0.0 || baseModEnvToFilter != 0.0
                || (modulators != null && modulators.HasDynamicFilterRouting);

            // Engage the filter if the (note-on modulated) cutoff is audibly below
            // both the Nyquist guard and the SF2 "open" value, or if modulation
            // can bring it down into range later. The dynamic-routing term matters
            // even with an open base cutoff: a file modulator like CC74->cutoff on
            // an open region must engage when the controller drops.
            filterActive = (effectiveHz < nyquist * 0.95 && effectiveCents < OpenFilterCents)
                || filterModulated;

            if (filterActive)
            {
                // SF2.04 §8.1.2 gen 8: the filter gain at DC is reduced by half
                // the resonance dB (100 cB resonance -> DC 5 dB below unity), so
                // raising the resonance doesn't raise the perceived level. Folded
                // into the static gain (UpdateModulation re-reads it each block).
                filterGainComp = (float)Math.Pow(10.0, -(resonanceCb / 10.0) / 2.0 / 20.0);
                double hz = Math.Clamp(effectiveHz, 20.0, nyquist * 0.95);
                lastFilterCents = effectiveCents; // the control-rate retune skips while unmoved
                // retune the persistent filters and clear their sample history so
                // the note starts from a fresh filter state (no inherited ringing
                // or latched NaN from a previous note); a stereo voice filters
                // each channel with its own state
                RetuneFilter(filter, (float)hz);
                filter.ResetState();
                if (stereo)
                {
                    RetuneFilter(filterRight, (float)hz);
                    filterRight.ResetState();
                }
            }
            else
            {
                filterGainComp = 1f;
            }
        }

        private void ConfigureEq(System.Collections.Generic.IReadOnlyList<SamplerEqBand> bands)
        {
            if (bands == null || bands.Count == 0) { eqBandCount = 0; return; }

            // SetPeakingEq resets the band's sample history, so each note's EQ
            // starts clean even though the filter objects persist across notes
            eqBandCount = Math.Min(bands.Count, MaxEqBands);
            for (int i = 0; i < eqBandCount; i++)
            {
                var band = bands[i];
                float hz = (float)Math.Clamp(band.FrequencyHz, 20.0, nyquist * 0.95);
                eqLeft[i].SetPeakingEq(outputSampleRate, hz, band.Q, band.GainDb);
                if (stereo) eqRight[i].SetPeakingEq(outputSampleRate, hz, band.Q, band.GainDb);
            }
        }

        // state-preserving coefficient retune for the active filter shape; the
        // coefficient arithmetic matches the Set*/factory methods exactly, so a
        // retune+ResetState at Start is equivalent to constructing a fresh filter
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
    }
}
