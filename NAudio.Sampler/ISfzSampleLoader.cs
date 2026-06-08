using System;
using System.IO;

namespace NAudio.Sampler
{
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
        /// Returns false if it cannot be found or decoded.
        /// </summary>
        bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate);
    }

    /// <summary>
    /// Loads SFZ samples from disk, resolving relative paths against a base
    /// directory. Currently decodes WAV (via <see cref="WaveSampleLoader"/>),
    /// down-mixing multi-channel files to mono; other formats return false until
    /// a decoder is wired in.
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
        public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate)
        {
            left = null;
            right = null;
            sampleRate = 0;
            if (string.IsNullOrEmpty(path)) return false;

            var normalised = path.Replace('\\', Path.DirectorySeparatorChar)
                                 .Replace('/', Path.DirectorySeparatorChar);
            var full = Path.IsPathRooted(normalised) ? normalised : Path.Combine(baseDirectory, normalised);

            // WAV only for now; an unknown/undecodable file falls through to false
            if (!full.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) return false;

            return WaveSampleLoader.TryLoad(full, out left, out right, out sampleRate);
        }
    }
}
