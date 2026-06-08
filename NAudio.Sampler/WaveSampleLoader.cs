using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Loads a WAV file into mono or stereo float channels for the sampler.
    /// Shared by the SFZ sample loader and the single-sample instrument.
    /// (FLAC/Ogg support will arrive via NAudio.SoundFile.)
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
            return TryLoad(reader, out left, out right, out sampleRate);
        }

        /// <summary>
        /// Reads a WAV stream into channel buffers (see the path overload).
        /// </summary>
        public static bool TryLoad(WaveFileReader reader, out float[] left, out float[] right, out int sampleRate)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            sampleRate = reader.WaveFormat.SampleRate;
            int channels = reader.WaveFormat.Channels;
            var sampleProvider = reader.ToSampleProvider();

            var interleaved = new List<float>((int)(reader.Length / 2));
            var buffer = new float[8192];
            int read;
            while ((read = sampleProvider.Read(buffer.AsSpan(0, buffer.Length))) > 0)
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
