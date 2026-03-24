using System;
using NAudio.Wave;

namespace NAudio.Extras
{
    class CachedSoundSampleProvider : ISampleSource
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
            cachedSound.AudioData.AsSpan((int)position, (int)samplesToCopy).CopyTo(buffer);
            position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat => cachedSound.WaveFormat;
    }
}
