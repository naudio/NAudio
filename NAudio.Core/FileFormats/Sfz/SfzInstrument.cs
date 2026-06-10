using System.Collections.Generic;

namespace NAudio.Sfz
{
    /// <summary>
    /// A parsed SFZ instrument: the flattened playable <see cref="SfzRegion"/>s,
    /// the raw <see cref="SfzSection"/>s in document order, and the file-wide
    /// <c>&lt;control&gt;</c> settings. Produced by <see cref="SfzParser"/>.
    /// </summary>
    public sealed class SfzInstrument
    {
        internal SfzInstrument(IReadOnlyList<SfzRegion> regions, IReadOnlyList<SfzSection> sections,
            string defaultPath, int noteOffset, int octaveOffset,
            IReadOnlyList<(int Controller, int Value)> initialControllerValues = null)
        {
            Regions = regions;
            Sections = sections;
            DefaultPath = defaultPath;
            NoteOffset = noteOffset;
            OctaveOffset = octaveOffset;
            InitialControllerValues = initialControllerValues;
        }

        /// <summary>The playable regions, with their opcodes fully merged.</summary>
        public IReadOnlyList<SfzRegion> Regions { get; }

        /// <summary>Every parsed section, in the order written.</summary>
        public IReadOnlyList<SfzSection> Sections { get; }

        /// <summary>
        /// The <c>default_path</c> from the <c>&lt;control&gt;</c> section
        /// (backslashes normalised to forward slashes), or null if none was set.
        /// Already applied to each region's <see cref="SfzRegion.Sample"/>.
        /// </summary>
        public string DefaultPath { get; }

        /// <summary>The <c>note_offset</c> from <c>&lt;control&gt;</c> (default 0).</summary>
        public int NoteOffset { get; }

        /// <summary>The <c>octave_offset</c> from <c>&lt;control&gt;</c> (default 0).</summary>
        public int OctaveOffset { get; }

        /// <summary>
        /// Initial MIDI controller values from <c>&lt;control&gt;</c>
        /// <c>set_ccN</c> opcodes (values clamped to 0–127; the last setting per
        /// controller wins), or null when the file sets none. A player should
        /// seed each channel's controller state with these at load time — they
        /// matter because <c>loccN</c>/<c>hiccN</c> gating reads controllers
        /// that otherwise default to 0 — without treating them as controller
        /// <em>changes</em> (so <c>on_loccN</c>/<c>on_hiccN</c> trigger regions
        /// must not fire from them).
        /// </summary>
        public IReadOnlyList<(int Controller, int Value)> InitialControllerValues { get; }

        /// <summary>
        /// Interprets every region's opcodes into typed
        /// <see cref="SfzMappedRegion"/>s, applying this instrument's
        /// note/octave offsets.
        /// </summary>
        public IReadOnlyList<SfzMappedRegion> MapRegions()
        {
            var mapped = new List<SfzMappedRegion>(Regions.Count);
            foreach (var region in Regions)
                mapped.Add(SfzMappedRegion.Map(region, NoteOffset, OctaveOffset));
            return mapped;
        }
    }
}
