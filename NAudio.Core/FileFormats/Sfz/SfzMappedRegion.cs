using System.Collections.Generic;
using System.Globalization;

namespace NAudio.Sfz
{
    /// <summary>SFZ <c>loop_mode</c> values.</summary>
    public enum SfzLoopMode
    {
        /// <summary>Play once, no loop (the default unless loop points are set).</summary>
        NoLoop,
        /// <summary>Play the whole sample to the end, ignoring note-off.</summary>
        OneShot,
        /// <summary>Loop continuously between the loop points.</summary>
        LoopContinuous,
        /// <summary>Loop while the note is held, then play out to the sample end.</summary>
        LoopSustain
    }

    /// <summary>SFZ <c>trigger</c> values.</summary>
    public enum SfzTrigger
    {
        /// <summary>Play on note-on (the default).</summary>
        Attack,
        /// <summary>Play on note-off.</summary>
        Release,
        /// <summary>Play on note-on only if no other notes are sounding on the channel.</summary>
        First,
        /// <summary>Play on note-on only if other notes are already sounding (legato).</summary>
        Legato
    }

    /// <summary>SFZ <c>off_mode</c> values — how a region is silenced by its group.</summary>
    public enum SfzOffMode
    {
        /// <summary>Cut quickly with a short fade (the default).</summary>
        Fast,
        /// <summary>Release normally using the amp envelope's release.</summary>
        Normal
    }

    /// <summary>The curve a crossfade uses (SFZ <c>xf_keycurve</c>/<c>xf_velcurve</c>).</summary>
    public enum SfzCrossfadeCurve
    {
        /// <summary>Linear (SFZ <c>gain</c>).</summary>
        Linear,
        /// <summary>Equal-power (SFZ <c>power</c>, the default).</summary>
        Power
    }

    /// <summary>The filter family selected by SFZ <c>fil_type</c>.</summary>
    public enum SfzFilterType
    {
        /// <summary>Low-pass (the default).</summary>
        LowPass,
        /// <summary>High-pass.</summary>
        HighPass,
        /// <summary>Band-pass.</summary>
        BandPass,
        /// <summary>Band-reject (notch).</summary>
        BandReject
    }

    /// <summary>
    /// One per-region SFZ EQ band (<c>eqN_freq</c>/<c>eqN_bw</c>/<c>eqN_gain</c>):
    /// a centre frequency in Hz, a bandwidth in octaves and a gain in dB.
    /// </summary>
    public readonly struct SfzEqBand
    {
        /// <summary>Creates one EQ band.</summary>
        public SfzEqBand(float frequencyHz, float bandwidthOctaves, float gainDb)
        {
            FrequencyHz = frequencyHz;
            BandwidthOctaves = bandwidthOctaves;
            GainDb = gainDb;
        }

        /// <summary>Centre frequency in Hz (<c>eqN_freq</c>; the spec defaults are 50/500/5000 Hz for bands 1/2/3).</summary>
        public float FrequencyHz { get; }
        /// <summary>Bandwidth in octaves (<c>eqN_bw</c>, default 1).</summary>
        public float BandwidthOctaves { get; }
        /// <summary>Gain in dB (<c>eqN_gain</c>, default 0 — a flat, no-op band).</summary>
        public float GainDb { get; }
    }

    /// <summary>
    /// An SFZ region with its Tier-1 and Tier-2 opcodes interpreted into typed,
    /// engine-ready synthesis parameters: absolute key/velocity ranges (note
    /// names resolved, offsets applied), tuning in cents, volume in dB, pan
    /// normalised to ±1, the amplitude envelope in seconds, the filter, loop
    /// behaviour, sample offsets, group/trigger routing, note-on selection
    /// (keyswitches, round-robin, random and CC windows), crossfades, the
    /// per-region LFOs/EGs and EQ bands, effect sends and release decay.
    ///
    /// This is the SFZ semantic layer, the counterpart to the SoundFont
    /// generator model. It does not load samples or touch the voice engine —
    /// projecting both formats onto one neutral model the engine plays is the
    /// next step.
    /// </summary>
    public sealed class SfzMappedRegion
    {
        private SfzMappedRegion(SfzRegion region)
        {
            Region = region;
        }

