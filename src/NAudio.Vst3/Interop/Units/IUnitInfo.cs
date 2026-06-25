using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Unit / program-list information (<c>Vst::IUnitInfo</c>, from <c>ivstunits.h</c>). In VST 3 the
/// programs (presets) a plug-in ships are organised into <i>program lists</i> attached to <i>units</i>
/// (a hierarchy), rather than VST 2's single flat program bank. Implemented by the plug-in's
/// <c>IEditController</c>; QI for it (optional — many plug-ins don't implement it).
/// </summary>
/// <remarks>
/// Only the read-side methods the host needs for enumeration are declared, in their SDK vtable order
/// (getUnitCount, getUnitInfo, getProgramListCount, getProgramListInfo, getProgramName). The later
/// slots (getProgramInfo, hasProgramPitchNames, getProgramPitchName, getSelectedUnit, selectUnit,
/// getUnitByBus, setUnitProgramData) are intentionally omitted — they are never invoked, and omitting
/// trailing methods is safe because the declared ones still occupy the correct leading vtable slots.
/// </remarks>
[GeneratedComInterface]
[Guid("3D4BD6B5-913A-4FD2-A886-E768A5EB92C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IUnitInfo
{
    /// <summary>Number of units the plug-in exposes (always ≥ 1 — the root unit — when implemented).</summary>
    [PreserveSig]
    int GetUnitCount();

    /// <summary>Fills <paramref name="info"/> for the unit at <paramref name="unitIndex"/>.</summary>
    [PreserveSig]
    int GetUnitInfo(int unitIndex, out UnitInfo info);

    /// <summary>Number of program lists the plug-in exposes.</summary>
    [PreserveSig]
    int GetProgramListCount();

    /// <summary>Fills <paramref name="info"/> for the program list at <paramref name="listIndex"/>.</summary>
    [PreserveSig]
    int GetProgramListInfo(int listIndex, out ProgramListInfo info);

    /// <summary>
    /// Writes the name of program <paramref name="programIndex"/> in list <paramref name="listId"/>
    /// into <paramref name="name"/> (a <c>String128</c> = <c>char16[128]</c> buffer the host owns).
    /// </summary>
    [PreserveSig]
    int GetProgramName(int listId, int programIndex, IntPtr name);
}

/// <summary>Well-known sentinel ids for <see cref="UnitInfo"/> / <see cref="ProgramListInfo"/>.</summary>
internal static class Vst3UnitConstants
{
    /// <summary>The always-present root unit (<c>kRootUnitId</c>).</summary>
    public const int RootUnitId = 0;

    /// <summary>"No parent" sentinel for the root unit (<c>kNoParentUnitId</c>).</summary>
    public const int NoParentUnitId = -1;

    /// <summary>"No program list" sentinel (<c>kNoProgramListId</c>).</summary>
    public const int NoProgramListId = -1;
}

/// <summary>Unit descriptor (<c>Vst::UnitInfo</c>). Returned by <c>IUnitInfo::getUnitInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UnitInfo
{
    /// <summary>Unit identifier (<c>UnitID</c> = <c>int32</c>); 0 is the root unit.</summary>
    public int Id;
    /// <summary>Parent unit id, or <see cref="Vst3UnitConstants.NoParentUnitId"/> for the root.</summary>
    public int ParentUnitId;
    /// <summary>UTF-16 unit name (<c>String128</c>).</summary>
    public fixed char Name[128];
    /// <summary>Id of the program list assigned to this unit, or <see cref="Vst3UnitConstants.NoProgramListId"/>.</summary>
    public int ProgramListId;
}

/// <summary>
/// Program-list descriptor (<c>Vst::ProgramListInfo</c>). Returned by
/// <c>IUnitInfo::getProgramListInfo</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ProgramListInfo
{
    /// <summary>Program-list identifier (<c>ProgramListID</c> = <c>int32</c>).</summary>
    public int Id;
    /// <summary>UTF-16 program-list name (<c>String128</c>).</summary>
    public fixed char Name[128];
    /// <summary>Number of programs in the list.</summary>
    public int ProgramCount;
}
