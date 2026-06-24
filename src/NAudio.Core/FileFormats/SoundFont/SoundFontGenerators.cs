namespace NAudio.SoundFont;

/// <summary>
/// A resolved set of SoundFont generator values, indexed by
/// <see cref="GeneratorEnum"/>. Instances start from the SoundFont 2.04
/// default values (§8.1.3) and are then modified by zone generators during
/// instrument resolution (see <see cref="SoundFontInstrumentResolver"/>).
///
/// Values are the raw signed generator amounts from the file; the units
/// (cents, timecents, centibels, etc.) are interpreted by the synthesiser
/// (see <c>NAudio.Dsp.SynthMath</c>). Range and index generators
/// (KeyRange, VelocityRange, Instrument, SampleID) are <em>not</em> stored
/// here — they drive zone selection and are surfaced on
/// <see cref="SoundFontRegion"/> instead.
/// </summary>
public sealed class SoundFontGenerators
{
    private static readonly short[] DefaultValues = BuildDefaults();

    private readonly short[] values;

    private SoundFontGenerators(short[] values)
    {
        this.values = values;
    }

    /// <summary>
    /// Creates a generator set initialised to the SoundFont 2.04 default
    /// values — the correct starting point for instrument-level resolution.
    /// </summary>
    public static SoundFontGenerators CreateWithDefaults()
    {
        return new SoundFontGenerators((short[])DefaultValues.Clone());
    }

    /// <summary>
    /// Creates a generator set with every value zero — the correct starting
    /// point for accumulating preset-level offsets, which are added to the
    /// instrument-level values.
    /// </summary>
    public static SoundFontGenerators CreateZeroed()
    {
        return new SoundFontGenerators(new short[DefaultValues.Length]);
    }

    /// <summary>
    /// Gets or sets the raw value of a generator. Out-of-range generator
    /// enumerators read as 0 and ignore writes.
    /// </summary>
    public short this[GeneratorEnum generator]
    {
        get
        {
            int i = (int)generator;
            return (uint)i < (uint)values.Length ? values[i] : (short)0;
        }
        set
        {
            int i = (int)generator;
            if ((uint)i < (uint)values.Length) values[i] = value;
        }
    }

    /// <summary>
    /// The MIDI key that plays the sample at its recorded pitch
    /// (overridingRootKey), or -1 to use the sample header's original pitch.
    /// </summary>
    public int OverridingRootKey => this[GeneratorEnum.OverridingRootKey];

    /// <summary>
    /// A fixed key to use for this region regardless of the played note,
    /// or -1 (the default) to use the played note.
    /// </summary>
    public int KeyNumberOverride => this[GeneratorEnum.KeyNumber];

    /// <summary>
    /// A fixed velocity to use for this region regardless of the played
    /// velocity, or -1 (the default) to use the played velocity.
    /// </summary>
    public int VelocityOverride => this[GeneratorEnum.Velocity];

    /// <summary>
    /// The exclusive (choke) class — notes sharing a non-zero exclusive class
    /// within a preset cut each other off (e.g. hi-hat open/closed). 0 = none.
    /// </summary>
    public int ExclusiveClass => this[GeneratorEnum.ExclusiveClass];

    /// <summary>
    /// The sample loop behaviour for this region.
    /// </summary>
    public SampleMode SampleModes => (SampleMode)(this[GeneratorEnum.SampleModes] & 0x3);

    /// <summary>
    /// Sample-point offset to add to the sample header's start point,
    /// combining the fine and coarse (×32768) start-address generators.
    /// </summary>
    public int StartAddressOffset =>
        this[GeneratorEnum.StartAddressOffset] + 32768 * this[GeneratorEnum.StartAddressCoarseOffset];

    /// <summary>
    /// Sample-point offset to add to the sample header's end point.
    /// </summary>
    public int EndAddressOffset =>
        this[GeneratorEnum.EndAddressOffset] + 32768 * this[GeneratorEnum.EndAddressCoarseOffset];

    /// <summary>
    /// Sample-point offset to add to the sample header's loop start point.
    /// </summary>
    public int StartLoopAddressOffset =>
        this[GeneratorEnum.StartLoopAddressOffset] + 32768 * this[GeneratorEnum.StartLoopAddressCoarseOffset];

    /// <summary>
    /// Sample-point offset to add to the sample header's loop end point.
    /// </summary>
    public int EndLoopAddressOffset =>
        this[GeneratorEnum.EndLoopAddressOffset] + 32768 * this[GeneratorEnum.EndLoopAddressCoarseOffset];

    /// <summary>
    /// Returns a copy of this generator set.
    /// </summary>
    public SoundFontGenerators Clone() => new((short[])values.Clone());

    private static short[] BuildDefaults()
    {
        var d = new short[(int)GeneratorEnum.UnusedEnd + 1];
        d[(int)GeneratorEnum.InitialFilterCutoffFrequency] = 13500;
        d[(int)GeneratorEnum.DelayModulationLFO] = -12000;
        d[(int)GeneratorEnum.DelayVibratoLFO] = -12000;
        d[(int)GeneratorEnum.DelayModulationEnvelope] = -12000;
        d[(int)GeneratorEnum.AttackModulationEnvelope] = -12000;
        d[(int)GeneratorEnum.HoldModulationEnvelope] = -12000;
        d[(int)GeneratorEnum.DecayModulationEnvelope] = -12000;
        d[(int)GeneratorEnum.ReleaseModulationEnvelope] = -12000;
        d[(int)GeneratorEnum.DelayVolumeEnvelope] = -12000;
        d[(int)GeneratorEnum.AttackVolumeEnvelope] = -12000;
        d[(int)GeneratorEnum.HoldVolumeEnvelope] = -12000;
        d[(int)GeneratorEnum.DecayVolumeEnvelope] = -12000;
        d[(int)GeneratorEnum.ReleaseVolumeEnvelope] = -12000;
        d[(int)GeneratorEnum.KeyNumber] = -1;
        d[(int)GeneratorEnum.Velocity] = -1;
        d[(int)GeneratorEnum.ScaleTuning] = 100;
        d[(int)GeneratorEnum.OverridingRootKey] = -1;
        return d;
    }
}
