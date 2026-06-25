namespace NAudio.Vst3;

/// <summary>
/// A VST 3® unit — a node in the plug-in's unit hierarchy, enumerated from its <c>IUnitInfo</c>.
/// Units group parameters and own program lists; the always-present root unit has
/// <see cref="Id"/> 0. Surfaced via <see cref="Vst3Plugin.Units"/>.
/// </summary>
public sealed class Vst3Unit
{
    internal Vst3Unit(int id, int parentId, string name, int programListId)
    {
        Id = id;
        ParentId = parentId;
        Name = name;
        ProgramListId = programListId;
    }

    /// <summary>The unit identifier (<c>UnitID</c>); 0 is the root unit.</summary>
    public int Id { get; }

    /// <summary>The parent unit id, or <c>-1</c> for the root unit.</summary>
    public int ParentId { get; }

    /// <summary>The unit name; may be empty.</summary>
    public string Name { get; }

    /// <summary>
    /// The id of the program list assigned to this unit, or <c>-1</c> when the unit has none.
    /// Match against <see cref="Vst3ProgramList.Id"/>.
    /// </summary>
    public int ProgramListId { get; }

    /// <inheritdoc/>
    public override string ToString() => $"Unit {Id} '{Name}'";
}
