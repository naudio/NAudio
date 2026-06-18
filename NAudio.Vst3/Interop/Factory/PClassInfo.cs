using System.Runtime.InteropServices;

namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 class-info v1 layout (<c>PClassInfo</c>). The first form of class metadata returned by
/// <c>IPluginFactory::getClassInfo</c>.
/// </summary>
/// <remarks>
/// All strings are UTF-8 char8 buffers. Plug-ins that set <see cref="FactoryFlags.Unicode"/> on
/// their factory still use UTF-8 strings here — the unicode flag affects the v2/v3 variants only.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PClassInfo
{
    public const int CategorySize = 32;
    public const int NameSize = 64;
    public const int ManyInstances = 0x7FFFFFFF;

    /// <summary>Class ID (16-byte raw TUID).</summary>
    public fixed byte Cid[16];

    /// <summary>Number of allowed instances — typically <see cref="ManyInstances"/>.</summary>
    public int Cardinality;

    /// <summary>Category string (e.g. <c>"Audio Module Class"</c>).</summary>
    public fixed byte Category[CategorySize];

    /// <summary>User-visible class name.</summary>
    public fixed byte Name[NameSize];
}

/// <summary>
/// VST 3 class-info v2 layout (<c>PClassInfo2</c>). Adds vendor / version / sub-category /
/// SDK-version strings on top of <see cref="PClassInfo"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PClassInfo2
{
    public const int VendorSize = 64;
    public const int VersionSize = 64;
    public const int SubCategoriesSize = 128;

    public fixed byte Cid[16];
    public int Cardinality;
    public fixed byte Category[PClassInfo.CategorySize];
    public fixed byte Name[PClassInfo.NameSize];

    /// <summary>Flags used by a specific category (<c>ComponentFlags</c>).</summary>
    public uint ClassFlags;

    /// <summary>Sub-categories, OR-combined (e.g. <c>"Fx|Reverb"</c>).</summary>
    public fixed byte SubCategories[SubCategoriesSize];

    public fixed byte Vendor[VendorSize];
    public fixed byte Version[VersionSize];
    public fixed byte SdkVersion[VersionSize];
}

/// <summary>
/// VST 3 class-info unicode layout (<c>PClassInfoW</c>). Same shape as <see cref="PClassInfo2"/>
/// but with UTF-16 strings for everything that is user-visible.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PClassInfoW
{
    public fixed byte Cid[16];
    public int Cardinality;
    public fixed byte Category[PClassInfo.CategorySize];

    /// <summary>UTF-16 name buffer (<c>char16[NameSize]</c>).</summary>
    public fixed char Name[PClassInfo.NameSize];

    public uint ClassFlags;
    public fixed byte SubCategories[PClassInfo2.SubCategoriesSize];

    /// <summary>UTF-16 vendor buffer.</summary>
    public fixed char Vendor[PClassInfo2.VendorSize];
    /// <summary>UTF-16 version buffer.</summary>
    public fixed char Version[PClassInfo2.VersionSize];
    /// <summary>UTF-16 SDK-version buffer.</summary>
    public fixed char SdkVersion[PClassInfo2.VersionSize];
}
