using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

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
        /// Loads the sample at <paramref name="path"/> (as written in the SFZ).
        /// Returns false if it cannot be found or decoded.
        /// </summary>
        bool TryLoad(string path, out float[] data, out int sampleRate);
    }

    /// <summary>
    /// Loads SFZ samples from disk, resolving relative paths against a base
    /// directory. Currently decodes WAV (via <see cref="WaveFileReader"/>),
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
        public bool TryLoad(string path, out float[] data, out int sampleRate)
        {
            data = null;
            sampleRate = 0;
            if (string.IsNullOrEmpty(path)) return false;

            var normalised = path.Replace('\\', Path.DirectorySeparatorChar)
                                 .Replace('/', Path.DirectorySeparatorChar);
            var full = Path.IsPathRooted(normalised) ? normalised : Path.Combine(baseDirectory, normalised);
            if (!File.Exists(full)) return false;

            // WAV only for now; an unknown/undecodable file falls through to false
            if (!full.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase)) return false;

            using var reader = new WaveFileReader(full);
            sampleRate = reader.WaveFormat.SampleRate;
            int channels = reader.WaveFormat.Channels;
            var sampleProvider = reader.ToSampleProvider();

            var samples = new List<float>((int)(reader.Length / 2));
            var buffer = new float[8192];
            int read;
            while ((read = sampleProvider.Read(buffer.AsSpan(0, buffer.Length))) > 0)
                for (int i = 0; i < read; i++) samples.Add(buffer[i]);

            data = channels <= 1 ? samples.ToArray() : DownmixToMono(samples, channels);
            return data.Length > 0;
        }

        private static float[] DownmixToMono(List<float> interleaved, int channels)
        {
            int frames = interleaved.Count / channels;
            var mono = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++) sum += interleaved[f * channels + c];
                mono[f] = sum / channels;
            }
            return mono;
        }
    }
}
