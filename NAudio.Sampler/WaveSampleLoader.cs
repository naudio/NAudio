using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Loads a WAV file into a mono float buffer for the sampler, down-mixing
    /// multi-channel files. Shared by the SFZ sample loader and the single-sample
    /// instrument. (FLAC/Ogg support will arrive via NAudio.SoundFile.)
    /// </summary>
    public static class WaveSampleLoader
    {
        /// <summary>
        /// Reads a WAV file into a mono float buffer. Returns false if the file
        /// does not exist or contains no samples.
        /// </summary>
        public static bool TryLoad(string path, out float[] data, out int sampleRate)
        {
            data = null;
            sampleRate = 0;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

            using var reader = new WaveFileReader(path);
            return TryLoad(reader, out data, out sampleRate);
        }

        /// <summary>
        /// Reads a WAV stream into a mono float buffer. Returns false if it
        /// contains no samples.
        /// </summary>
        public static bool TryLoad(WaveFileReader reader, out float[] data, out int sampleRate)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

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
