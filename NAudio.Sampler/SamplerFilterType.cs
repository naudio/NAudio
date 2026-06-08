namespace NAudio.Sampler
{
    /// <summary>
    /// The resonant filter shape a region uses. SoundFont is always
    /// <see cref="LowPass"/>; SFZ selects it via <c>fil_type</c>.
    /// </summary>
    public enum SamplerFilterType
    {
        /// <summary>Low-pass (the SoundFont default and SFZ <c>lpf_*</c>).</summary>
        LowPass,
        /// <summary>High-pass (SFZ <c>hpf_*</c>).</summary>
        HighPass,
        /// <summary>Band-pass (SFZ <c>bpf_*</c>).</summary>
        BandPass
    }
}
