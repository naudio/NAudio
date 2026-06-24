using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 class factory (<c>Steinberg::IPluginFactory</c>). Every <c>.vst3</c> module exposes one
/// via the exported <c>GetPluginFactory</c> entry point.
/// </summary>
/// <remarks>
/// Defined in <c>pluginterfaces/base/ipluginbase.h</c>. The TUID parameters to
/// <see cref="CreateInstance"/> are 16-byte raw GUID bytes — NOT C strings, despite the SDK's
/// <c>FIDString</c> (<c>const char*</c>) typedef. The caller must pin the IID/CID bytes for
/// the duration of the call.
/// </remarks>
[GeneratedComInterface]
[Guid("7A4D811C-5211-4A1F-AED9-D2EE0B43BF9F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPluginFactory
{
    /// <summary>Fill a <see cref="PFactoryInfo"/> with vendor/url/email/flags.</summary>
    [PreserveSig]
    int GetFactoryInfo(out PFactoryInfo info);

    /// <summary>Number of classes this factory exposes.</summary>
    [PreserveSig]
    int CountClasses();

    /// <summary>Fill a <see cref="PClassInfo"/> for the class at <paramref name="index"/>.</summary>
    [PreserveSig]
    int GetClassInfo(int index, out PClassInfo info);

    /// <summary>
    /// Instantiate the class identified by <paramref name="cid"/> and return the requested
    /// interface (<paramref name="iid"/>) via <paramref name="obj"/>.
    /// </summary>
    /// <param name="cid">Pointer to 16-byte class TUID.</param>
    /// <param name="iid">Pointer to 16-byte interface TUID.</param>
    /// <param name="obj">Returned native pointer to the requested interface.</param>
    [PreserveSig]
    int CreateInstance(IntPtr cid, IntPtr iid, out IntPtr obj);
}

/// <summary>
/// VST 3 class factory v2 (<c>Steinberg::IPluginFactory2</c>). Adds <see cref="GetClassInfo2"/>
/// for richer class metadata (vendor, version, sub-category).
/// </summary>
[GeneratedComInterface]
[Guid("0007B650-F24B-4C0B-A464-EDB9F00B2ABB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPluginFactory2
{
    // ---- IPluginFactory methods (must redeclare in vtable order) ----

    [PreserveSig]
    int GetFactoryInfo(out PFactoryInfo info);

    [PreserveSig]
    int CountClasses();

    [PreserveSig]
    int GetClassInfo(int index, out PClassInfo info);

    [PreserveSig]
    int CreateInstance(IntPtr cid, IntPtr iid, out IntPtr obj);

    // ---- IPluginFactory2-specific methods ----

    [PreserveSig]
    int GetClassInfo2(int index, out PClassInfo2 info);
}

/// <summary>
/// VST 3 class factory v3 (<c>Steinberg::IPluginFactory3</c>). Adds unicode class info plus a
/// <see cref="SetHostContext"/> entry so the plug-in can receive the <c>IHostApplication</c> at
/// factory level (separate from per-component <see cref="IPluginBase.Initialize"/>).
/// </summary>
[GeneratedComInterface]
[Guid("4555A2AB-C123-4E57-9B12-291036878931")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPluginFactory3
{
    // ---- IPluginFactory methods ----

    [PreserveSig]
    int GetFactoryInfo(out PFactoryInfo info);

    [PreserveSig]
    int CountClasses();

    [PreserveSig]
    int GetClassInfo(int index, out PClassInfo info);

    [PreserveSig]
    int CreateInstance(IntPtr cid, IntPtr iid, out IntPtr obj);

    // ---- IPluginFactory2 methods ----

    [PreserveSig]
    int GetClassInfo2(int index, out PClassInfo2 info);

    // ---- IPluginFactory3-specific methods ----

    [PreserveSig]
    int GetClassInfoUnicode(int index, out PClassInfoW info);

    [PreserveSig]
    int SetHostContext(IntPtr context);
}
