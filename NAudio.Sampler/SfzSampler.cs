using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Sfz;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// A polyphonic software sampler that plays an SFZ instrument, rendering
    /// 32-bit float stereo through the <see cref="ISampleProvider"/> pull model.
    /// Built on the shared <see cref="SamplerEngine"/>, so it gets the same voice
    /// engine as <see cref="SoundFontSampler"/> (pitch, looping, DAHDSR envelope,
    /// resonant filter, pan, voice stealing, exclusive groups, reverb/chorus
    /// sends).
    ///
    /// An SFZ file is a single instrument (no banks/programs), so every channel
    /// plays the same region set. Regions whose sample is missing are dropped at
    /// load time. Note-on selection (keyswitches, round-robin, random layers, CC
    /// gating) and the Tier-1 opcode coverage are applied by the engine and
    /// <see cref="SfzRegionProjector"/>.
    /// </summary>
    public sealed class SfzSampler : SamplerEngine
    {
        private readonly IReadOnlyList<SamplerRegion> regions;
        private readonly int keyswitchLow;   // > keyswitchHigh when the instrument has no keyswitches
        private readonly int keyswitchHigh;

        /// <summary>
        /// Creates a sampler for a parsed SFZ instrument, loading its samples via
        /// <paramref name="loader"/>.
        /// </summary>
        public SfzSampler(SfzInstrument instrument, ISfzSampleLoader loader,
            int sampleRate = 44100, int maxVoices = 64)
            : base(sampleRate, maxVoices)
        {
            if (instrument == null) throw new ArgumentNullException(nameof(instrument));
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            var playable = new List<SamplerRegion>();
            int low = int.MaxValue, high = int.MinValue;
            foreach (var mapped in instrument.MapRegions())
            {
                // a region's sw_lokey/sw_hikey contributes to the keyswitch range
                if (mapped.KeyswitchLow >= 0 && mapped.KeyswitchHigh >= 0)
                {
                    low = Math.Min(low, mapped.KeyswitchLow);
                    high = Math.Max(high, mapped.KeyswitchHigh);
                }
                var region = SfzRegionProjector.Project(mapped, loader);
                if (region != null) playable.Add(region);
            }

            regions = playable;
            keyswitchLow = low;
            keyswitchHigh = high;
        }

        /// <summary>
        /// Loads and parses an <c>.sfz</c> file and its samples, resolving relative
        /// paths against the file's directory.
        /// </summary>
        public static SfzSampler FromFile(string path, int sampleRate = 44100, int maxVoices = 64)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var instrument = SfzParser.ParseFile(path);
            var loader = new FileSfzSampleLoader(Path.GetDirectoryName(Path.GetFullPath(path)));
            return new SfzSampler(instrument, loader, sampleRate, maxVoices);
        }

        /// <summary>The number of playable regions loaded.</summary>
        public int RegionCount => regions.Count;

        /// <inheritdoc />
        private protected override IReadOnlyList<SamplerRegion> GetRegionsForNoteOn(MidiChannelState channel) => regions;

        /// <inheritdoc />
        private protected override bool IsKeyswitch(int key) => key >= keyswitchLow && key <= keyswitchHigh;
    }
}
