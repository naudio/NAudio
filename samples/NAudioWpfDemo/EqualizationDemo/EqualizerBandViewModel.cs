using System;
using NAudio.Effects;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.EqualizationDemo;

/// <summary>
/// One slider's worth of the graphic EQ: the gain the user drags, plus the labels that say
/// which frequency it steers and how much boost or cut is currently dialled in.
/// </summary>
internal class EqualizerBandViewModel : ViewModelBase
{
    private readonly EqualizerBand band;
    private readonly Action onGainChanged;

    public EqualizerBandViewModel(EqualizerBand band, Action onGainChanged)
    {
        this.band = band;
        this.onGainChanged = onGainChanged;
    }

    /// <summary>Gain for this band in dB. Writing it retunes the live equaliser.</summary>
    public double Gain
    {
        get => band.GainDb;
        set
        {
            if (band.GainDb == (float)value) return;
            band.GainDb = (float)value;
            OnPropertyChanged(nameof(Gain));
            OnPropertyChanged(nameof(GainText));
            onGainChanged();
        }
    }

    /// <summary>Centre frequency caption under the slider, e.g. "400 Hz" or "9.6 kHz".</summary>
    public string FrequencyText => FormatFrequency(band.Frequency);

    /// <summary>Signed gain readout above the slider, e.g. "+4.5" or "-12.0".</summary>
    public string GainText => $"{band.GainDb:+0.0;-0.0;0.0}";

    private static string FormatFrequency(float hz) =>
        hz >= 1000 ? $"{hz / 1000.0:0.#} kHz" : $"{hz:0} Hz";
}
