namespace NAudio.Effects;

/// <summary>
/// The filter shape an <see cref="EqualizerBand"/> applies.
/// </summary>
public enum EqualizerBandType
{
    /// <summary>Peaking (bell) boost or cut centred on the band frequency.</summary>
    Peaking,
    /// <summary>Low shelf: boosts or cuts everything below the frequency.</summary>
    LowShelf,
    /// <summary>High shelf: boosts or cuts everything above the frequency.</summary>
    HighShelf,
    /// <summary>Low-pass: attenuates content above the frequency.</summary>
    LowPass,
    /// <summary>High-pass: attenuates content below the frequency.</summary>
    HighPass,
    /// <summary>Notch: deep, narrow cut at the frequency.</summary>
    Notch,
    /// <summary>Band-pass with 0 dB peak gain at the frequency.</summary>
    BandPass,
    /// <summary>All-pass: flat magnitude, frequency-dependent phase shift.</summary>
    AllPass
}
