using System.Runtime.InteropServices;

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 factory flags (<c>PFactoryInfo::FactoryFlags</c>) — describes how the host should treat
/// the plug-in factory.
/// </summary>
[System.Flags]
internal enum FactoryFlags
{
    None = 0,
    /// <summary>The host must not cache class info — class list may change each load.</summary>
    ClassesDiscardable = 1 << 0,
    /// <summary>Deprecated — ignored from Cubase/Nuendo 12 onwards.</summary>
    LicenseCheck = 1 << 1,
    /// <summary>Component must not be unloaded until process exit.</summary>
    ComponentNonDiscardable = 1 << 3,
    /// <summary>All factory strings are unicode. Set for every VST 3 plug-in in practice.</summary>
    Unicode = 1 << 4,
}

/// <summary>
/// Raw factory-info layout (<c>PFactoryInfo</c>). Matches the C++ struct byte-for-byte so the
/// host can pass a pointer to <c>IPluginFactory::getFactoryInfo</c>.
/// </summary>
/// <remarks>
/// Strings are fixed-size char8 buffers in the SDK. We project them as inline byte buffers and
/// decode to <see cref="string"/> at the wrapper boundary. <see cref="Vendor"/>, <see cref="Url"/>,
/// and <see cref="Email"/> are zero-terminated UTF-8 strings; the <see cref="FactoryFlags.Unicode"/>
/// bit in <see cref="Flags"/> is set when the class-info strings are UTF-16 but the factory-info
/// strings themselves are always UTF-8 (per the SDK).
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PFactoryInfo
{
    public const int NameSize = 64;
    public const int UrlSize = 256;
    public const int EmailSize = 128;

    public fixed byte Vendor[NameSize];
    public fixed byte Url[UrlSize];
    public fixed byte Email[EmailSize];
    public int Flags;
}
