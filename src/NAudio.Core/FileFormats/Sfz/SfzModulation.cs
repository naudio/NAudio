namespace NAudio.Sfz;

/// <summary>
/// One per-region SFZ LFO (<c>amplfo_*</c>, <c>fillfo_*</c> or
/// <c>pitchlfo_*</c>): a rate in Hz, a modulation depth (dB for the
/// amplitude LFO, cents for the filter and pitch LFOs) and an onset delay
/// in seconds. All values default to 0.
/// </summary>
public readonly struct SfzLfo
{
    /// <summary>Creates the settings for one LFO.</summary>
    public SfzLfo(float frequencyHz, float depth, float delaySeconds)
    {
        FrequencyHz = frequencyHz;
        Depth = depth;
        DelaySeconds = delaySeconds;
    }

    /// <summary>LFO rate in Hz (<c>amplfo_freq</c>/<c>fillfo_freq</c>/<c>pitchlfo_freq</c>, default 0).</summary>
    public float FrequencyHz { get; }
    /// <summary>Modulation depth (<c>amplfo_depth</c> in dB; <c>fillfo_depth</c>/<c>pitchlfo_depth</c> in cents; default 0).</summary>
    public float Depth { get; }
    /// <summary>Delay before the LFO starts, in seconds (<c>amplfo_delay</c>/<c>fillfo_delay</c>/<c>pitchlfo_delay</c>, default 0).</summary>
    public float DelaySeconds { get; }

    /// <summary>Whether the LFO modulates anything: a positive rate and a non-zero depth.</summary>
    public bool IsActive => FrequencyHz > 0 && Depth != 0;
}

/// <summary>
/// One per-region SFZ modulation envelope (<c>fileg_*</c> or
/// <c>pitcheg_*</c>): a DAHDSR shape in seconds (sustain as a percentage,
/// default 100) and a modulation depth in cents. An envelope with zero
/// depth modulates nothing.
/// </summary>
public readonly struct SfzModulationEnvelope
{
    /// <summary>Creates the settings for one modulation envelope.</summary>
    public SfzModulationEnvelope(float delaySeconds, float attackSeconds, float holdSeconds,
        float decaySeconds, float sustainPercent, float releaseSeconds, float depthCents)
    {
        DelaySeconds = delaySeconds;
        AttackSeconds = attackSeconds;
        HoldSeconds = holdSeconds;
        DecaySeconds = decaySeconds;
        SustainPercent = sustainPercent;
        ReleaseSeconds = releaseSeconds;
        DepthCents = depthCents;
    }

    /// <summary>Envelope delay in seconds (<c>*eg_delay</c>, default 0).</summary>
    public float DelaySeconds { get; }
    /// <summary>Envelope attack in seconds (<c>*eg_attack</c>, default 0).</summary>
    public float AttackSeconds { get; }
    /// <summary>Envelope hold in seconds (<c>*eg_hold</c>, default 0).</summary>
    public float HoldSeconds { get; }
    /// <summary>Envelope decay in seconds (<c>*eg_decay</c>, default 0).</summary>
    public float DecaySeconds { get; }
    /// <summary>Envelope sustain level as a percentage (<c>*eg_sustain</c>, default 100).</summary>
    public float SustainPercent { get; }
    /// <summary>Envelope release in seconds (<c>*eg_release</c>, default 0).</summary>
    public float ReleaseSeconds { get; }
    /// <summary>Modulation depth in cents (<c>*eg_depth</c>, default 0).</summary>
    public float DepthCents { get; }

    /// <summary>Whether the envelope modulates anything: a non-zero depth.</summary>
    public bool IsActive => DepthCents != 0;
}
