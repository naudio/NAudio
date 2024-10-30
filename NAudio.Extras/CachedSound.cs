using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Used by AudioPlaybackEngine
    /// </summary>
    public class CachedSound
    {
        /// <summary>
        /// Audio data
        /// </summary>
        public float[] AudioData { get; }

        /// <summary>
        /// Format of the audio
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Creates a new CachedSound from a file
        /// </summary>
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
    }
}