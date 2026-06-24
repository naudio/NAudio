using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Host callback (<c>Vst::IComponentHandler</c>) — receives parameter-edit notifications and
/// restart requests from the plug-in UI.
/// </summary>
/// <remarks>
/// The host implements this interface. The plug-in calls it from the UI thread, in the sequence
/// <see cref="BeginEdit"/> → <see cref="PerformEdit"/>(...) → <see cref="EndEdit"/> when the user
/// drags a control.
/// </remarks>
[GeneratedComInterface]
[Guid("93A0BEA3-0BD0-45DB-8E89-0B0CC1E46AC6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IComponentHandler
{
    [PreserveSig]
    int BeginEdit(uint id);

    [PreserveSig]
    int PerformEdit(uint id, double valueNormalized);

    [PreserveSig]
    int EndEdit(uint id);

    /// <summary>
    /// Plug-in requests a host-side restart for a set of <see cref="RestartFlags"/> reasons
    /// (e.g. latency changed, parameter values changed, bus configuration changed).
    /// </summary>
    [PreserveSig]
    int RestartComponent(int flags);
}

/// <summary>
/// Extended host callback (<c>Vst::IComponentHandler2</c>) — optional. Supports dirty-state
/// signalling, "please open the editor" requests, and grouping multiple parameter edits under a
/// single host timestamp.
/// </summary>
[GeneratedComInterface]
[Guid("F040B4B3-A360-45EC-ABCD-C045B4D5A2CC")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IComponentHandler2
{
    [PreserveSig]
    int SetDirty(byte state);

    [PreserveSig]
    int RequestOpenEditor(System.IntPtr name);

    [PreserveSig]
    int StartGroupEdit();

    [PreserveSig]
    int FinishGroupEdit();
}
