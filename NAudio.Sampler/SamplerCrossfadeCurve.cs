namespace NAudio.Sampler
{
    /// <summary>
    /// The curve a key/velocity crossfade uses to turn a fade position (0..1) into
    /// a gain (SFZ <c>xf_keycurve</c>/<c>xf_velcurve</c>).
    /// </summary>
    public enum SamplerCrossfadeCurve
    {
        /// <summary>Linear: gain = position (SFZ <c>gain</c>).</summary>
        Linear,
        /// <summary>Equal-power: gain = sqrt(position), so two crossfading layers sum to constant power (SFZ <c>power</c>).</summary>
        Power
    }
}
