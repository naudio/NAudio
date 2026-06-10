using System;
using NAudio.Dsp;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// The "drop a sample on the keyboard" instrument: a single audio buffer
    /// (a loaded WAV or a recorded snippet) auto-mapped across the keyboard at a
    /// chosen root key, with editable start/end and loop points, loop mode,
    /// tuning, gain, pan, velocity tracking and an amplitude envelope. It is the
    /// simplest producer of the format-neutral region the voice plays; edits are
    /// picked up by the next note (see <see cref="SingleSampleSampler"/>).
    ///
    /// Properties are plain mutable settings so a UI can bind to them directly.
    /// </summary>
    public sealed class SingleSampleInstrument
    {
        private readonly float[] data;
        private readonly float[] dataRight;
        private readonly int sampleRate;

        // backing fields for the editable settings; every write bumps Version so
        // the sampler re-projects the region only when something actually changed
        private int rootKey;
        private int loKey;
        private int hiKey = 127;
        private int loVelocity;
        private int hiVelocity = 127;
        private double tuneCents;
        private float volumeDb;
        private float pan;
        private float velocityTrackingPercent = 100f;
        private int start;
        private int end;
        private LoopMode loopMode = LoopMode.None;
        private int loopStart;
        private int loopEnd;
        private float loopCrossfadeSeconds;
        private float delaySeconds;
        private float attackSeconds;
        private float holdSeconds;
        private float decaySeconds;
        private float sustainLevel = 1f;
        private float releaseSeconds = 0.01f;
        private int version;

        /// <summary>
        /// A monotonic edit stamp, bumped by every property write. The sampler
        /// rebuilds its projected region only when this changes, so steady-state
        /// note-on does not re-project (or allocate) per note.
        /// </summary>
        internal int Version => version;

        /// <summary>
        /// Creates an instrument from a sample buffer, mapped across the whole
        /// keyboard at the given root key.
        /// </summary>
        /// <param name="data">The sample, or the left channel of a stereo sample (-1..1).</param>
        /// <param name="sampleRate">The sample's recording rate in Hz.</param>
        /// <param name="rootKey">The MIDI key that plays it at recorded pitch (default 60).</param>
        /// <param name="dataRight">The right channel of a stereo sample, or null for mono.</param>
        public SingleSampleInstrument(float[] data, int sampleRate, int rootKey = 60, float[] dataRight = null)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) throw new ArgumentException("Sample is empty", nameof(data));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (dataRight != null && dataRight.Length != data.Length)
                throw new ArgumentException("Right channel length must match the left channel.", nameof(dataRight));

            this.sampleRate = sampleRate;
            this.dataRight = dataRight;
            RootKey = rootKey;
            End = data.Length;
            LoopEnd = data.Length;
        }

        /// <summary>Whether the loaded sample is stereo.</summary>
        public bool IsStereo => dataRight != null;

        /// <summary>The sample's recording rate in Hz.</summary>
        public int SampleRate => sampleRate;

        /// <summary>The sample length in frames.</summary>
        public int Length => data.Length;

        /// <summary>The MIDI key that plays the sample at its recorded pitch (default 60).</summary>
        public int RootKey { get => rootKey; set { rootKey = value; version++; } }

        /// <summary>Lowest mapped MIDI key, inclusive (default 0).</summary>
        public int LoKey { get => loKey; set { loKey = value; version++; } }
        /// <summary>Highest mapped MIDI key, inclusive (default 127).</summary>
        public int HiKey { get => hiKey; set { hiKey = value; version++; } }
        /// <summary>Lowest mapped velocity, inclusive (default 0).</summary>
        public int LoVelocity { get => loVelocity; set { loVelocity = value; version++; } }
        /// <summary>Highest mapped velocity, inclusive (default 127).</summary>
        public int HiVelocity { get => hiVelocity; set { hiVelocity = value; version++; } }

        /// <summary>Fixed detune in cents (default 0).</summary>
        public double TuneCents { get => tuneCents; set { tuneCents = value; version++; } }
        /// <summary>Gain in decibels (default 0).</summary>
        public float VolumeDb { get => volumeDb; set { volumeDb = value; version++; } }
        /// <summary>Pan, −1 (left) … +1 (right) (default 0, centre).</summary>
        public float Pan { get => pan; set { pan = value; version++; } }
        /// <summary>Velocity-to-amplitude tracking percent (default 100; 0 = velocity ignored).</summary>
        public float VelocityTrackingPercent { get => velocityTrackingPercent; set { velocityTrackingPercent = value; version++; } }

        /// <summary>Playback start in frames (default 0).</summary>
        public int Start { get => start; set { start = value; version++; } }
        /// <summary>Playback end in frames (default the sample length).</summary>
        public int End { get => end; set { end = value; version++; } }
        /// <summary>Loop behaviour (default none).</summary>
        public LoopMode LoopMode { get => loopMode; set { loopMode = value; version++; } }
        /// <summary>Loop start in frames (default 0).</summary>
        public int LoopStart { get => loopStart; set { loopStart = value; version++; } }
        /// <summary>Loop end in frames (default the sample length).</summary>
        public int LoopEnd { get => loopEnd; set { loopEnd = value; version++; } }
        /// <summary>
        /// Loop-seam crossfade length in seconds (default 0). Smooths the loop wrap
        /// when the loop points don't fall on matching samples. Limited by the audio
        /// before <see cref="LoopStart"/>, so set a loop start that has some lead-in.
        /// </summary>
        public float LoopCrossfadeSeconds { get => loopCrossfadeSeconds; set { loopCrossfadeSeconds = value; version++; } }

        /// <summary>Amplitude-envelope delay in seconds (default 0).</summary>
        public float DelaySeconds { get => delaySeconds; set { delaySeconds = value; version++; } }
        /// <summary>Amplitude-envelope attack in seconds (default 0, instant).</summary>
        public float AttackSeconds { get => attackSeconds; set { attackSeconds = value; version++; } }
        /// <summary>Amplitude-envelope hold in seconds (default 0).</summary>
        public float HoldSeconds { get => holdSeconds; set { holdSeconds = value; version++; } }
        /// <summary>Amplitude-envelope decay in seconds (default 0).</summary>
        public float DecaySeconds { get => decaySeconds; set { decaySeconds = value; version++; } }
        /// <summary>Amplitude-envelope sustain level, 0..1 (default 1).</summary>
        public float SustainLevel { get => sustainLevel; set { sustainLevel = value; version++; } }
        /// <summary>Amplitude-envelope release in seconds (default 0.01, an anti-click tail).</summary>
        public float ReleaseSeconds { get => releaseSeconds; set { releaseSeconds = value; version++; } }

        /// <summary>Projects the current settings onto the neutral region the voice plays.</summary>
        internal SamplerRegion BuildRegion()
        {
            int length = data.Length;
            int start = Clamp(Start, 0, length);
            int end = Clamp(End <= 0 ? length : End, start, length);
            int loopStart = Clamp(LoopStart, start, end);
            int loopEnd = Clamp(LoopEnd <= 0 ? end : LoopEnd, start, end);

            var gen = SoundFontGenerators.CreateWithDefaults();
            // negative gain -> SF2 attenuation cB; a positive (boost) gain can't
            // ride the attenuation slot (the voice clamps it at >= 0), so it is
            // carried as the region's linear gain instead
            gen[GeneratorEnum.InitialAttenuation] = (short)Math.Round(Math.Max(0f, -VolumeDb) * 10.0);
            gen[GeneratorEnum.Pan] = (short)Math.Round(Math.Clamp(Pan, -1f, 1f) * 500.0);

            int coarse = (int)Math.Round(TuneCents / 100.0);
            gen[GeneratorEnum.CoarseTune] = (short)coarse;
            gen[GeneratorEnum.FineTune] = (short)Math.Round(TuneCents - coarse * 100.0);

            gen[GeneratorEnum.DelayVolumeEnvelope] = GeneratorUnits.ToTimecents(DelaySeconds);
            gen[GeneratorEnum.AttackVolumeEnvelope] = GeneratorUnits.ToTimecents(AttackSeconds);
            gen[GeneratorEnum.HoldVolumeEnvelope] = GeneratorUnits.ToTimecents(HoldSeconds);
            gen[GeneratorEnum.DecayVolumeEnvelope] = GeneratorUnits.ToTimecents(DecaySeconds);
            gen[GeneratorEnum.ReleaseVolumeEnvelope] = GeneratorUnits.ToTimecents(ReleaseSeconds);
            gen[GeneratorEnum.SustainVolumeEnvelope] = GeneratorUnits.SustainCentibels(SustainLevel);

            gen[GeneratorEnum.SampleModes] = (short)MapLoopMode(LoopMode);

            return new SamplerRegion
            {
                Sample = new SampleData
                {
                    Data = data,
                    DataRight = dataRight,
                    SampleRate = sampleRate,
                    RootKey = RootKey,
                    PitchCorrectionCents = 0,
                    Start = start,
                    End = end,
                    LoopStart = loopStart,
                    LoopEnd = loopEnd,
                    CrossfadeSamples = (int)(Math.Max(0f, LoopCrossfadeSeconds) * sampleRate)
                },
                Generators = gen,
                Modulators = null,
                VelocityTrackingPercent = VelocityTrackingPercent,
                GainLinear = VolumeDb > 0 ? (float)Math.Pow(10.0, VolumeDb / 20.0) : 1f,
                LoKey = (byte)Clamp(LoKey, 0, 127),
                HiKey = (byte)Clamp(HiKey, 0, 127),
                LoVelocity = (byte)Clamp(LoVelocity, 0, 127),
                HiVelocity = (byte)Clamp(HiVelocity, 0, 127)
            };
        }

        private static int MapLoopMode(LoopMode mode)
        {
            switch (mode)
            {
                case LoopMode.Continuous: return (int)SampleMode.LoopContinuously;
                case LoopMode.UntilRelease: return (int)SampleMode.LoopAndContinue;
                default: return (int)SampleMode.NoLoop;
            }
        }

        private static int Clamp(int value, int lo, int hi) => value < lo ? lo : value > hi ? hi : value;
    }
}
