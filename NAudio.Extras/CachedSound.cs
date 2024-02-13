using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Used by AudioPlaybackEngine
    /// </summary>
    public class CachedSound
    {
        public float[] AudioData { get; protected set; }
        public WaveFormat WaveFormat { get; protected set; }
        public CachedSound(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                Init(audioFileReader);
            }
        }

        public CachedSound(Stream sound)
        {

            using (var audioFileReader = new WaveFileReader(sound))
            {
                Init(audioFileReader);
            }
        }

        protected void Init(WaveStream waveStream)
        {
            if (!(waveStream is ISampleProvider sampleProvider))
            {
                sampleProvider = waveStream.ToSampleProvider();
            }

            // TODO: could add resampling in here if required
            WaveFormat = sampleProvider.WaveFormat;
            var wholeFile = new List<float>((int)(waveStream.Length / 4));
            var readBuffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
            int samplesRead;
            while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }
            AudioData = wholeFile.ToArray();

        }

        protected CachedSound()
        {
            // no-op but makes this easier on subclasses
        }
    }
}