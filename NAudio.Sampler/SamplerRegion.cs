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
    }
}
