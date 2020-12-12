using System;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudioWpfDemo.FireAndForgetPlayback
{
    class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound cachedSound;
        private long position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            this.cachedSound = cachedSound;
        }

        public int Read(Span<float> buffer)
        {
            var availableSamples = cachedSound.AudioData.Length - position;
            var samplesToCopy = Math.Min(availableSamples, buffer.Length);
            SpanExtensions.ArrayCopy(cachedSound.AudioData, (int)position, buffer, (int)samplesToCopy);
            position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat => cachedSound.WaveFormat;
    }
}