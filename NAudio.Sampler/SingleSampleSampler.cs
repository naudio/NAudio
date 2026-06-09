using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Plays a <see cref="SingleSampleInstrument"/> polyphonically as an
    /// <see cref="ISampleProvider"/>, through the shared <see cref="SamplerEngine"/>
    /// (so it gets pitch, looping, the amplitude envelope, pan, voice stealing and
    /// the reverb/chorus sends for free). Edits to <see cref="Instrument"/> are
    /// reflected by the next note played.
    /// </summary>
    public sealed class SingleSampleSampler : SamplerEngine
    {
        private readonly SamplerRegion[] region = new SamplerRegion[1];

        /// <summary>Creates a sampler for a single-sample instrument.</summary>
        public SingleSampleSampler(SingleSampleInstrument instrument,
            int sampleRate = 44100, int maxVoices = 32)
            : base(sampleRate, maxVoices)
        {
            Instrument = instrument ?? throw new ArgumentNullException(nameof(instrument));
        }

        /// <summary>
        /// Loads a WAV file and maps it across the keyboard at <paramref name="rootKey"/>.
        /// </summary>
        public static SingleSampleSampler FromWaveFile(string path, int rootKey = 60,
            int sampleRate = 44100, int maxVoices = 32)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!WaveSampleLoader.TryLoad(path, out var left, out var right, out var rate))
                throw new InvalidOperationException($"Could not load WAV sample '{path}'");
            return new SingleSampleSampler(new SingleSampleInstrument(left, rate, rootKey, right), sampleRate, maxVoices);
        }

        /// <summary>The instrument being played. Mutate its properties to edit it live.</summary>
        public SingleSampleInstrument Instrument { get; }

        /// <inheritdoc />
        private protected override IReadOnlyList<SamplerRegion> GetRegionsForNoteOn(int channel, MidiChannelState state)
        {
            // rebuild from the (possibly just-edited) instrument so live changes
            // to loop points, tuning, gain etc. take effect on the next note
            region[0] = Instrument.BuildRegion();
            return region;
        }
    }
}
