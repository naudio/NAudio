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
    /// An SFZ region with its Tier-1 opcodes interpreted into typed,
    /// engine-ready synthesis parameters: absolute key/velocity ranges (note
    /// names resolved, offsets applied), tuning in cents, volume in dB, pan
    /// normalised to ±1, the amplitude envelope in seconds, the filter, loop
    /// behaviour, sample offsets, and group/trigger routing.
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

        /// <summary>Loop behaviour (<c>loop_mode</c>, default no loop).</summary>
        public SfzLoopMode LoopMode { get; private set; }
        /// <summary>Sample start offset in frames (<c>offset</c>, default 0).</summary>
        public int Offset { get; private set; }
        /// <summary>Sample end in frames, or −1 for the whole sample (<c>end</c>).</summary>
        public int End { get; private set; }
        /// <summary>Loop start in frames, or −1 if unset (<c>loop_start</c>).</summary>
        public int LoopStart { get; private set; }
        /// <summary>Loop end in frames, or −1 if unset (<c>loop_end</c>).</summary>
        public int LoopEnd { get; private set; }

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

        /// <summary>
        /// Interprets a parsed <see cref="SfzRegion"/>'s opcodes. The
        /// <paramref name="noteOffset"/> and <paramref name="octaveOffset"/> (from
        /// the instrument's <c>&lt;control&gt;</c> section) shift the key values
        /// that were explicitly specified.
        /// </summary>
        public static SfzMappedRegion Map(SfzRegion region, int noteOffset = 0, int octaveOffset = 0)
        {
            var r = new SfzMappedRegion(region);
            int keyShift = noteOffset + 12 * octaveOffset;

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

            r.LoopMode = ParseLoopMode(region.GetString("loop_mode", region.GetString("loopmode")));
            r.Offset = region.GetInt("offset", 0);
            r.End = region.GetInt("end", -1);
            r.LoopStart = region.GetInt("loop_start", region.GetInt("loopstart", -1));
            r.LoopEnd = region.GetInt("loop_end", region.GetInt("loopend", -1));

            r.Trigger = ParseTrigger(region.GetString("trigger"));
            r.Group = region.GetInt("group", 0);
            r.OffBy = region.GetInt("off_by", region.GetInt("offby", 0));
            r.OffMode = ParseOffMode(region.GetString("off_mode"));
            r.Polyphony = region.GetInt("polyphony", -1);

            return r;
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
