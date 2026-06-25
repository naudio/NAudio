using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.SoundFile;

/// <summary>
/// Source-generated P/Invoke surface for the <c>libsndfile</c> library.
/// </summary>
/// <remarks>
/// The runtime SONAME differs per OS (<c>libsndfile.so.1</c> on Linux,
/// <c>libsndfile.1.dylib</c> on macOS, <c>sndfile.dll</c> or
/// <c>libsndfile-1.dll</c> on Windows depending on how it was built). A
/// <see cref="NativeLibrary"/> resolver maps the import name to whichever
/// of those is present so the library loads without the <c>-dev</c>
/// symlink and regardless of the Windows packaging variant.
/// </remarks>
internal static partial class SndFileInterop
{
    private const string Library = "libsndfile";

    // sf_open mode flags.
    internal const int SFM_READ = 0x10;
    internal const int SFM_WRITE = 0x20;

    // sf_seek / sf_vio_seek whence values (libc SEEK_*).
    internal const int SEEK_SET = 0;
    internal const int SEEK_CUR = 1;
    internal const int SEEK_END = 2;

    // Format bitfield masks.
    internal const int SF_FORMAT_SUBMASK = 0x0000FFFF;
    internal const int SF_FORMAT_TYPEMASK = 0x0FFF0000;

    // sf_command selectors.
    internal const int SFC_GET_FORMAT_MAJOR_COUNT = 0x1030;
    internal const int SFC_GET_FORMAT_MAJOR = 0x1031;
    internal const int SFC_GET_FORMAT_SUBTYPE_COUNT = 0x1032;
    internal const int SFC_GET_FORMAT_SUBTYPE = 0x1033;
    internal const int SFC_SET_CLIPPING = 0x10C0;
    internal const int SFC_SET_VBR_ENCODING_QUALITY = 0x1300;
    internal const int SFC_SET_COMPRESSION_LEVEL = 0x1301;

    // sf_set_string / sf_get_string field selectors.
    internal const int SF_STR_TITLE = 0x01;
    internal const int SF_STR_COPYRIGHT = 0x02;
    internal const int SF_STR_SOFTWARE = 0x03;
    internal const int SF_STR_ARTIST = 0x04;
    internal const int SF_STR_COMMENT = 0x05;
    internal const int SF_STR_DATE = 0x06;
    internal const int SF_STR_ALBUM = 0x07;
    internal const int SF_STR_LICENSE = 0x08;
    internal const int SF_STR_TRACKNUMBER = 0x09;
    internal const int SF_STR_GENRE = 0x10;

    // SF_TRUE, passed as the datasize argument for boolean sf_command
    // selectors like SFC_SET_CLIPPING.
    internal const int SF_TRUE = 1;

