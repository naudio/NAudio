using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 edit controller (<c>Vst::IEditController</c>). Owns the parameter authority and the
/// plug-in UI. Defined in <c>pluginterfaces/vst/ivsteditcontroller.h</c>.
/// </summary>
/// <remarks>
/// Inherits from <see cref="IPluginBase"/>. The component (<see cref="IComponent"/>) and the
/// controller may be the same C++ object or two distinct objects connected through
/// <c>IConnectionPoint</c>; the host must handle both cases.
/// </remarks>
[GeneratedComInterface]
[Guid("DCD7BBE3-7742-448D-A874-AACC979C759E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IEditController
{
    // ---- IPluginBase methods ----

    [PreserveSig]
    int Initialize(IntPtr context);

    [PreserveSig]
    int Terminate();

    // ---- IEditController methods ----

    /// <summary>Receives the component state (so the controller can mirror its parameters).</summary>
    [PreserveSig]
    int SetComponentState(IntPtr state);

    [PreserveSig]
    int SetState(IntPtr state);

    [PreserveSig]
    int GetState(IntPtr state);

    [PreserveSig]
    int GetParameterCount();

    [PreserveSig]
    int GetParameterInfo(int paramIndex, out ParameterInfo info);

    /// <summary>
    /// Format a normalised value as a UTF-16 string (<c>String128</c>) into
    /// <paramref name="stringOut"/>.
    /// </summary>
    [PreserveSig]
    int GetParamStringByValue(uint id, double valueNormalized, IntPtr stringOut);

    /// <summary>
    /// Parse a UTF-16 string back to a normalised value.
    /// </summary>
    [PreserveSig]
    int GetParamValueByString(uint id, IntPtr stringIn, out double valueNormalized);

    /// <summary>Convert a normalised value to plain (e.g. dB).</summary>
    [PreserveSig]
    double NormalizedParamToPlain(uint id, double valueNormalized);

    /// <summary>Convert a plain value to normalised.</summary>
    [PreserveSig]
    double PlainParamToNormalized(uint id, double plainValue);

    /// <summary>Read the current normalised value for a parameter.</summary>
    [PreserveSig]
    double GetParamNormalized(uint id);

    /// <summary>
    /// Set the current normalised value. The controller MUST NOT echo this through
    /// <c>IComponentHandler</c> — it's a "GUI refresh only" call.
    /// </summary>
    [PreserveSig]
    int SetParamNormalized(uint id, double value);

    /// <summary>Hands the host's <c>IComponentHandler</c> to the controller.</summary>
    [PreserveSig]
    int SetComponentHandler(IntPtr handler);

    /// <summary>
    /// Creates the editor view. Pass the UTF-8 string <c>"editor"</c> (<c>ViewType::kEditor</c>).
    /// Returns a native <c>IPlugView*</c> via the return value (NOT an out parameter).
    /// </summary>
    [PreserveSig]
    IntPtr CreateView(IntPtr name);
}