        /// <summary>The underlying parsed region (raw opcodes, resolved sample path).</summary>
        public SfzRegion Region { get; private set; }

        /// <summary>The region's sample path (from <see cref="SfzRegion.Sample"/>).</summary>
        public string Sample => Region.Sample;

        /// <summary>Lowest MIDI key (inclusive) the region responds to (default 0).</summary>
        public int LoKey { get; private set; }
        /// <summary>Highest MIDI key (inclusive) the region responds to (default 127).</summary>
        public int HiKey { get; private set; }
        /// <summary>Lowest velocity (inclusive) the region responds to (default 0).</summary>
        public int LoVel { get; private set; }
        /// <summary>Highest velocity (inclusive) the region responds to (default 127).</summary>
        public int HiVel { get; private set; }
        /// <summary>The key that plays the sample at its recorded pitch (default 60).</summary>
        public int PitchKeycenter { get; private set; }
        /// <summary>Cents per key of pitch tracking across the keyboard (default 100).</summary>
        public int PitchKeytrack { get; private set; }

        /// <summary>Fixed detune in cents (<c>tune</c>/<c>pitch</c> plus <c>transpose</c>×100).</summary>
        public double TuneCents { get; private set; }
        /// <summary>Region gain in decibels (<c>volume</c>, default 0).</summary>
        public float VolumeDb { get; private set; }
        /// <summary>Pan normalised to −1 (left) … +1 (right) from SFZ's −100…100.</summary>
        public float Pan { get; private set; }
        /// <summary>Velocity-to-amplitude tracking percentage (<c>amp_veltrack</c>, default 100).</summary>
        public float AmpVelTrack { get; private set; }

        /// <summary>Amplitude-envelope delay in seconds (<c>ampeg_delay</c>).</summary>
        public float AmpegDelay { get; private set; }
        /// <summary>Amplitude-envelope attack in seconds (<c>ampeg_attack</c>).</summary>
        public float AmpegAttack { get; private set; }
        /// <summary>Amplitude-envelope hold in seconds (<c>ampeg_hold</c>).</summary>
        public float AmpegHold { get; private set; }
        /// <summary>Amplitude-envelope decay in seconds (<c>ampeg_decay</c>).</summary>
        public float AmpegDecay { get; private set; }
        /// <summary>Amplitude-envelope sustain level, 0…1 (<c>ampeg_sustain</c>%, default 1).</summary>
        public float AmpegSustain { get; private set; }
        /// <summary>Amplitude-envelope release in seconds (<c>ampeg_release</c>).</summary>
        public float AmpegRelease { get; private set; }

        /// <summary>Whether a filter cutoff was specified.</summary>
        public bool HasCutoff { get; private set; }
        /// <summary>Filter cutoff in Hz (<c>cutoff</c>), valid when <see cref="HasCutoff"/>.</summary>
        public float CutoffHz { get; private set; }
        /// <summary>Filter resonance in dB (<c>resonance</c>, default 0).</summary>
        public float ResonanceDb { get; private set; }
        /// <summary>The filter family (<c>fil_type</c>, default low-pass).</summary>
        public SfzFilterType FilterType { get; private set; }

