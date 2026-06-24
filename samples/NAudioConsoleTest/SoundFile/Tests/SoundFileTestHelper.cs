using NAudio.SoundFile;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.SoundFile.Tests;

/// <summary>
/// Shared plumbing for the <c>SoundFile.*</c> tests: the libsndfile presence
/// probe (so every test degrades to a clear <see cref="TestOutcome.Skipped"/>
/// rather than crashing when the native binary is missing), codec-availability
/// gating, the format/extension maps, and a finite sine pump for the
/// self-contained round-trip tests.
/// </summary>
internal static class SoundFileTestHelper
{
    public static IReadOnlyList<string> FormatChoices { get; } =
        ["Wav", "Aiff", "Flac", "OggVorbis", "Opus", "Mp3"];

    public static SoundFileMajorFormat ToMajor(string name) => name switch
    {
        "Wav" => SoundFileMajorFormat.Wav,
        "Aiff" => SoundFileMajorFormat.Aiff,
        "Flac" => SoundFileMajorFormat.Flac,
        "OggVorbis" => SoundFileMajorFormat.OggVorbis,
        "Opus" => SoundFileMajorFormat.Opus,
        "Mp3" => SoundFileMajorFormat.Mp3,
        _ => throw new ArgumentException($"Unknown format '{name}'", nameof(name))
    };

    public static string ExtensionFor(SoundFileMajorFormat major) => major switch
    {
        SoundFileMajorFormat.Wav => ".wav",
        SoundFileMajorFormat.Aiff => ".aiff",
        SoundFileMajorFormat.Flac => ".flac",
        SoundFileMajorFormat.OggVorbis => ".ogg",
        SoundFileMajorFormat.Opus => ".opus",
        SoundFileMajorFormat.Mp3 => ".mp3",
        _ => ".bin"
    };

    /// <summary>
    /// Probes for a usable libsndfile. Returns a <see cref="TestOutcome.Skipped"/>
    /// result (with an actionable install message) when the native library is
    /// absent, or <c>null</c> when it loaded fine.
    /// </summary>
    public static TestResult? ProbeLibrary()
    {
        try
        {
            _ = SoundFileCapabilities.GetSupportedMajorFormats();
            return null;
        }
        catch (DllNotFoundException)
        {
            return TestResult.Skipped(
                "libsndfile not found. Install it (Debian/Ubuntu: 'sudo apt install libsndfile1', " +
                "macOS: 'brew install libsndfile') or drop sndfile.dll / libsndfile-1.dll next to " +
                "the executable. See Docs/CrossPlatformAudioFilesWithSoundFile.md.");
        }
    }

    /// <summary>
    /// Returns a <see cref="TestOutcome.Skipped"/> result when the installed
    /// libsndfile build lacks the requested codec, or <c>null</c> when it is
    /// available.
    /// </summary>
    public static TestResult? RequireCodec(SoundFileMajorFormat major)
        => SoundFileCapabilities.IsFormatSupported(major)
            ? null
            : TestResult.Skipped(
                $"This libsndfile build ({SoundFileCapabilities.LibraryVersion}) has no {major} support.");

    /// <summary>
    /// Builds writer options for a quality value (0..1). FLAC maps it to
    /// compression level; the lossy codecs map it to VBR quality. Returns
    /// <c>null</c> when no quality was requested.
    /// </summary>
    public static SoundFileWriterOptions? OptionsFor(SoundFileMajorFormat major, double? quality)
    {
        if (quality is not double q)
        {
            return null;
        }
        var options = new SoundFileWriterOptions();
        if (major == SoundFileMajorFormat.Flac)
        {
            options.CompressionLevel = q;
        }
        else
        {
            options.VbrQuality = q;
        }
        return options;
    }

    /// <summary>Writes <paramref name="seconds"/> of 440 Hz float sine into a writer.</summary>
    public static void PumpSine(SineWaveSource sine, SoundFileWriter writer, int seconds)
    {
        var format = sine.WaveFormat;
        var buffer = new byte[format.AverageBytesPerSecond];
        long target = (long)seconds * format.AverageBytesPerSecond;
        long written = 0;
        while (written < target)
        {
            int want = (int)Math.Min(buffer.Length, target - written);
            sine.Read(buffer.AsSpan(0, want));
            writer.Write(buffer.AsSpan(0, want));
            written += want;
        }
    }

    /// <summary>Decodes a reader to the end and returns (frames, RMS).</summary>
    public static (long frames, double rms) DrainAndMeasure(SoundFileReader reader)
    {
        var buffer = new float[reader.WaveFormat.Channels * 8192];
        long count = 0;
        double sumSquares = 0;
        int n;
        while ((n = reader.Read(buffer)) > 0)
        {
            for (int i = 0; i < n; i++)
            {
                sumSquares += (double)buffer[i] * buffer[i];
            }
            count += n;
        }
        double rms = count == 0 ? 0 : Math.Sqrt(sumSquares / count);
        return (count / reader.WaveFormat.Channels, rms);
    }
}
