namespace NAudio.Sfz;

/// <summary>
/// The SFZ section headers. An <c>.sfz</c> file is a sequence of sections,
/// each introduced by one of these in angle brackets (e.g. <c>&lt;region&gt;</c>)
/// and followed by <c>opcode=value</c> assignments.
/// </summary>
public enum SfzHeader
{
    /// <summary>A header not recognised by this parser (kept verbatim).</summary>
    Unknown,
    /// <summary><c>&lt;control&gt;</c> — file-wide settings (default_path, defines, offsets).</summary>
    Control,
    /// <summary><c>&lt;global&gt;</c> — opcodes applied to every region in the file.</summary>
    Global,
    /// <summary><c>&lt;master&gt;</c> — opcodes applied to every region until the next master (ARIA).</summary>
    Master,
    /// <summary><c>&lt;group&gt;</c> — opcodes applied to every region until the next group.</summary>
    Group,
    /// <summary><c>&lt;region&gt;</c> — one playable region.</summary>
    Region,
    /// <summary><c>&lt;curve&gt;</c> — a value-mapping curve table (ARIA).</summary>
    Curve,
    /// <summary><c>&lt;effect&gt;</c> — an effect bus definition (ARIA).</summary>
    Effect,
    /// <summary><c>&lt;midi&gt;</c> — MIDI setup section.</summary>
    Midi,
    /// <summary><c>&lt;sample&gt;</c> — embedded sample data section (Base64, rare).</summary>
    Sample
}
