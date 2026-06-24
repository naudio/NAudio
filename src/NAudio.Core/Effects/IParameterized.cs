using System.Collections.Generic;

namespace NAudio.Effects;

/// <summary>
/// Optional capability an effect implements to expose its tweakable controls as a
/// uniform list, so a generic UI (the real-time effects harness, a future VST3-style
/// host), presets, serialisation or automation can drive any effect without
/// effect-specific code. This is purely additive — it does not change
/// <see cref="IAudioEffect"/> or the real-time processing contract. <c>Bypass</c>
/// and <c>Mix</c> live on <see cref="AudioEffect"/> and are surfaced generically, so
/// implementations should not repeat them here.
/// </summary>
public interface IParameterized
{
    /// <summary>The effect's parameters, in display order.</summary>
    IReadOnlyList<EffectParameter> Parameters { get; }
}