        /// <summary>
        /// Loop behaviour (<c>loop_mode</c>). Only meaningful when
        /// <see cref="HasLoopMode"/> is true: per the SFZ spec an absent opcode
        /// defaults to <c>loop_continuous</c> when the sample file defines a loop
        /// (e.g. a WAV <c>smpl</c> chunk) and <c>no_loop</c> otherwise, which only
        /// the sample loader can decide.
        /// </summary>
        public SfzLoopMode LoopMode { get; private set; }
        /// <summary>Whether a <c>loop_mode</c>/<c>loopmode</c> opcode was specified.</summary>
        public bool HasLoopMode { get; private set; }
        /// <summary>Sample start offset in frames (<c>offset</c>, default 0).</summary>
        public int Offset { get; private set; }
        /// <summary>
        /// Sample end in frames (<c>end</c>, an <em>inclusive</em> index per the SFZ
        /// spec), meaningful when <see cref="HasEnd"/> is true. An explicit −1 means
        /// the region is disabled (not played) per the spec.
        /// </summary>
        public int End { get; private set; }
        /// <summary>Whether an <c>end</c> opcode was specified.</summary>
        public bool HasEnd { get; private set; }
        /// <summary>Loop start in frames (<c>loop_start</c>), meaningful when
        /// <see cref="HasLoopStart"/> is true; overrides any loop start embedded in the sample file.</summary>
        public int LoopStart { get; private set; }
        /// <summary>Whether a <c>loop_start</c>/<c>loopstart</c> opcode was specified.</summary>
        public bool HasLoopStart { get; private set; }
        /// <summary>Loop end in frames (<c>loop_end</c>, an <em>inclusive</em> index per the
        /// SFZ spec), meaningful when <see cref="HasLoopEnd"/> is true; overrides any loop
        /// end embedded in the sample file.</summary>
        public int LoopEnd { get; private set; }
        /// <summary>Whether a <c>loop_end</c>/<c>loopend</c> opcode was specified.</summary>
        public bool HasLoopEnd { get; private set; }

        /// <summary>When the region plays (<c>trigger</c>, default attack).</summary>
        public SfzTrigger Trigger { get; private set; }
        /// <summary>Exclusive group (<c>group</c>, 0 = none).</summary>
        public int Group { get; private set; }
        /// <summary>The group whose notes this region silences (<c>off_by</c>, 0 = none).</summary>
        public int OffBy { get; private set; }
        /// <summary>How this region is silenced by its group (<c>off_mode</c>, default fast).</summary>
        public SfzOffMode OffMode { get; private set; }
        /// <summary>Maximum simultaneous voices (<c>polyphony</c>, −1 = unlimited).</summary>
        public int Polyphony { get; private set; }

        /// <summary>Lowest keyswitch key (<c>sw_lokey</c>), or −1 if no keyswitch range.</summary>
        public int KeyswitchLow { get; private set; } = -1;
        /// <summary>Highest keyswitch key (<c>sw_hikey</c>), or −1.</summary>
        public int KeyswitchHigh { get; private set; } = -1;
        /// <summary>The keyswitch that must have been pressed last (<c>sw_last</c>), or −1.</summary>
        public int KeyswitchLast { get; private set; } = -1;
        /// <summary>The keyswitch active before any is pressed (<c>sw_default</c>), or −1.</summary>
        public int KeyswitchDefault { get; private set; } = -1;
        /// <summary>Round-robin length (<c>seq_length</c>, default 1 = none).</summary>
        public int SequenceLength { get; private set; } = 1;
        /// <summary>This region's 1-based round-robin slot (<c>seq_position</c>, default 1).</summary>
        public int SequencePosition { get; private set; } = 1;
        /// <summary>Low end of the random window (<c>lorand</c>, default 0).</summary>
        public float LowRandom { get; private set; }
        /// <summary>High end of the random window (<c>hirand</c>, default 1).</summary>
        public float HighRandom { get; private set; } = 1f;
        /// <summary>CC value windows that must all hold for the region to sound (<c>loccN</c>/<c>hiccN</c>).</summary>
        public IReadOnlyList<(int Controller, int Low, int High)> CcGates { get; private set; }

