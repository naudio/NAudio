using System.Collections.Generic;

namespace NAudio.Sfz
{
    /// <summary>
    /// One parsed SFZ section: a header and the <c>opcode=value</c> pairs that
    /// followed it, in the order written. This is the raw parse result before the
    /// global/master/group/region hierarchy is flattened into
    /// <see cref="SfzRegion"/>s.
    /// </summary>
    public sealed class SfzSection
    {
        private readonly Dictionary<string, string> opcodes;

        internal SfzSection(SfzHeader header, string headerText, Dictionary<string, string> opcodes)
        {
            Header = header;
            HeaderText = headerText;
            this.opcodes = opcodes;
        }

        /// <summary>The section header.</summary>
        public SfzHeader Header { get; }

        /// <summary>
        /// The raw header text as written (without the angle brackets), e.g.
        /// <c>region</c>. Useful for <see cref="SfzHeader.Unknown"/> sections.
        /// </summary>
        public string HeaderText { get; }

        /// <summary>The opcodes declared in this section. Later duplicates win.</summary>
        public IReadOnlyDictionary<string, string> Opcodes => opcodes;
    }
}
