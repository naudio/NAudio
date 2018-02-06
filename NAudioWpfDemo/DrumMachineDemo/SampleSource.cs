using System;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class SampleSource
    {
        public static SampleSource CreateFromWaveFile(string fileName)
        {
            using (var reader = new WaveFileReader(fileName))
            {
                var sp = reader.ToSampleProvider();
                var sourceSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
                var sampleData = new float[sourceSamples];
                int n = sp.Read(sampleData, 0, sourceSamples);
                if (n != sourceSamples)
                {
                    throw new InvalidOperationException(
                        $"Couldn't read the whole sample, expected {n} samples, got {sourceSamples}");
                }
                var ss = new SampleSource(sampleData, sp.WaveFormat);
                return ss;
            }
        }

        public SampleSource(float[] sampleData, WaveFormat waveFormat) :
            this(sampleData, waveFormat, 0, sampleData.Length)
        {
        }

        public SampleSource(float[] sampleData, WaveFormat waveFormat, int startIndex, int length)
        {
            SampleData = sampleData;
            SampleWaveFormat = waveFormat;
            StartIndex = startIndex;
            Length = length;
        }

        /// <summary>
        /// Sample data
        /// </summary>
        public float[] SampleData { get; }
        /// <summary>
        /// Format of sampleData
        /// </summary>
        public WaveFormat SampleWaveFormat { get; }
        /// <summary>
        /// Index of the first sample to play
        /// </summary>
        public int StartIndex { get; }
        /// <summary>
        /// Number of valid samples
        /// </summary>
        public int Length { get; }
    }
}