        /// <summary>Key crossfade-in low/high (<c>xfin_lokey</c>/<c>xfin_hikey</c>), or −1.</summary>
        public int KeyFadeInLow { get; private set; } = -1;
        /// <summary>Key crossfade-in high.</summary>
        public int KeyFadeInHigh { get; private set; } = -1;
        /// <summary>Key crossfade-out low/high (<c>xfout_lokey</c>/<c>xfout_hikey</c>), or −1.</summary>
        public int KeyFadeOutLow { get; private set; } = -1;
        /// <summary>Key crossfade-out high.</summary>
        public int KeyFadeOutHigh { get; private set; } = -1;
        /// <summary>Velocity crossfade-in low/high (<c>xfin_lovel</c>/<c>xfin_hivel</c>), or −1.</summary>
        public int VelocityFadeInLow { get; private set; } = -1;
        /// <summary>Velocity crossfade-in high.</summary>
        public int VelocityFadeInHigh { get; private set; } = -1;
        /// <summary>Velocity crossfade-out low/high (<c>xfout_lovel</c>/<c>xfout_hivel</c>), or −1.</summary>
        public int VelocityFadeOutLow { get; private set; } = -1;
        /// <summary>Velocity crossfade-out high.</summary>
        public int VelocityFadeOutHigh { get; private set; } = -1;
        /// <summary>Key-crossfade curve (<c>xf_keycurve</c>, default power).</summary>
        public SfzCrossfadeCurve KeyFadeCurve { get; private set; } = SfzCrossfadeCurve.Power;
        /// <summary>Velocity-crossfade curve (<c>xf_velcurve</c>, default power).</summary>
        public SfzCrossfadeCurve VelocityFadeCurve { get; private set; } = SfzCrossfadeCurve.Power;

        /// <summary>Release-trigger decay in dB per second the note was held (<c>rt_decay</c>, default 0).</summary>
        public float ReleaseDecayDbPerSecond { get; private set; }
        /// <summary>Send level to the first effect bus as a percentage (<c>effect1</c>, default 0).</summary>
        public float Effect1Percent { get; private set; }
        /// <summary>Send level to the second effect bus as a percentage (<c>effect2</c>, default 0).</summary>
        public float Effect2Percent { get; private set; }
        /// <summary>CC value windows that <em>trigger</em> the region when the controller
        /// rises into them (<c>on_loccN</c>/<c>on_hiccN</c>); null when the region is
        /// not CC-triggered.</summary>
        public IReadOnlyList<(int Controller, int Low, int High)> OnCcTriggers { get; private set; }
        /// <summary>The EQ bands the region specifies (<c>eq1_*</c>/<c>eq2_*</c>/<c>eq3_*</c>),
        /// with unspecified members defaulted (centre frequency 50/500/5000 Hz by band,
        /// bandwidth 1 octave, gain 0); null when no EQ opcode is present.</summary>
        public IReadOnlyList<SfzEqBand> EqBands { get; private set; }

        /// <summary>Amplitude (tremolo) LFO (<c>amplfo_freq</c>/<c>amplfo_depth</c>/<c>amplfo_delay</c>; depth in dB).</summary>
        public SfzLfo AmpLfo { get; private set; }
        /// <summary>Filter-cutoff LFO (<c>fillfo_freq</c>/<c>fillfo_depth</c>/<c>fillfo_delay</c>; depth in cents).</summary>
        public SfzLfo FilterLfo { get; private set; }
        /// <summary>Pitch (vibrato) LFO (<c>pitchlfo_freq</c>/<c>pitchlfo_depth</c>/<c>pitchlfo_delay</c>; depth in cents).</summary>
        public SfzLfo PitchLfo { get; private set; }
        /// <summary>Filter-cutoff modulation envelope (<c>fileg_*</c>; depth in cents).</summary>
        public SfzModulationEnvelope FilterEg { get; private set; }
        /// <summary>Pitch modulation envelope (<c>pitcheg_*</c>; depth in cents).</summary>
        public SfzModulationEnvelope PitchEg { get; private set; }

