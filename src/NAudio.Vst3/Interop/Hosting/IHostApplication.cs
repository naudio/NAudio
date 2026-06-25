using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Host application callback (<c>Vst::IHostApplication</c>) passed to
/// <see cref="IPluginBase.Initialize"/> as the <c>context</c> argument. Defined in
/// <c>pluginterfaces/vst/ivsthostapplication.h</c>.
/// </summary>
/// <remarks>
/// Mandatory — many plug-ins refuse to initialise without it. <see cref="GetName"/> writes a
/// <c>String128</c> (UTF-16, 128 chars); <see cref="CreateInstance"/> lets the plug-in ask the
/// host for host-side objects like <c>IMessage</c> / <c>IAttributeList</c>.
/// </remarks>
[GeneratedComInterface]
[Guid("58E595CC-DB2D-4969-8B6A-AF8C36A664E5")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IHostApplication
{
    /// <summary>Write the host name into a UTF-16 buffer (<c>String128</c>, 128 chars).</summary>
    [PreserveSig]
    int GetName(IntPtr name);

    [PreserveSig]
    int CreateInstance(IntPtr cid, IntPtr iid, out IntPtr obj);
}

/// <summary>
/// Connection point (<c>Vst::IConnectionPoint</c>) — used to wire <see cref="IComponent"/> and
/// <see cref="IEditController"/> together so they exchange messages when they live in separate
/// C++ objects.
/// </summary>
[GeneratedComInterface]
[Guid("70A4156F-6E6E-4026-9891-48BFAA60D8D1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IConnectionPoint
{
    [PreserveSig]
    int Connect(IntPtr other);

    [PreserveSig]
    int Disconnect(IntPtr other);

    [PreserveSig]
    int Notify(IntPtr message);
}
