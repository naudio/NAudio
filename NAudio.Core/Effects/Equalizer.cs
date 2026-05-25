using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Multi-band parametric equaliser. Each <see cref="EqualizerBand"/> is an
    /// independent biquad (peaking, shelf, pass, notch, band-pass or all-pass) applied
    /// per channel. Edit the bands and call <see cref="Update"/> to retune — changes
    /// crossfade so they never click, even while audio is flowing.
    /// </summary>
    public class Equalizer : AudioEffect
    {
        private readonly List<EqualizerBand> bands;
        private CrossfadingBiQuadFilter[,] filters;
        private int channels;
        private int crossfadeSamples = 1;

        /// <summary>
        /// Creates an equaliser with the given bands. The list is taken by reference so
        /// editing a band and calling <see cref="Update"/> takes effect.
        /// </summary>
        public Equalizer(params EqualizerBand[] bands)
        {
            ArgumentNullException.ThrowIfNull(bands);
            this.bands = new List<EqualizerBand>(bands);
        }

        /// <summary>
        /// The bands, in processing order.
        /// </summary>
        public IReadOnlyList<EqualizerBand> Bands => bands;

        /// <summary>
        /// Re-applies the current band settings, crossfading from the old response to
        /// the new one. Call after changing any band's properties.
        /// </summary>
        public void Update()
        {
            if (filters == null)
                return;
            for (var band = 0; band < bands.Count; band++)
            {
                for (var ch = 0; ch < channels; ch++)
                {
                    filters[ch, band].ReplaceStandby(CreateFilter(bands[band]));
                    filters[ch, band].BeginCrossfade();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            channels = format.Channels;
            crossfadeSamples = Math.Max(1, format.SampleRate / 100);
            filters = new CrossfadingBiQuadFilter[channels, bands.Count];
            for (var band = 0; band < bands.Count; band++)
            {
                for (var ch = 0; ch < channels; ch++)
                {
                    filters[ch, band] = new CrossfadingBiQuadFilter(
                        CreateFilter(bands[band]), CreateFilter(bands[band]), crossfadeSamples);
                }
            }
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var bandCount = bands.Count;
            if (bandCount == 0)
                return;
            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                for (var ch = 0; ch < channels; ch++)
                {
                    var sample = buffer[i + ch];
                    for (var band = 0; band < bandCount; band++)
                        sample = filters[ch, band].Transform(sample);
                    buffer[i + ch] = sample;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            if (filters == null)
                return;
            for (var band = 0; band < bands.Count; band++)
                for (var ch = 0; ch < channels; ch++)
                    filters[ch, band].Reset();
        }

        private BiQuadFilter CreateFilter(EqualizerBand band)
        {
            var sampleRate = SampleRate;
            var frequency = Clamp(band.Frequency, 1f, sampleRate * 0.5f - 1f);
            var q = band.Q <= 0f ? 0.707f : band.Q;
            var slope = band.ShelfSlope <= 0f ? 1f : band.ShelfSlope;
            return band.Type switch
            {
                EqualizerBandType.LowShelf => BiQuadFilter.LowShelf(sampleRate, frequency, slope, band.GainDb),
                EqualizerBandType.HighShelf => BiQuadFilter.HighShelf(sampleRate, frequency, slope, band.GainDb),
                EqualizerBandType.LowPass => BiQuadFilter.LowPassFilter(sampleRate, frequency, q),
                EqualizerBandType.HighPass => BiQuadFilter.HighPassFilter(sampleRate, frequency, q),
                EqualizerBandType.Notch => BiQuadFilter.NotchFilter(sampleRate, frequency, q),
                EqualizerBandType.BandPass => BiQuadFilter.BandPassFilterConstantPeakGain(sampleRate, frequency, q),
                EqualizerBandType.AllPass => BiQuadFilter.AllPassFilter(sampleRate, frequency, q),
                _ => BiQuadFilter.PeakingEQ(sampleRate, frequency, q, band.GainDb)
            };
        }

        private static float Clamp(float value, float min, float max)
            => value < min ? min : value > max ? max : value;
    }
}