        /// <summary>
        /// Interprets a parsed <see cref="SfzRegion"/>'s opcodes. The
        /// <paramref name="noteOffset"/> and <paramref name="octaveOffset"/> (from
        /// the instrument's <c>&lt;control&gt;</c> section) transpose incoming MIDI
        /// notes; here that is realised by shifting the explicitly specified
        /// key-valued opcodes the opposite way.
        /// </summary>
        public static SfzMappedRegion Map(SfzRegion region, int noteOffset = 0, int octaveOffset = 0)
        {
            var r = new SfzMappedRegion(region);
            // note_offset/octave_offset transpose *incoming MIDI notes* upward
            // (note_offset=12: played key 48 behaves as 60, so the instrument
            // sounds an octave HIGHER). The mapping-side equivalent is shifting
            // every key-valued opcode *down* by the offset — hence the negation.
            int keyShift = -(noteOffset + 12 * octaveOffset);

            // key range: `key` sets all three; otherwise lokey/hikey/pitch_keycenter
            if (region.Has("key"))
            {
                int k = Clamp(Key(region, "key", 60) + keyShift);
                r.LoKey = r.HiKey = r.PitchKeycenter = k;
            }
            else
            {
                r.LoKey = region.Has("lokey") ? Clamp(Key(region, "lokey", 0) + keyShift) : 0;
                r.HiKey = region.Has("hikey") ? Clamp(Key(region, "hikey", 127) + keyShift) : 127;
                r.PitchKeycenter = region.Has("pitch_keycenter")
                    ? Clamp(Key(region, "pitch_keycenter", 60) + keyShift) : 60;
            }

            r.LoVel = region.GetInt("lovel", 0);
            r.HiVel = region.GetInt("hivel", 127);
            r.PitchKeytrack = region.GetInt("pitch_keytrack", 100);

            float tune = region.Has("tune") ? region.GetFloat("tune", 0) : region.GetFloat("pitch", 0);
            r.TuneCents = tune + region.GetInt("transpose", 0) * 100.0;
            r.VolumeDb = region.GetFloat("volume", 0);
            r.Pan = Clamp(region.GetFloat("pan", 0) / 100f, -1f, 1f);
            r.AmpVelTrack = region.GetFloat("amp_veltrack", 100);

            r.AmpegDelay = region.GetFloat("ampeg_delay", 0);
            r.AmpegAttack = region.GetFloat("ampeg_attack", 0);
            r.AmpegHold = region.GetFloat("ampeg_hold", 0);
            r.AmpegDecay = region.GetFloat("ampeg_decay", 0);
            r.AmpegSustain = Clamp(region.GetFloat("ampeg_sustain", 100) / 100f, 0f, 1f);
            r.AmpegRelease = region.GetFloat("ampeg_release", 0);

            r.HasCutoff = region.Has("cutoff");
            r.CutoffHz = region.GetFloat("cutoff", 0);
            r.ResonanceDb = region.GetFloat("resonance", 0);
            r.FilterType = ParseFilterType(region.GetString("fil_type"));

            r.HasLoopMode = region.Has("loop_mode") || region.Has("loopmode");
            r.LoopMode = ParseLoopMode(region.GetString("loop_mode", region.GetString("loopmode")));
            r.Offset = region.GetInt("offset", 0);
            r.HasEnd = region.Has("end");
            r.End = region.GetInt("end", -1);
            r.HasLoopStart = region.Has("loop_start") || region.Has("loopstart");
            r.LoopStart = region.GetInt("loop_start", region.GetInt("loopstart", -1));
            r.HasLoopEnd = region.Has("loop_end") || region.Has("loopend");
            r.LoopEnd = region.GetInt("loop_end", region.GetInt("loopend", -1));

            r.Trigger = ParseTrigger(region.GetString("trigger"));
            r.Group = region.GetInt("group", 0);
            r.OffBy = region.GetInt("off_by", region.GetInt("offby", 0));
            r.OffMode = ParseOffMode(region.GetString("off_mode"));
            r.Polyphony = region.GetInt("polyphony", -1);

            // Tier-2 note-on selection
            if (region.Has("sw_lokey")) r.KeyswitchLow = Clamp(Key(region, "sw_lokey", 0) + keyShift);
            if (region.Has("sw_hikey")) r.KeyswitchHigh = Clamp(Key(region, "sw_hikey", 127) + keyShift);
            if (region.Has("sw_last")) r.KeyswitchLast = Clamp(Key(region, "sw_last", 0) + keyShift);
            if (region.Has("sw_default")) r.KeyswitchDefault = Clamp(Key(region, "sw_default", 0) + keyShift);
            r.SequenceLength = region.GetInt("seq_length", 1);
            r.SequencePosition = region.GetInt("seq_position", 1);
            r.LowRandom = region.GetFloat("lorand", 0f);
            r.HighRandom = region.GetFloat("hirand", 1f);
            r.CcGates = BuildCcGates(region);

            // key/velocity crossfades
            if (region.Has("xfin_lokey")) r.KeyFadeInLow = Clamp(Key(region, "xfin_lokey", 0) + keyShift);
            if (region.Has("xfin_hikey")) r.KeyFadeInHigh = Clamp(Key(region, "xfin_hikey", 0) + keyShift);
            if (region.Has("xfout_lokey")) r.KeyFadeOutLow = Clamp(Key(region, "xfout_lokey", 0) + keyShift);
            if (region.Has("xfout_hikey")) r.KeyFadeOutHigh = Clamp(Key(region, "xfout_hikey", 0) + keyShift);
            if (region.Has("xfin_lovel")) r.VelocityFadeInLow = region.GetInt("xfin_lovel", -1);
            if (region.Has("xfin_hivel")) r.VelocityFadeInHigh = region.GetInt("xfin_hivel", -1);
            if (region.Has("xfout_lovel")) r.VelocityFadeOutLow = region.GetInt("xfout_lovel", -1);
            if (region.Has("xfout_hivel")) r.VelocityFadeOutHigh = region.GetInt("xfout_hivel", -1);
            r.KeyFadeCurve = ParseCrossfadeCurve(region.GetString("xf_keycurve"));
            r.VelocityFadeCurve = ParseCrossfadeCurve(region.GetString("xf_velcurve"));

            // Tier-2 modulation, EQ, effect sends, release decay and CC triggers
            r.ReleaseDecayDbPerSecond = region.GetFloat("rt_decay", 0);
            r.Effect1Percent = region.GetFloat("effect1", 0);
            r.Effect2Percent = region.GetFloat("effect2", 0);
            r.OnCcTriggers = BuildOnCcTriggers(region);
            r.EqBands = BuildEqBands(region);
            r.AmpLfo = BuildLfo(region, "amplfo");
            r.FilterLfo = BuildLfo(region, "fillfo");
            r.PitchLfo = BuildLfo(region, "pitchlfo");
            r.FilterEg = BuildModulationEnvelope(region, "fileg");
            r.PitchEg = BuildModulationEnvelope(region, "pitcheg");

            return r;
        }

