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
        public int RootKey { get; set; }

        /// <summary>Lowest mapped MIDI key, inclusive (default 0).</summary>
        public int LoKey { get; set; } = 0;
        /// <summary>Highest mapped MIDI key, inclusive (default 127).</summary>
        public int HiKey { get; set; } = 127;
        /// <summary>Lowest mapped velocity, inclusive (default 0).</summary>
        public int LoVelocity { get; set; } = 0;
        /// <summary>Highest mapped velocity, inclusive (default 127).</summary>
        public int HiVelocity { get; set; } = 127;

        /// <summary>Fixed detune in cents (default 0).</summary>
        public double TuneCents { get; set; }
        /// <summary>Gain in decibels (default 0).</summary>
        public float VolumeDb { get; set; }
        /// <summary>Pan, −1 (left) … +1 (right) (default 0, centre).</summary>
        public float Pan { get; set; }
        /// <summary>Velocity-to-amplitude tracking percent (default 100; 0 = velocity ignored).</summary>
        public float VelocityTrackingPercent { get; set; } = 100f;

        /// <summary>Playback start in frames (default 0).</summary>
        public int Start { get; set; }
        /// <summary>Playback end in frames (default the sample length).</summary>
        public int End { get; set; }
        /// <summary>Loop behaviour (default none).</summary>
        public LoopMode LoopMode { get; set; } = LoopMode.None;
        /// <summary>Loop start in frames (default 0).</summary>
        public int LoopStart { get; set; }
        /// <summary>Loop end in frames (default the sample length).</summary>
        public int LoopEnd { get; set; }

        /// <summary>Amplitude-envelope delay in seconds (default 0).</summary>
        public float DelaySeconds { get; set; }
        /// <summary>Amplitude-envelope attack in seconds (default 0, instant).</summary>
        public float AttackSeconds { get; set; }
        /// <summary>Amplitude-envelope hold in seconds (default 0).</summary>
        public float HoldSeconds { get; set; }
        /// <summary>Amplitude-envelope decay in seconds (default 0).</summary>
        public float DecaySeconds { get; set; }
        /// <summary>Amplitude-envelope sustain level, 0..1 (default 1).</summary>
        public float SustainLevel { get; set; } = 1f;
        /// <summary>Amplitude-envelope release in seconds (default 0.01, an anti-click tail).</summary>
        public float ReleaseSeconds { get; set; } = 0.01f;

        /// <summary>Projects the current settings onto the neutral region the voice plays.</summary>
        internal SamplerRegion BuildRegion()
        {
            int length = data.Length;
            int start = Clamp(Start, 0, length);
            int end = Clamp(End <= 0 ? length : End, start, length);
            int loopStart = Clamp(LoopStart, start, end);
            int loopEnd = Clamp(LoopEnd <= 0 ? end : LoopEnd, start, end);

            var gen = SoundFontGenerators.CreateWithDefaults();
            gen[GeneratorEnum.InitialAttenuation] = (short)Math.Round(-VolumeDb * 10.0);
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
                    LoopEnd = loopEnd
                },
                Generators = gen,
                Modulators = null,
                VelocityTrackingPercent = VelocityTrackingPercent,
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
