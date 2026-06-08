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
            string defaultPath, int noteOffset, int octaveOffset)
        {
            Regions = regions;
            Sections = sections;
            DefaultPath = defaultPath;
            NoteOffset = noteOffset;
            OctaveOffset = octaveOffset;
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
    }
}
