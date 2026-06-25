using System.Collections.Generic;

namespace NAudio.Vst3;

/// <summary>
/// A VST 3® program list — a named set of programs (presets) the plug-in ships, enumerated from its
/// <c>IUnitInfo</c>. Surfaced via <see cref="Vst3Plugin.ProgramLists"/> and
/// <see cref="Vst3Plugin.ActiveProgramList"/>.
/// </summary>
/// <remarks>
/// To switch to a program by index, drive the plug-in's program-change parameter with
/// <see cref="Vst3Plugin.SendProgramChange"/> (live) or <see cref="Vst3Plugin.EnqueueProgramChange"/>
/// (segment-driven) — VST 3 has no program-change <i>event</i>; selection is a parameter change.
/// </remarks>
public sealed class Vst3ProgramList
{
    internal Vst3ProgramList(int id, string name, IReadOnlyList<string> programs)
    {
        Id = id;
        Name = name;
        Programs = programs;
    }

    /// <summary>The program-list identifier (<c>ProgramListID</c>).</summary>
    public int Id { get; }

    /// <summary>The program-list name (e.g. <c>"Factory"</c>); may be empty.</summary>
    public string Name { get; }

    /// <summary>The program names, indexed by program number (0-based).</summary>
    public IReadOnlyList<string> Programs { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Programs.Count} programs)";
}
