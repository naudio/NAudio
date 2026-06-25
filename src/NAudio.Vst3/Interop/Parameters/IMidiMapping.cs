using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Maps MIDI controllers to plug-in parameters (<c>Vst::IMidiMapping</c>, from
/// <c>ivstmidicontrollers.h</c>). In VST 3, MIDI CCs / pitch-bend / mod-wheel / aftertouch are
/// <b>not</b> events — the host resolves each to a parameter id via this interface and then sends a
/// normalised parameter change. Implemented by the plug-in's <c>IEditController</c>; QI for it.
/// </summary>
[GeneratedComInterface]
[Guid("DF0FF9F7-49B7-4669-B63A-B7327ADBF5E5")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IMidiMapping
{
    /// <summary>
    /// Returns the parameter id a given MIDI controller is assigned to on a bus/channel, or a
    /// non-OK result if the controller is unmapped.
    /// </summary>
    /// <param name="busIndex">Event bus index.</param>
    /// <param name="channel">Channel within the bus (0-based).</param>
    /// <param name="midiControllerNumber">A <see cref="Vst3ControllerNumbers"/> value (0–129).</param>
    /// <param name="id">Receives the assigned parameter id.</param>
    [PreserveSig]
    int GetMidiControllerAssignment(int busIndex, short channel, short midiControllerNumber, out uint id);
}

/// <summary>
/// Well-known VST 3 MIDI controller numbers (<c>Vst::ControllerNumbers</c>). Values 0–127 are the
/// standard MIDI CCs; the host also maps channel pressure and pitch-bend as "controllers" 128/129.
/// </summary>
internal static class Vst3ControllerNumbers
{
    public const short ModWheel = 1;       // CC 1
    public const short Expression = 11;    // CC 11
    public const short SustainOnOff = 64;  // CC 64
    public const short AfterTouch = 128;   // channel pressure
    public const short PitchBend = 129;    // 14-bit pitch wheel
}
