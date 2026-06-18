using System;

namespace NAudio.Vst3;

/// <summary>
/// Behaviour flags for a VST 3® parameter (<c>Vst::ParameterInfo::ParameterFlags</c>).
/// </summary>
/// <remarks>
/// <para>
/// Mirrors <c>ParameterInfo::ParameterFlags</c> from
/// <c>pluginterfaces/vst/ivsteditcontroller.h</c>. Flags marked "advisory" describe how a
/// host's UI <i>should</i> treat the parameter — they don't change the legal value range.
/// </para>
/// </remarks>
[Flags]
public enum Vst3ParameterFlags
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>The parameter may be driven by host automation.</summary>
    CanAutomate = 1 << 0,

    /// <summary>The parameter is read-only; the host should not allow editing it.</summary>
    IsReadOnly = 1 << 1,

    /// <summary>Advisory: the parameter wraps around between min and max (e.g. an LFO phase).</summary>
    IsWrapAround = 1 << 2,

    /// <summary>Advisory: the parameter is a discrete list (use <c>StepCount + 1</c> entries).</summary>
    IsList = 1 << 3,

    /// <summary>Advisory: the parameter should not be displayed in a generic parameter list.</summary>
    IsHidden = 1 << 4,

    /// <summary>The parameter selects the active program for its owning unit.</summary>
    IsProgramChange = 1 << 15,

    /// <summary>
    /// The parameter is the plug-in's bypass switch. Hosts typically expose this as a button
    /// rather than a knob and use <see cref="Vst3ParameterCollection.BypassParameter"/> to find
    /// it.
    /// </summary>
    IsBypass = 1 << 16,
}
