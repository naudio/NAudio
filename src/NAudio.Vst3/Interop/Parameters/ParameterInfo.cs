using System;
using System.Runtime.InteropServices;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Parameter flags (<c>ParameterInfo::ParameterFlags</c>).
/// </summary>
[Flags]
internal enum ParameterFlags
{
    NoFlags = 0,
    CanAutomate = 1 << 0,
    IsReadOnly = 1 << 1,
    IsWrapAround = 1 << 2,
    IsList = 1 << 3,
    IsHidden = 1 << 4,
    IsProgramChange = 1 << 15,
    IsBypass = 1 << 16,
}

/// <summary>
/// Restart-component flags (<c>RestartFlags</c>) — passed to
/// <c>IComponentHandler::restartComponent</c> when plug-in state requires the host to re-query
/// parameters, buses, latency, etc.
/// </summary>
[Flags]
internal enum RestartFlags
{
    ReloadComponent = 1 << 0,
    IoChanged = 1 << 1,
    ParamValuesChanged = 1 << 2,
    LatencyChanged = 1 << 3,
    ParamTitlesChanged = 1 << 4,
    MidiCCAssignmentChanged = 1 << 5,
    NoteExpressionChanged = 1 << 6,
    IoTitlesChanged = 1 << 7,
    PrefetchableSupportChanged = 1 << 8,
    RoutingInfoChanged = 1 << 9,
    KeyswitchChanged = 1 << 10,
    ParamIDMappingChanged = 1 << 11,
}

/// <summary>
/// Parameter descriptor (<c>Vst::ParameterInfo</c>). Returned by
/// <c>IEditController::getParameterInfo</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ParameterInfo
{
    /// <summary>Unique parameter identifier (<c>ParamID</c> = <c>uint32</c>).</summary>
    public uint Id;
    /// <summary>UTF-16 title (<c>String128</c>).</summary>
    public fixed char Title[128];
    /// <summary>UTF-16 short title.</summary>
    public fixed char ShortTitle[128];
    /// <summary>UTF-16 unit string (e.g. <c>"dB"</c>).</summary>
    public fixed char Units[128];
    /// <summary>0 = continuous; 1 = toggle; &gt;1 = discrete step count.</summary>
    public int StepCount;
    /// <summary>Default normalised value, range [0, 1].</summary>
    public double DefaultNormalizedValue;
    /// <summary>Owning unit id (<c>UnitID</c> = <c>int32</c>).</summary>
    public int UnitId;
    /// <summary>Combination of <see cref="ParameterFlags"/>.</summary>
    public int Flags;
}
