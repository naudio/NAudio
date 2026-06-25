using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 <c>Steinberg::IPluginBase</c> — initialise / terminate the plug-in component.
/// </summary>
/// <remarks>
/// Defined in <c>pluginterfaces/base/ipluginbase.h</c>. The <c>context</c> passed to
/// <see cref="Initialize"/> must implement <c>IHostApplication</c>.
/// </remarks>
[GeneratedComInterface]
[Guid("22888DDB-156E-45AE-8358-B34808190625")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPluginBase
{
    /// <summary>
    /// The host passes a number of interfaces as context to initialise the plug-in class.
    /// If this call does not return <see cref="TResultCodes.Ok"/>, the object is released
    /// immediately and <see cref="Terminate"/> is NOT called.
    /// </summary>
    [PreserveSig]
    int Initialize(IntPtr context);

    /// <summary>
    /// Called before the plug-in is unloaded; release all references to host interfaces here.
    /// </summary>
    [PreserveSig]
    int Terminate();
}
