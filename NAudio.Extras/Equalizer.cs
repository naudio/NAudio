using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Extras
{
    /// <summary>
    /// Basic example of a multi-band eq
    /// uses the same settings for both channels in stereo audio
    /// Call Update after you've updated the bands
    /// Potentially to be added to NAudio in a future version
    /// </summary>
    public class Equalizer : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly EqualizerBand[] bands;
        private readonly BiQuadFilter[,] filters;
        private readonly int channels;
        private readonly int bandCount;
        private bool updated;

        /// <summary>
        /// Creates a new Equalizer
        /// </summary>
        public Equalizer(ISampleProvider sourceProvider, EqualizerBand[] bands)
        {
            this.sourceProvider = sourceProvider;
            this.bands = bands;
            channels = sourceProvider.WaveFormat.Channels;
            bandCount = bands.Length;
            filters = new BiQuadFilter[channels,bands.Length];
            CreateFilters();
        }

        private void CreateFilters()
        {
            var sampleRate = sourceProvider.WaveFormat.SampleRate;
            var nyquist = sampleRate / 2.0f;
            for (int bandIndex = 0; bandIndex < bandCount; bandIndex++)
            {
                var band = bands[bandIndex];
                // Bands outside (0, Nyquist) or with non-positive bandwidth would make BiQuadFilter
                // throw or go unstable; skip them so a single out-of-range band doesn't kill playback.
                var bandUsable = band.Frequency > 0 && band.Frequency < nyquist && band.Bandwidth > 0;
                for (int n = 0; n < channels; n++)
                {
                    if (!bandUsable)
                    {
                        filters[n, bandIndex] = null;
                        continue;
                    }
                    if (filters[n, bandIndex] == null)
                        filters[n, bandIndex] = BiQuadFilter.PeakingEQ(sampleRate, band.Frequency, band.Bandwidth, band.Gain);
                    else
                        filters[n, bandIndex].SetPeakingEq(sampleRate, band.Frequency, band.Bandwidth, band.Gain);
                }
            }
        }

        /// <summary>
        /// Update the equalizer settings
        /// </summary>
        public void Update()
        {
            updated = true;
            CreateFilters();
        }

        /// <summary>
        /// Gets the WaveFormat of this Sample Provider
        /// </summary>
        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        /// <summary>
        /// Reads samples from this Sample Provider
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);

            if (updated)
            {
                CreateFilters();
                updated = false;
            }

            for (int n = 0; n < samplesRead; n++)
            {
                int ch = n % channels; 
                
                for (int band = 0; band < bandCount; band++)
                {
                    var filter = filters[ch, band];
                    if (filter != null)
                        buffer[offset + n] = filter.Transform(buffer[offset + n]);
                }
            }
            return samplesRead;
        }
    }
}