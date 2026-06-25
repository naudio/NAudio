using System;
using System.IO;
using NAudio.SoundFile;

namespace NAudio.Sampler;

/// <summary>
/// Loads an SFZ region's sample file into a mono float buffer. Pluggable so
/// the format coverage can grow (WAV today; FLAC/Ogg via NAudio.SoundFile
/// later) and so tests can supply synthetic samples.
/// </summary>
public interface ISfzSampleLoader
{
    /// <summary>
    /// Loads the sample at <paramref name="path"/> (as written in the SFZ) into
    /// channel buffers: <paramref name="left"/> is the left/mono channel and
    /// <paramref name="right"/> is the right channel, or null for a mono sample.
    /// <paramref name="embeddedLoop"/> carries loop points authored in the
    /// sample file itself (a WAV <c>smpl</c> chunk), or null if there are none —
    /// per the SFZ spec these are the region's default loop points and make
    /// <c>loop_mode</c> default to <c>loop_continuous</c>.
    /// Returns false if the sample cannot be found or decoded.
    /// </summary>
    bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
        out SampleLoop? embeddedLoop);
}

/// <summary>
/// Loads SFZ samples from disk, resolving relative paths against a base
/// directory and decoding each fully into memory. WAV is read directly;
/// other formats (FLAC, Ogg-Vorbis, Opus, …) decode through
/// <c>NAudio.SoundFile</c> (libsndfile). If libsndfile is unavailable or the
/// file cannot be decoded, the load fails gracefully (the region is skipped).
/// </summary>
public sealed class FileSfzSampleLoader : ISfzSampleLoader
{
    private readonly string baseDirectory;

    /// <summary>Creates a loader rooted at the given base directory.</summary>
    public FileSfzSampleLoader(string baseDirectory)
    {
        this.baseDirectory = baseDirectory ?? "";
    }

    /// <inheritdoc />
    public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
        out SampleLoop? embeddedLoop)
    {
        left = null;
        right = null;
        sampleRate = 0;
        embeddedLoop = null;
        if (string.IsNullOrEmpty(path)) return false;

        var normalised = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar);
        var full = Path.IsPathRooted(normalised) ? normalised : Path.Combine(baseDirectory, normalised);
        if (!File.Exists(full)) return false;

        // WAV reads directly, including smpl-chunk loop points; FLAC/Ogg/Opus/
        // etc. go through libsndfile, which doesn't surface loop instrument
        // data on this path — embedded loops are WAV-only for now
        if (full.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            return WaveSampleLoader.TryLoad(full, out left, out right, out sampleRate, out embeddedLoop);

        try
        {
            using var reader = new SoundFileReader(full);
            return WaveSampleLoader.TryLoad(reader, out left, out right, out sampleRate);
        }
        catch (Exception)
        {
            // libsndfile missing, or an unsupported/corrupt file — skip the region
            return false;
        }
    }
}
