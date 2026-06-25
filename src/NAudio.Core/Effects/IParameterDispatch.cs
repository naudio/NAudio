namespace NAudio.Effects;

/// <summary>
/// A sink that marshals <see cref="EffectParameter"/> writes onto the audio
/// thread. While an effect's parameters are routed through a dispatch, setting
/// <see cref="EffectParameter.Value"/> from another thread does not mutate the
/// effect inline; the (already clamped) write is posted here and applied later,
/// at a block boundary, on the thread that runs the effect — so coefficient
/// recomputation and buffer resizes never race the audio callback. See
/// <see cref="ParameterDispatchQueue"/>.
/// </summary>
public interface IParameterDispatch
{
    /// <summary>
    /// Posts a clamped parameter write to be applied later on the audio thread.
    /// Must be safe to call from a non-audio thread and must not block.
    /// </summary>
    void Post(EffectParameter parameter, float value);
}
