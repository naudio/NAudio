using System;
using System.IO;

namespace NAudio.SoundFile;

/// <summary>
/// The container / codec a <see cref="SoundFileWriter"/> produces. This
/// is the discoverable, NAudio-idiomatic way to pick an output format;
/// power users who need a format libsndfile supports but this enum omits
/// can use the raw-<see cref="int"/> writer constructor instead.
/// </summary>
public enum SoundFileMajorFormat
{
    /// <summary>Microsoft WAV (RIFF).</summary>
    Wav,
    /// <summary>Apple/SGI AIFF.</summary>
    Aiff,
    /// <summary>Sun/NeXT AU.</summary>
    Au,
    /// <summary>Apple Core Audio Format.</summary>
    Caf,
    /// <summary>Sony Wave64.</summary>
    W64,
    /// <summary>Headerless raw PCM (defaults to 16-bit PCM if no subtype is set).</summary>
    Raw,
    /// <summary>FLAC (requires libsndfile built with libFLAC).</summary>
    Flac,
    /// <summary>Ogg container with the Vorbis codec (requires libvorbis).</summary>
    OggVorbis,
    /// <summary>Ogg container with the Opus codec (libsndfile ≥ 1.0.29 + libopus).</summary>
    Opus,
    /// <summary>MPEG audio, Layer III (libsndfile ≥ 1.1.0).</summary>
    Mp3
}

/// <summary>
/// The sample encoding stored inside the file. <see cref="Default"/> lets
/// the writer pick the natural subtype for the chosen
/// <see cref="SoundFileMajorFormat"/> (e.g. Vorbis for Ogg/Vorbis,
/// 16-bit PCM for WAV).
/// </summary>
public enum SoundFileSubtype
{
    /// <summary>Pick the natural subtype for the major format.</summary>
    Default,
    /// <summary>Unsigned 8-bit PCM.</summary>
    PcmU8,
    /// <summary>Signed 8-bit PCM.</summary>
    PcmS8,
    /// <summary>Signed 16-bit PCM.</summary>
    Pcm16,
    /// <summary>Signed 24-bit PCM.</summary>
    Pcm24,
    /// <summary>Signed 32-bit PCM.</summary>
    Pcm32,
    /// <summary>32-bit IEEE float.</summary>
    Float,
    /// <summary>64-bit IEEE double.</summary>
    Double,
    /// <summary>Xiph Vorbis.</summary>
    Vorbis,
    /// <summary>Xiph Opus.</summary>
    Opus,
    /// <summary>MPEG Layer III.</summary>
    Mp3
}

/// <summary>
/// Maps the <see cref="SoundFileMajorFormat"/> / <see cref="SoundFileSubtype"/>
/// enums onto libsndfile's <c>SF_FORMAT_*</c> bitfield, and infers a major
/// format from a file extension.
/// </summary>
internal static class SoundFileFormatMap
{
    // libsndfile SF_FORMAT_* major (container) values.
    private const int SF_FORMAT_WAV = 0x010000;
    private const int SF_FORMAT_AIFF = 0x020000;
    private const int SF_FORMAT_AU = 0x030000;
    private const int SF_FORMAT_RAW = 0x040000;
    private const int SF_FORMAT_W64 = 0x0B0000;
    private const int SF_FORMAT_CAF = 0x180000;
    private const int SF_FORMAT_FLAC = 0x170000;
    private const int SF_FORMAT_OGG = 0x200000;
    private const int SF_FORMAT_MPEG = 0x230000;

    // libsndfile SF_FORMAT_* subtype values.
    private const int SF_FORMAT_PCM_S8 = 0x0001;
    private const int SF_FORMAT_PCM_16 = 0x0002;
    private const int SF_FORMAT_PCM_24 = 0x0003;
    private const int SF_FORMAT_PCM_32 = 0x0004;
    private const int SF_FORMAT_PCM_U8 = 0x0005;
    private const int SF_FORMAT_FLOAT = 0x0006;
    private const int SF_FORMAT_DOUBLE = 0x0007;
    private const int SF_FORMAT_VORBIS = 0x0060;
    private const int SF_FORMAT_OPUS = 0x0064;
    private const int SF_FORMAT_MPEG_LAYER_III = 0x0082;

