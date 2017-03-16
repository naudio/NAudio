using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Takes a stereo input and turns it to mono
    /// </summary>
    public class StereoToMonoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private float[] sourceBuffer;

        /// <summary>
        /// Creates a new mono ISampleProvider based on a stereo input
        /// </summary>
        /// <param name="sourceProvider">Stereo 16 bit PCM input</param>
        public StereoToMonoSampleProvider(ISampleProvider sourceProvider)
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
        /// Reads bytes from this SampleProvider
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            var sourceSamplesRequired = count * 2;
            if (sourceBuffer == null || sourceBuffer.Length < sourceSamplesRequired) sourceBuffer = new float[sourceSamplesRequired];

            var sourceSamplesRead = sourceProvider.Read(sourceBuffer, 0, sourceSamplesRequired);
            var destOffset = offset / 2;
            for (var sourceSample = 0; sourceSample < sourceSamplesRead; sourceSample += 2)
            {
                var left = sourceBuffer[sourceSample];
                var right = sourceBuffer[sourceSample + 1];
                var outSample = (left * LeftVolume) + (right * RightVolume);

                buffer[destOffset++] = outSample;
            }
            return sourceSamplesRead / 2;
        }
    }
}