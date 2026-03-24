using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Takes a stereo input and turns it to mono
    /// </summary>
    public class StereoToMonoSampleProvider : ISampleSource
    {
        private readonly ISampleSource sourceProvider;
        private float[] sourceBuffer;

        /// <summary>
        /// Creates a new mono ISampleSource based on a stereo input
        /// </summary>
        /// <param name="sourceProvider">Stereo input source</param>
        public StereoToMonoSampleProvider(ISampleSource sourceProvider)
        {
            LeftVolume = 0.5f;
            RightVolume = 0.5f;
            if (sourceProvider.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must be stereo");
            }
            this.sourceProvider = sourceProvider;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, 1);
        }

        /// <summary>
        /// 1.0 to mix the mono source entirely to the left channel
        /// </summary>
        public float LeftVolume { get; set; }

        /// <summary>
        /// 1.0 to mix the mono source entirely to the right channel
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Output Wave Format
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads samples from this provider into a span
        /// </summary>
        public int Read(Span<float> buffer)
        {
            var sourceSamplesRequired = buffer.Length * 2;
            if (sourceBuffer == null || sourceBuffer.Length < sourceSamplesRequired) sourceBuffer = new float[sourceSamplesRequired];

            var sourceSamplesRead = sourceProvider.Read(sourceBuffer.AsSpan(0, sourceSamplesRequired));
            int destIndex = 0;
            for (var sourceSample = 0; sourceSample < sourceSamplesRead; sourceSample += 2)
            {
                var left = sourceBuffer[sourceSample];
                var right = sourceBuffer[sourceSample + 1];
                buffer[destIndex++] = (left * LeftVolume) + (right * RightVolume);
            }
            return sourceSamplesRead / 2;
        }
    }
}
