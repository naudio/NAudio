namespace NAudio.Sampler
{
    /// <summary>
    /// One peaking-EQ band applied by a voice (SFZ <c>eqN_*</c>): a centre
    /// frequency, a gain in dB, and a Q (derived from the SFZ bandwidth).
    /// </summary>
    internal readonly struct SamplerEqBand
    {
        public SamplerEqBand(float frequencyHz, float gainDb, float q)
        {
            FrequencyHz = frequencyHz;
            GainDb = gainDb;
            Q = q;
        }

        public float FrequencyHz { get; }
        public float GainDb { get; }
        public float Q { get; }
    }
}
