using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.SoundFile;

/// <summary>
/// One container format the installed libsndfile build can produce.
/// </summary>
public sealed class SoundFileFormatInfo
{
    internal SoundFileFormatInfo(int rawFormat, string name, string extension)
    {
        RawFormat = rawFormat;
        Name = name ?? string.Empty;
        Extension = extension ?? string.Empty;
    }

    /// <summary>The libsndfile major-format bitfield value.</summary>
    public int RawFormat { get; }

    /// <summary>Human-readable name (e.g. <c>"WAV (Microsoft)"</c>).</summary>
    public string Name { get; }

    /// <summary>Typical file extension (e.g. <c>"wav"</c>), may be empty.</summary>
    public string Extension { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} (.{Extension})";
}

/// <summary>
/// Queries which formats the system libsndfile was built with. FLAC,
/// Vorbis, Opus and MP3 are optional libsndfile build features, so a
/// given install may not support them — check before encoding.
/// </summary>
public static class SoundFileCapabilities
{
    /// <summary>
    /// The version string of the loaded libsndfile (e.g.
    /// <c>"libsndfile-1.2.2"</c>) — useful for diagnostics, since codec
    /// availability is build-dependent.
    /// </summary>
    public static string LibraryVersion => SndFileInterop.LibraryVersion();

    /// <summary>
    /// Whether the libsndfile build can write the given format/subtype
    /// combination (uses <c>sf_format_check</c> at 48 kHz stereo).
    /// </summary>
    /// <param name="major">The container / codec.</param>
    /// <param name="subtype">The sample encoding, or <see cref="SoundFileSubtype.Default"/>.</param>
    /// <returns><c>true</c> if the combination is supported.</returns>
    public static bool IsFormatSupported(SoundFileMajorFormat major, SoundFileSubtype subtype = SoundFileSubtype.Default)
    {
        var info = new SfInfo
        {
            SampleRate = 48000,
            Channels = 2,
            Format = SoundFileFormatMap.BuildFormat(major, subtype)
        };
        return SndFileInterop.FormatCheck(ref info) != 0;
    }

    /// <summary>
    /// Lists every major (container) format the installed libsndfile
    /// build supports.
    /// </summary>
    /// <returns>The supported container formats.</returns>
    public static IReadOnlyList<SoundFileFormatInfo> GetSupportedMajorFormats()
    {
        var result = new List<SoundFileFormatInfo>();
        if (SndFileInterop.CommandGetInt(IntPtr.Zero, SndFileInterop.SFC_GET_FORMAT_MAJOR_COUNT, out int count, sizeof(int)) != 0
            || count <= 0)
        {
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            var fi = new SfFormatInfo { Format = i };
            if (SndFileInterop.CommandFormatInfo(IntPtr.Zero, SndFileInterop.SFC_GET_FORMAT_MAJOR, ref fi, Unsafe.SizeOf<SfFormatInfo>()) != 0)
            {
                continue;
            }
            result.Add(new SoundFileFormatInfo(
                fi.Format,
                fi.Name == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(fi.Name),
                fi.Extension == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(fi.Extension)));
        }
        return result;
    }
}
