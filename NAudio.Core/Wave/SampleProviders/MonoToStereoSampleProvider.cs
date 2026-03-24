using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// No nonsense mono to stereo provider, no volume adjustment,
    /// just copies input to left and right.
    /// </summary>
    public class MonoToStereoSampleProvider : ISampleSource
    {
        private readonly ISampleSource source;
        private float[] sourceBuffer;

        /// <summary>
        /// Initializes a new instance of MonoToStereoSampleProvider
        /// </summary>
        /// <param name="source">Source sample source</param>
        public MonoToStereoSampleProvider(ISampleSource source)
        {
            LeftVolume = 1.0f;
            RightVolume = 1.0f;
            if (source.WaveFormat.Channels != 1)
            {
                throw new ArgumentException("Source must be mono");
            }
            this.source = source;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
        }

        /// <summary>
        /// WaveFormat of this provider
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads samples from this provider into a span
        /// </summary>
        public int Read(Span<float> buffer)
        {
            var sourceSamplesRequired = buffer.Length / 2;
            EnsureSourceBuffer(sourceSamplesRequired);
            var sourceSamplesRead = source.Read(sourceBuffer.AsSpan(0, sourceSamplesRequired));
            int outIndex = 0;
            for (var n = 0; n < sourceSamplesRead; n++)
            {
                buffer[outIndex++] = sourceBuffer[n] * LeftVolume;
                buffer[outIndex++] = sourceBuffer[n] * RightVolume;
            }
            return sourceSamplesRead * 2;
        }

        /// <summary>
        /// Multiplier for left channel (default is 1.0)
        /// </summary>
        public float LeftVolume { get; set; }

        /// <summary>
        /// Multiplier for right channel (default is 1.0)
        /// </summary>
        public float RightVolume { get; set; }

        private void EnsureSourceBuffer(int count)
        {
            if (sourceBuffer == null || sourceBuffer.Length < count)
            {
                sourceBuffer = new float[count];
            }
        }
    }
}