        private static SfzLfo BuildLfo(SfzRegion region, string prefix) =>
            new SfzLfo(
                region.GetFloat(prefix + "_freq", 0),
                region.GetFloat(prefix + "_depth", 0),
                region.GetFloat(prefix + "_delay", 0));

        private static SfzModulationEnvelope BuildModulationEnvelope(SfzRegion region, string prefix) =>
            new SfzModulationEnvelope(
                region.GetFloat(prefix + "_delay", 0),
                region.GetFloat(prefix + "_attack", 0),
                region.GetFloat(prefix + "_hold", 0),
                region.GetFloat(prefix + "_decay", 0),
                region.GetFloat(prefix + "_sustain", 100f),
                region.GetFloat(prefix + "_release", 0),
                region.GetFloat(prefix + "_depth", 0));

        // Collects the eqN_* opcodes into typed bands: a band appears when any of
        // its three opcodes is present, with the spec defaults for the rest.
        private static IReadOnlyList<SfzEqBand> BuildEqBands(SfzRegion region)
        {
            List<SfzEqBand> bands = null;
            AddEqBand(region, "eq1", 50f, ref bands);
            AddEqBand(region, "eq2", 500f, ref bands);
            AddEqBand(region, "eq3", 5000f, ref bands);
            return bands;
        }

        private static void AddEqBand(SfzRegion region, string prefix, float defaultFreq,
            ref List<SfzEqBand> bands)
        {
            if (!region.Has(prefix + "_freq") && !region.Has(prefix + "_bw") && !region.Has(prefix + "_gain"))
                return;
            bands ??= new List<SfzEqBand>(3);
            bands.Add(new SfzEqBand(
                region.GetFloat(prefix + "_freq", defaultFreq),
                region.GetFloat(prefix + "_bw", 1f),
                region.GetFloat(prefix + "_gain", 0f)));
        }