    [ModuleInitializer]
    internal static void RegisterResolver()
    {
        NativeLibrary.SetDllImportResolver(typeof(SndFileInterop).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != Library)
        {
            return IntPtr.Zero;
        }

        // One ordered candidate list covering every supported OS; TryLoad
        // simply fails on names that don't apply to the current platform.
        string[] candidates =
        {
            "libsndfile.so.1",   // Linux runtime SONAME
            "libsndfile.so",     // Linux with -dev symlink
            "libsndfile.1.dylib",// macOS (Homebrew)
            "libsndfile.dylib",  // macOS without version
            "sndfile.dll",       // Windows (vcpkg)
            "libsndfile-1.dll",  // Windows (official build)
            "libsndfile",        // last resort / custom deployment
        };

        foreach (var candidate in candidates)
        {
            if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    // --- Open / close -------------------------------------------------

    [LibraryImport(Library, EntryPoint = "sf_open", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr Open(string path, int mode, ref SfInfo sfinfo);

    [LibraryImport(Library, EntryPoint = "sf_open_virtual")]
    internal static partial IntPtr OpenVirtual(ref SfVirtualIo sfvirtual, int mode, ref SfInfo sfinfo, IntPtr userData);

    [LibraryImport(Library, EntryPoint = "sf_close")]
    internal static partial int Close(IntPtr sndfile);

    // --- Frame I/O ----------------------------------------------------

    [LibraryImport(Library, EntryPoint = "sf_readf_float")]
    internal static partial long ReadFloat(IntPtr sndfile, Span<float> ptr, long frames);

    [LibraryImport(Library, EntryPoint = "sf_writef_float")]
    internal static partial long WriteFloat(IntPtr sndfile, ReadOnlySpan<float> ptr, long frames);

    [LibraryImport(Library, EntryPoint = "sf_readf_short")]
    internal static partial long ReadShort(IntPtr sndfile, Span<short> ptr, long frames);

    [LibraryImport(Library, EntryPoint = "sf_writef_short")]
    internal static partial long WriteShort(IntPtr sndfile, ReadOnlySpan<short> ptr, long frames);

    [LibraryImport(Library, EntryPoint = "sf_seek")]
    internal static partial long Seek(IntPtr sndfile, long frames, int whence);

    [LibraryImport(Library, EntryPoint = "sf_write_sync")]
    internal static partial void WriteSync(IntPtr sndfile);

    // --- Errors -------------------------------------------------------

    [LibraryImport(Library, EntryPoint = "sf_error")]
    internal static partial int Error(IntPtr sndfile);

    [LibraryImport(Library, EntryPoint = "sf_strerror")]
    private static partial IntPtr StrError(IntPtr sndfile);

    internal static string ErrorString(IntPtr sndfile)
    {
        var ptr = StrError(sndfile);
        return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    // --- Capability detection ----------------------------------------

    [LibraryImport(Library, EntryPoint = "sf_format_check")]
    internal static partial int FormatCheck(ref SfInfo sfinfo);

    [LibraryImport(Library, EntryPoint = "sf_command")]
    internal static partial int CommandGetInt(IntPtr sndfile, int command, out int data, int dataSize);

    [LibraryImport(Library, EntryPoint = "sf_command")]
    internal static partial int CommandFormatInfo(IntPtr sndfile, int command, ref SfFormatInfo data, int dataSize);

    [LibraryImport(Library, EntryPoint = "sf_command")]
    internal static partial int CommandSetDouble(IntPtr sndfile, int command, ref double data, int dataSize);

    // For boolean/no-data selectors (e.g. SFC_SET_CLIPPING) where the
    // flag is carried in the dataSize argument and data is NULL.
    [LibraryImport(Library, EntryPoint = "sf_command")]
    internal static partial int Command(IntPtr sndfile, int command, IntPtr data, int dataSize);

    // --- Version / metadata ------------------------------------------

    [LibraryImport(Library, EntryPoint = "sf_version_string")]
    private static partial IntPtr VersionString();

    internal static string LibraryVersion()
    {
        var ptr = VersionString();
        return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    [LibraryImport(Library, EntryPoint = "sf_get_string")]
    private static partial IntPtr GetStringRaw(IntPtr sndfile, int strType);

    [LibraryImport(Library, EntryPoint = "sf_set_string", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SetString(IntPtr sndfile, int strType, string value);

    internal static string GetString(IntPtr sndfile, int strType)
    {
        var ptr = GetStringRaw(sndfile, strType);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }
}

/// <summary>
/// Mirror of libsndfile's <c>SF_INFO</c>. <c>sf_count_t</c> is a 64-bit
/// signed integer on every platform in libsndfile 1.x.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SfInfo
{
    public long Frames;
    public int SampleRate;
    public int Channels;
    public int Format;
    public int Sections;
    public int Seekable;
}

/// <summary>
/// Mirror of libsndfile's <c>SF_FORMAT_INFO</c> (used by the
/// <c>SFC_GET_FORMAT_*</c> capability queries).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SfFormatInfo
{
    public int Format;
    public IntPtr Name;
    public IntPtr Extension;
}

/// <summary>
/// Mirror of libsndfile's <c>SF_VIRTUAL_IO</c> — five unmanaged callbacks
/// that let libsndfile read/write through an arbitrary backing store
/// (here a <see cref="System.IO.Stream"/>). Function pointers to static
/// <c>[UnmanagedCallersOnly]</c> methods keep this AOT-safe.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SfVirtualIo
{
    public delegate* unmanaged[Cdecl]<IntPtr, long> GetFileLen;
    public delegate* unmanaged[Cdecl]<long, int, IntPtr, long> Seek;
    public delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr, long> Read;
    public delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr, long> Write;
    public delegate* unmanaged[Cdecl]<IntPtr, long> Tell;
}