    public static int MajorToNative(SoundFileMajorFormat major) => major switch
    {
        SoundFileMajorFormat.Wav => SF_FORMAT_WAV,
        SoundFileMajorFormat.Aiff => SF_FORMAT_AIFF,
        SoundFileMajorFormat.Au => SF_FORMAT_AU,
        SoundFileMajorFormat.Caf => SF_FORMAT_CAF,
        SoundFileMajorFormat.W64 => SF_FORMAT_W64,
        SoundFileMajorFormat.Raw => SF_FORMAT_RAW,
        SoundFileMajorFormat.Flac => SF_FORMAT_FLAC,
        SoundFileMajorFormat.OggVorbis => SF_FORMAT_OGG,
        SoundFileMajorFormat.Opus => SF_FORMAT_OGG,
        SoundFileMajorFormat.Mp3 => SF_FORMAT_MPEG,
        _ => throw new ArgumentOutOfRangeException(nameof(major))
    };

    private static int SubtypeToNative(SoundFileSubtype subtype) => subtype switch
    {
        SoundFileSubtype.PcmU8 => SF_FORMAT_PCM_U8,
        SoundFileSubtype.PcmS8 => SF_FORMAT_PCM_S8,
        SoundFileSubtype.Pcm16 => SF_FORMAT_PCM_16,
        SoundFileSubtype.Pcm24 => SF_FORMAT_PCM_24,
        SoundFileSubtype.Pcm32 => SF_FORMAT_PCM_32,
        SoundFileSubtype.Float => SF_FORMAT_FLOAT,
        SoundFileSubtype.Double => SF_FORMAT_DOUBLE,
        SoundFileSubtype.Vorbis => SF_FORMAT_VORBIS,
        SoundFileSubtype.Opus => SF_FORMAT_OPUS,
        SoundFileSubtype.Mp3 => SF_FORMAT_MPEG_LAYER_III,
        _ => throw new ArgumentOutOfRangeException(nameof(subtype))
    };

    private static int DefaultSubtypeNative(SoundFileMajorFormat major) => major switch
    {
        SoundFileMajorFormat.OggVorbis => SF_FORMAT_VORBIS,
        SoundFileMajorFormat.Opus => SF_FORMAT_OPUS,
        SoundFileMajorFormat.Mp3 => SF_FORMAT_MPEG_LAYER_III,
        // Lossless containers default to 16-bit PCM — the safe, universal
        // choice; callers wanting float/24-bit set the subtype explicitly.
        _ => SF_FORMAT_PCM_16
    };

    /// <summary>
    /// Combines a major format and subtype into the libsndfile format
    /// bitfield. <see cref="SoundFileSubtype.Default"/> resolves to the
    /// natural subtype for the major format.
    /// </summary>
    public static int BuildFormat(SoundFileMajorFormat major, SoundFileSubtype subtype)
    {
        int nativeSubtype = subtype == SoundFileSubtype.Default
            ? DefaultSubtypeNative(major)
            : SubtypeToNative(subtype);
        return MajorToNative(major) | nativeSubtype;
    }

    /// <summary>Whether the subtype is Opus (rate-restricted by the codec).</summary>
    public static bool IsOpus(int rawFormat)
        => (rawFormat & SndFileInterop.SF_FORMAT_SUBMASK) == SF_FORMAT_OPUS;

    /// <summary>
    /// Whether the container needs a seekable target to finalise (its
    /// header is back-patched at close). FLAC/Ogg/MPEG and headerless RAW
    /// stream fine to a forward-only target; WAV/AIFF/AU/CAF/W64 do not.
    /// </summary>
    public static bool MajorNeedsSeekableStream(int rawFormat)
    {
        int major = rawFormat & SndFileInterop.SF_FORMAT_TYPEMASK;
        return major is SF_FORMAT_WAV or SF_FORMAT_AIFF or SF_FORMAT_AU
            or SF_FORMAT_CAF or SF_FORMAT_W64;
    }

    /// <summary>
    /// Infers a major format from a path's extension. Throws when the
    /// extension is unknown so the caller can fall back to an explicit
    /// <see cref="SoundFileMajorFormat"/>.
    /// </summary>
    public static SoundFileMajorFormat InferMajorFromExtension(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".wav" or ".wave" => SoundFileMajorFormat.Wav,
            ".aif" or ".aiff" or ".aifc" => SoundFileMajorFormat.Aiff,
            ".au" or ".snd" => SoundFileMajorFormat.Au,
            ".caf" => SoundFileMajorFormat.Caf,
            ".w64" => SoundFileMajorFormat.W64,
            ".flac" => SoundFileMajorFormat.Flac,
            ".ogg" or ".oga" => SoundFileMajorFormat.OggVorbis,
            ".opus" => SoundFileMajorFormat.Opus,
            ".mp3" => SoundFileMajorFormat.Mp3,
            _ => throw new ArgumentException(
                $"Cannot infer an audio format from extension '{ext}'. " +
                "Use a constructor that takes an explicit SoundFileMajorFormat.",
                nameof(path))
        };
    }
}
