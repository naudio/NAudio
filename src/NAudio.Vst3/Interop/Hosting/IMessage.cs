using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Inter-object message (<c>Vst::IMessage</c>) — a named bag of attributes exchanged between the
/// component and controller halves of a plug-in over their <see cref="IConnectionPoint"/> link.
/// Defined in <c>pluginterfaces/vst/ivstmessage.h</c>.
/// </summary>
/// <remarks>
/// Host-allocated via <see cref="IHostApplication.CreateInstance"/> when the plug-in passes
/// <c>IMessage::iid</c> as both the class and interface id. JUCE-wrapped plug-ins also lean on
/// this allocator during <c>IComponent::getState</c> to build the binary parameter snapshot.
/// </remarks>
[GeneratedComInterface]
[Guid("936F033B-C6C0-47DB-BB08-82F813C1E613")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IMessage
{
    /// <summary>Returns the message's identifier as a borrowed C string (UTF-8/ASCII).</summary>
    [PreserveSig]
    IntPtr GetMessageId();

    /// <summary>Sets the message's identifier; the implementation copies the string.</summary>
    [PreserveSig]
    void SetMessageId(IntPtr id);

    /// <summary>
    /// Returns a borrowed pointer to the message's owned <see cref="IAttributeList"/>. Per the
    /// SDK convention the caller does not <c>Release</c> the returned pointer.
    /// </summary>
    [PreserveSig]
    IntPtr GetAttributes();
}

/// <summary>
/// Typed attribute store (<c>Vst::IAttributeList</c>) — string-keyed dictionary supporting four
/// value kinds: <c>int64</c>, <c>double</c>, UTF-16 string, and binary blob. Used by
/// <see cref="IMessage"/> and as the in-memory carrier for plug-in state on JUCE-wrapped and
/// Steinberg-SDK-helper plug-ins. Defined in <c>pluginterfaces/vst/ivstattributes.h</c>.
/// </summary>
/// <remarks>
/// Each method's <c>id</c> parameter is a <c>const char*</c> (the SDK's <c>AttrID</c> typedef);
/// <c>setString</c>/<c>getString</c> use UTF-16 (<c>TChar*</c>) for the value and report buffer
/// sizes in <em>bytes</em>, not characters. <c>setBinary</c> copies the supplied bytes; the
/// pointer returned from <c>getBinary</c> is owned by the attribute list and remains valid until
/// the value is overwritten or the list is released.
/// </remarks>
[GeneratedComInterface]
[Guid("1E5F0AEB-CC7F-4533-A254-401138AD5EE4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IAttributeList
{
    [PreserveSig]
    int SetInt(IntPtr id, long value);

    [PreserveSig]
    int GetInt(IntPtr id, out long value);

    [PreserveSig]
    int SetFloat(IntPtr id, double value);

    [PreserveSig]
    int GetFloat(IntPtr id, out double value);

    /// <summary><paramref name="value"/> is a null-terminated UTF-16 (<c>TChar*</c>) string.</summary>
    [PreserveSig]
    int SetString(IntPtr id, IntPtr value);

    /// <summary>
    /// Writes the stored UTF-16 string into the caller's buffer, null-terminated, truncated to
    /// fit <paramref name="sizeInBytes"/>.
    /// </summary>
    [PreserveSig]
    int GetString(IntPtr id, IntPtr value, uint sizeInBytes);

    /// <summary>The implementation copies <paramref name="data"/>; the caller may free it.</summary>
    [PreserveSig]
    int SetBinary(IntPtr id, IntPtr data, uint sizeInBytes);

    /// <summary>
    /// Returns a borrowed pointer to the stored bytes. Lifetime is tied to the attribute list and
    /// to subsequent overwrites of the same key.
    /// </summary>
    [PreserveSig]
    int GetBinary(IntPtr id, out IntPtr data, out uint sizeInBytes);
}

/// <summary>
/// Capability probe (<c>Vst::IPlugInterfaceSupport</c>) — exposed by the host application object
/// so plug-ins can ask "do you implement interface X?" before they bother to QI for it. Defined
/// in <c>pluginterfaces/vst/ivstpluginterfacesupport.h</c>.
/// </summary>
[GeneratedComInterface]
[Guid("4FB58B9E-9EAA-4E0F-AB36-1C1CCCB56FEA")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPlugInterfaceSupport
{
    /// <summary>
    /// Returns <see cref="TResultCodes.True"/> when the host implements the interface identified
    /// by the 16-byte TUID at <paramref name="iid"/>, otherwise <see cref="TResultCodes.False"/>.
    /// </summary>
    [PreserveSig]
    int IsPlugInterfaceSupported(IntPtr iid);
}
