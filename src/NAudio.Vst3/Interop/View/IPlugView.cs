using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Plug-in view rectangle (<c>Steinberg::ViewRect</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ViewRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

/// <summary>
/// Platform-UI type identifiers (UTF-8 C strings) used with <c>IPlugView::attached</c>.
/// </summary>
/// <remarks>
/// The string passed to the plug-in is null-terminated ASCII / UTF-8. <see cref="Hwnd"/> is the
/// Windows variant; the others are listed for future macOS / iOS / Linux / Wayland support.
/// </remarks>
internal static class PlatformUITypes
{
    public const string Hwnd = "HWND";
    public const string HiView = "HIView";
    public const string NSView = "NSView";
    public const string UIView = "UIView";
    public const string X11EmbedWindowId = "X11EmbedWindowID";
    public const string WaylandSurfaceId = "WaylandSurfaceID";
}

/// <summary>
/// Plug-in view (<c>Steinberg::IPlugView</c>) — the embedded UI surface for a VST 3 plug-in.
/// Defined in <c>pluginterfaces/gui/iplugview.h</c>.
/// </summary>
/// <remarks>
/// On Windows, <see cref="Attached"/> receives an <c>HWND</c> as the parent handle; the
/// plug-in creates a child window under it. The host must call <see cref="Removed"/> before
/// releasing the parent window.
/// </remarks>
[GeneratedComInterface]
[Guid("5BC32507-D060-49EA-A615-1B522B755B29")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPlugView
{
    [PreserveSig]
    int IsPlatformTypeSupported(IntPtr type);

    [PreserveSig]
    int Attached(IntPtr parent, IntPtr type);

    [PreserveSig]
    int Removed();

    [PreserveSig]
    int OnWheel(float distance);

    /// <summary><paramref name="key"/> is a <c>char16</c> (UTF-16 code unit) — projected as
    /// <see cref="ushort"/> to keep the binding free of ANSI-default marshalling.</summary>
    [PreserveSig]
    int OnKeyDown(ushort key, short keyCode, short modifiers);

    /// <summary><paramref name="key"/> is a <c>char16</c> — see <see cref="OnKeyDown"/>.</summary>
    [PreserveSig]
    int OnKeyUp(ushort key, short keyCode, short modifiers);

    [PreserveSig]
    int GetSize(out ViewRect size);

    [PreserveSig]
    int OnSize(ref ViewRect newSize);

    [PreserveSig]
    int OnFocus(byte state);

    [PreserveSig]
    int SetFrame(IntPtr frame);

    [PreserveSig]
    int CanResize();

    [PreserveSig]
    int CheckSizeConstraint(ref ViewRect rect);
}

/// <summary>
/// Host callback (<c>Steinberg::IPlugFrame</c>) — receives resize requests from the plug-in UI.
/// Defined in <c>pluginterfaces/gui/iplugview.h</c>.
/// </summary>
[GeneratedComInterface]
[Guid("367FAF01-AFA9-4693-8D4D-A2A0ED0882A3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPlugFrame
{
    [PreserveSig]
    int ResizeView(IntPtr view, ref ViewRect newSize);
}

/// <summary>
/// Host callback (<c>Steinberg::IPlugViewContentScaleSupport</c>) — pushes DPI scale to the view
/// before attach. Critical on Windows 4K displays.
/// </summary>
[GeneratedComInterface]
[Guid("65ED9690-8AC4-4525-8AAD-EF7A72EA703F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPlugViewContentScaleSupport
{
    [PreserveSig]
    int SetContentScaleFactor(float factor);
}
