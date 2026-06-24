using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Optional extension on top of <see cref="IBStream"/> that lets a plug-in fetch metadata about
/// the stream — a filename hint plus a free-form <see cref="IAttributeList"/> bag for additional
/// host-supplied context (<c>Vst::IStreamAttributes</c>, defined in
/// <c>pluginterfaces/vst/ivstattributes.h</c>).
/// </summary>
/// <remarks>
/// <para>
/// Plug-ins QI for this on the stream the host hands to <c>setState</c> / <c>getState</c> to
/// learn e.g. the name of the preset file they're reading from. Hosts that don't have a filename
/// to share fill the buffer with <c>'\0'</c> and return <see cref="TResultCodes.False"/>; the
/// <c>IAttributeList</c> may be empty but should still be a valid non-null pointer so a plug-in
/// that dereferences it without checking the result code doesn't fault.
/// </para>
/// </remarks>
[GeneratedComInterface]
[Guid("D6CE2FFC-EFAF-4B8C-9E74-F1BB12DA44B4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IStreamAttributes
{
    /// <summary>
    /// Writes the stream's filename (without extension) into the caller-supplied
    /// <c>String128</c> = <c>char16[128]</c> buffer. Implementations that have no filename should
    /// null-terminate the buffer and return <see cref="TResultCodes.False"/>.
    /// </summary>
    [PreserveSig]
    int GetFileName(IntPtr name);

    /// <summary>
    /// Returns a borrowed pointer to the stream's <see cref="IAttributeList"/>. Per SDK convention
    /// the caller does not <c>Release</c> the returned pointer; lifetime is tied to the stream.
    /// </summary>
    [PreserveSig]
    IntPtr GetAttributes();
}
