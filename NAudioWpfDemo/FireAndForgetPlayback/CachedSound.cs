using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using System.IO;

namespace NAudioWpfDemo.FireAndForgetPlayback
{
    class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }
        public CachedSound(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                // TODO: could add resampling in here if required
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
            }
        }
        public CachedSound(Stream sound) 
        {
            using (var audioFileReader = new WaveFileReader(sound))
            {
                WaveFormat = audioFileReader.WaveFormat;
                var sp = audioFileReader.ToSampleProvider();
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var sourceSamples = (int)(audioFileReader.Length / (audioFileReader.WaveFormat.BitsPerSample / 8));
                var sampleData = new float[sourceSamples];
                int samplesread;
                while ((samplesread = sp.Read(sampleData, 0, sourceSamples)) > 0)
                {
                    wholeFile.AddRange(sampleData.Take(samplesread));
                }
                AudioData = wholeFile.ToArray();                
            }
        }
    }
}