        // Collects on_loccN/on_hiccN opcodes into per-controller trigger windows.
        private static IReadOnlyList<(int Controller, int Low, int High)> BuildOnCcTriggers(SfzRegion region)
        {
            Dictionary<int, (int Low, int High)> triggers = null;
            foreach (var pair in region.Opcodes)
            {
                bool low = pair.Key.StartsWith("on_locc");
                bool high = pair.Key.StartsWith("on_hicc");
                if (!low && !high) continue;
                if (!int.TryParse(pair.Key.Substring(7), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cc))
                    continue;
                if (!int.TryParse(pair.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    continue;

                triggers ??= new Dictionary<int, (int, int)>();
                var current = triggers.TryGetValue(cc, out var g) ? g : (Low: 0, High: 127);
                triggers[cc] = low ? (value, current.High) : (current.Low, value);
            }

            if (triggers == null) return null;
            var result = new List<(int, int, int)>(triggers.Count);
            foreach (var pair in triggers) result.Add((pair.Key, pair.Value.Low, pair.Value.High));
            return result;
        }

        // Collects loccN/hiccN opcodes into per-controller [low, high] windows.
        private static IReadOnlyList<(int Controller, int Low, int High)> BuildCcGates(SfzRegion region)
        {
            Dictionary<int, (int Low, int High)> gates = null;
            foreach (var pair in region.Opcodes)
            {
                bool low = pair.Key.StartsWith("locc");
                bool high = pair.Key.StartsWith("hicc");
                if (!low && !high) continue;
                if (!int.TryParse(pair.Key.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cc))
                    continue;
                if (!int.TryParse(pair.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    continue;

                gates ??= new Dictionary<int, (int, int)>();
                var current = gates.TryGetValue(cc, out var g) ? g : (Low: 0, High: 127);
                gates[cc] = low ? (value, current.High) : (current.Low, value);
            }

            if (gates == null) return null;
            var result = new List<(int, int, int)>(gates.Count);
            foreach (var pair in gates) result.Add((pair.Key, pair.Value.Low, pair.Value.High));
            return result;
        }

        /// <summary>Whether this region should sound for the given key and velocity.</summary>
        public bool Matches(int key, int velocity) =>
            key >= LoKey && key <= HiKey && velocity >= LoVel && velocity <= HiVel;

        private static int Key(SfzRegion region, string opcode, int fallback) =>
            SfzNoteName.Parse(region.GetString(opcode), fallback);

        private static int Clamp(int v) => v < 0 ? 0 : v > 127 ? 127 : v;
        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

        private static SfzLoopMode ParseLoopMode(string value)
        {
            switch (value)
            {
                case "one_shot": return SfzLoopMode.OneShot;
                case "loop_continuous": return SfzLoopMode.LoopContinuous;
                case "loop_sustain": return SfzLoopMode.LoopSustain;
                default: return SfzLoopMode.NoLoop;
            }
        }

        private static SfzTrigger ParseTrigger(string value)
        {
            switch (value)
            {
                case "release": return SfzTrigger.Release;
                case "first": return SfzTrigger.First;
                case "legato": return SfzTrigger.Legato;
                default: return SfzTrigger.Attack;
            }
        }

        private static SfzCrossfadeCurve ParseCrossfadeCurve(string value) =>
            value == "gain" ? SfzCrossfadeCurve.Linear : SfzCrossfadeCurve.Power;

        private static SfzOffMode ParseOffMode(string value) =>
            value == "normal" ? SfzOffMode.Normal : SfzOffMode.Fast;

        private static SfzFilterType ParseFilterType(string value)
        {
            if (string.IsNullOrEmpty(value)) return SfzFilterType.LowPass;
            if (value.StartsWith("hpf")) return SfzFilterType.HighPass;
            if (value.StartsWith("bpf")) return SfzFilterType.BandPass;
            if (value.StartsWith("brf")) return SfzFilterType.BandReject;
            return SfzFilterType.LowPass;
        }
    }
}
