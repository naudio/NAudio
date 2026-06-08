using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Decodes an audio file (or any <see cref="ISampleProvider"/>) fully into
    /// mono/stereo float channel buffers for the sampler — the in-memory,
    /// random-access form the voice engine plays from. WAV is read directly;
    /// other formats decode through any provided <see cref="ISampleProvider"/>
    /// (e.g. <c>NAudio.SoundFile.SoundFileReader</c> for FLAC/Ogg). Shared by the
    /// SFZ sample loader and the single-sample instrument.
    /// </summary>
    public static class WaveSampleLoader
    {
        /// <summary>
        /// Reads a WAV file into channel buffers: <paramref name="left"/> is the
        /// left/mono channel and <paramref name="right"/> is the right channel, or
        /// null for a mono file. Channels beyond the first two are ignored.
        /// Returns false if the file is missing or empty.
        /// </summary>
        public static bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate)
        {
            left = null;
            right = null;
            sampleRate = 0;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

            using var reader = new WaveFileReader(path);
            return TryLoad(reader.ToSampleProvider(), out left, out right, out sampleRate);
        }

        /// <summary>
        /// Reads an entire sample provider into channel buffers (left/mono and an
        /// optional right channel), decoding it fully into memory. Returns false if
        /// it yields no samples.
        /// </summary>
        public static bool TryLoad(ISampleProvider source, out float[] left, out float[] right, out int sampleRate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            sampleRate = source.WaveFormat.SampleRate;
            int channels = source.WaveFormat.Channels;

            var interleaved = new List<float>();
            var buffer = new float[8192];
            int read;
            while ((read = source.Read(buffer.AsSpan(0, buffer.Length))) > 0)
                for (int i = 0; i < read; i++) interleaved.Add(buffer[i]);

            int frames = channels > 0 ? interleaved.Count / channels : 0;
            left = null;
            right = null;
            if (frames == 0) return false;

            left = ExtractChannel(interleaved, channels, 0, frames);
            if (channels >= 2) right = ExtractChannel(interleaved, channels, 1, frames);
            return true;
        }

        private static float[] ExtractChannel(List<float> interleaved, int channels, int channel, int frames)
        {
            var data = new float[frames];
            for (int f = 0; f < frames; f++) data[f] = interleaved[f * channels + channel];
            return data;
        }
    }
}
