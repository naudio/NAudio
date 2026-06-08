using System.Collections.Generic;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// The format-neutral region the voice engine plays: a <see cref="SampleData"/>
    /// reference plus a resolved parameter set (carried as
    /// <see cref="SoundFontGenerators"/> in engine units — the neutral currency,
    /// per the sampler design), the SF2 modulator list, and a velocity-to-amplitude
    /// tracking amount.
    ///
    /// SoundFont and SFZ each project their resolved regions onto this; the voice
    /// reads only this, so the same engine plays both. The generator vector is a
    /// pragmatic neutral representation — SFZ fills the same slots (timecents,
    /// centibels, absolute cents, 0.1% pan) its opcodes map to.
    /// </summary>
    internal sealed class SamplerRegion
    {
        /// <summary>The sample to play.</summary>
        public SampleData Sample { get; init; }

        /// <summary>The resolved synthesis parameters, in SoundFont generator units.</summary>
        public SoundFontGenerators Generators { get; init; }

        /// <summary>
        /// The SF2 modulator list (defaults + file modulators). For formats that
        /// do not use SF2 modulators (e.g. SFZ Tier 1) this is empty.
        /// </summary>
        public ModulatorSet Modulators { get; init; }

        /// <summary>
        /// Velocity-to-amplitude tracking as a percentage (SFZ <c>amp_veltrack</c>).
        /// 0 means velocity does not affect gain here — used by SoundFont, where
        /// velocity drives attenuation through the modulator list instead.
        /// </summary>
        public float VelocityTrackingPercent { get; init; }

        /// <summary>The resonant filter shape (default low-pass).</summary>
        public SamplerFilterType FilterType { get; init; }

        /// <summary>
        /// How a triggered region treats note-off: false (default) follows the amp
        /// envelope's release; true (SFZ <c>loop_mode=one_shot</c>) plays to the end
        /// of the sample regardless of note-off.
        /// </summary>
        public bool IgnoreNoteOff { get; init; }

        /// <summary>
        /// The group whose sounding voices this region silences when it starts
        /// (SFZ <c>off_by</c>); 0 for none. For a self-choking group (SoundFont
        /// <c>exclusiveClass</c>) <see cref="Group"/> and this are equal.
        /// </summary>
        public int OffByGroup { get; init; }

        /// <summary>
        /// When the region triggers (SFZ <c>trigger</c>). Note-on triggers
        /// (<see cref="SamplerTrigger.Attack"/>/<see cref="SamplerTrigger.First"/>/
        /// <see cref="SamplerTrigger.Legato"/>) sound on note-on; <see cref="SamplerTrigger.Release"/>
        /// sounds on note-off.
        /// </summary>
        public SamplerTrigger Trigger { get; init; }

        /// <summary>Lowest MIDI key (inclusive) this region responds to.</summary>
        public byte LoKey { get; init; }
        /// <summary>Highest MIDI key (inclusive) this region responds to.</summary>
        public byte HiKey { get; init; }
        /// <summary>Lowest velocity (inclusive) this region responds to.</summary>
        public byte LoVelocity { get; init; }
        /// <summary>Highest velocity (inclusive) this region responds to.</summary>
        public byte HiVelocity { get; init; }

        /// <summary>
        /// The choke group this region's voices belong to (SoundFont
        /// <c>exclusiveClass</c>, SFZ <c>group</c>); 0 for none. A starting voice
        /// silences sounding voices whose <see cref="Group"/> equals some region's
        /// <see cref="OffByGroup"/>.
        /// </summary>
        public int Group { get; init; }

        // ---- Tier-2 note-on selection (SFZ): keyswitch, round-robin, random, CC gating ----

        /// <summary>The keyswitch key that must have been last pressed for this region to
        /// be active (SFZ <c>sw_last</c>), or −1 for no keyswitch requirement.</summary>
        public int KeyswitchLast { get; init; } = -1;

        /// <summary>The keyswitch considered active before any is pressed (SFZ <c>sw_default</c>), or −1.</summary>
        public int KeyswitchDefault { get; init; } = -1;

        /// <summary>Round-robin length (SFZ <c>seq_length</c>); 1 (or less) = no round-robin.</summary>
        public int SequenceLength { get; init; } = 1;

        /// <summary>This region's 1-based slot in the round-robin (SFZ <c>seq_position</c>).</summary>
        public int SequencePosition { get; init; } = 1;

        /// <summary>Low end of the random window (SFZ <c>lorand</c>), 0..1.</summary>
        public float LowRandom { get; init; }

        /// <summary>High end of the random window (SFZ <c>hirand</c>), 0..1; ≤ <see cref="LowRandom"/> = no random gate.</summary>
        public float HighRandom { get; init; }

        /// <summary>CC value windows that must all be satisfied for the region to sound
        /// (SFZ <c>loccN</c>/<c>hiccN</c>); empty = ungated.</summary>
        public IReadOnlyList<(int Controller, int Low, int High)> CcGates { get; init; }

        /// <summary>Release-trigger decay in dB per second of held time (SFZ <c>rt_decay</c>); 0 = none.</summary>
        public float ReleaseDecayDbPerSecond { get; init; }

        /// <summary>CC windows that <em>trigger</em> the region when the CC rises into them
        /// (SFZ <c>on_loccN</c>/<c>on_hiccN</c>); null = key-triggered as usual.</summary>
        public IReadOnlyList<(int Controller, int Low, int High)> OnCcTriggers { get; init; }

        /// <summary>Whether this region is triggered by a CC rather than by a key.</summary>
        public bool IsCcTriggered => OnCcTriggers != null;

        /// <summary>Peaking-EQ bands applied to the voice (SFZ <c>eqN_*</c>); null = flat.</summary>
        public IReadOnlyList<SamplerEqBand> EqBands { get; init; }

        // ---- Tier-2 key/velocity crossfades (SFZ xfin_*/xfout_*); -1 = no fade on that edge ----

        /// <summary>Key crossfade-in range low/high (SFZ <c>xfin_lokey</c>/<c>xfin_hikey</c>).</summary>
        public int KeyFadeInLow { get; init; } = -1;
        /// <summary>Key crossfade-in range high.</summary>
        public int KeyFadeInHigh { get; init; } = -1;
        /// <summary>Key crossfade-out range low (SFZ <c>xfout_lokey</c>/<c>xfout_hikey</c>).</summary>
        public int KeyFadeOutLow { get; init; } = -1;
        /// <summary>Key crossfade-out range high.</summary>
        public int KeyFadeOutHigh { get; init; } = -1;
        /// <summary>Velocity crossfade-in range low/high (SFZ <c>xfin_lovel</c>/<c>xfin_hivel</c>).</summary>
        public int VelocityFadeInLow { get; init; } = -1;
        /// <summary>Velocity crossfade-in range high.</summary>
        public int VelocityFadeInHigh { get; init; } = -1;
        /// <summary>Velocity crossfade-out range low (SFZ <c>xfout_lovel</c>/<c>xfout_hivel</c>).</summary>
        public int VelocityFadeOutLow { get; init; } = -1;
        /// <summary>Velocity crossfade-out range high.</summary>
        public int VelocityFadeOutHigh { get; init; } = -1;
        /// <summary>The key-crossfade curve (SFZ <c>xf_keycurve</c>).</summary>
        public SamplerCrossfadeCurve KeyFadeCurve { get; init; }
        /// <summary>The velocity-crossfade curve (SFZ <c>xf_velcurve</c>).</summary>
        public SamplerCrossfadeCurve VelocityFadeCurve { get; init; }

        // rotating counter for round-robin; advanced each time the region is an
        // otherwise-eligible candidate
        private int sequenceCounter;

        /// <summary>Whether this region should sound for the given key and velocity.</summary>
        public bool Matches(int key, int velocity) =>
            key >= LoKey && key <= HiKey && velocity >= LoVelocity && velocity <= HiVelocity;

        /// <summary>Whether a per-note random draw falls in this region's random window.</summary>
        public bool PassesRandom(double random) =>
            HighRandom <= LowRandom || (random >= LowRandom && random < HighRandom);

        /// <summary>Whether the channel's current CC values satisfy every CC gate.</summary>
        public bool PassesCcGates(MidiChannelState channel)
        {
            if (CcGates == null) return true;
            foreach (var gate in CcGates)
            {
                int value = channel.Controller(gate.Controller);
                if (value < gate.Low || value > gate.High) return false;
            }
            return true;
        }

        /// <summary>
        /// Whether this region's round-robin slot is up now, advancing the rotating
        /// counter. Call once per note-on for which the region is otherwise eligible.
        /// </summary>
        public bool PassesSequence()
        {
            if (SequenceLength <= 1) return true;
            bool match = sequenceCounter % SequenceLength == SequencePosition - 1;
            sequenceCounter++;
            return match;
        }

        /// <summary>
        /// The key/velocity crossfade gain (0..1) for this note: the key fade and
        /// velocity fade gains multiplied. 1 when no crossfade is defined; 0 means
        /// the layer is silent here (the engine treats that as a gate).
        /// </summary>
        public float CrossfadeGain(int key, int velocity) =>
            FadeGain(key, KeyFadeInLow, KeyFadeInHigh, KeyFadeOutLow, KeyFadeOutHigh, KeyFadeCurve) *
            FadeGain(velocity, VelocityFadeInLow, VelocityFadeInHigh, VelocityFadeOutLow, VelocityFadeOutHigh, VelocityFadeCurve);

        private static float FadeGain(int value, int inLow, int inHigh, int outLow, int outHigh, SamplerCrossfadeCurve curve) =>
            Curve(FadeInPosition(value, inLow, inHigh), curve) * Curve(FadeOutPosition(value, outLow, outHigh), curve);

        // 0 below the range, ramps to 1 across it; 1 when no fade-in is set
        private static float FadeInPosition(int value, int low, int high)
        {
            if (low < 0 || high < 0) return 1f;
            if (value <= low) return 0f;
            if (value >= high) return 1f;
            return (value - low) / (float)(high - low);
        }

        // 1 below the range, ramps to 0 across it; 1 when no fade-out is set
        private static float FadeOutPosition(int value, int low, int high)
        {
            if (low < 0 || high < 0) return 1f;
            if (value <= low) return 1f;
            if (value >= high) return 0f;
            return 1f - (value - low) / (float)(high - low);
        }

        private static float Curve(float position, SamplerCrossfadeCurve curve) =>
            curve == SamplerCrossfadeCurve.Power ? (float)System.Math.Sqrt(position) : position;

        /// <summary>
        /// The release-trigger gain after a key was held <paramref name="heldSeconds"/>
        /// (SFZ <c>rt_decay</c>): 1 with no decay, falling by the dB-per-second rate.
        /// </summary>
        public float ReleaseDecayGain(double heldSeconds)
        {
            if (ReleaseDecayDbPerSecond <= 0f) return 1f;
            return (float)System.Math.Pow(10.0, -ReleaseDecayDbPerSecond * heldSeconds / 20.0);
        }

        /// <summary>
        /// Whether a CC change should trigger this region: a configured
        /// <c>on_loccN</c>/<c>on_hiccN</c> window the controller has just risen into.
        /// </summary>
        public bool TriggeredByCcChange(int controller, int oldValue, int newValue)
        {
            if (OnCcTriggers == null) return false;
            foreach (var t in OnCcTriggers)
            {
                if (t.Controller != controller) continue;
                bool wasInside = oldValue >= t.Low && oldValue <= t.High;
                bool isInside = newValue >= t.Low && newValue <= t.High;
                if (!wasInside && isInside) return true;
            }
            return false;
        }
    }
}
