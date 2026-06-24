using System;
using System.Collections.Generic;

namespace NAudio.Effects;

/// <summary>
/// The kind of control an <see cref="EffectParameter"/> represents.
/// </summary>
public enum EffectParameterKind
{
    /// <summary>A continuous numeric value (knob/slider) between Minimum and Maximum.</summary>
    Continuous,
    /// <summary>An on/off value (Value is 0 or 1).</summary>
    Toggle,
    /// <summary>A one-of-N selection (Value is the index into <see cref="EffectParameter.Choices"/>).</summary>
    Choice,
    /// <summary>A read-only output value for display only (e.g. gain reduction).</summary>
    Meter
}

/// <summary>
/// A thin, delegate-backed facade over one of an effect's existing typed properties,
/// describing it enough to drive a generic UI (or a VST3-style host, presets,
/// automation). It does not store state — get/set forward to the effect — so the
/// effect's property remains the single source of truth and
/// <see cref="IAudioEffect"/> is unaffected. Build these with the static factory
/// methods.
/// </summary>
public sealed class EffectParameter
{
    private readonly Func<float> getter;
    private readonly Action<float> setter; // null ⇒ read-only (Meter)
    private volatile IParameterDispatch dispatch;
    private float pendingValue;
    private bool hasPending;

    private EffectParameter(string name, EffectParameterKind kind, string unit,
        float minimum, float maximum, IReadOnlyList<string> choices,
        Func<float> getter, Action<float> setter)
    {
        Name = name;
        Kind = kind;
        Unit = unit ?? string.Empty;
        Minimum = minimum;
        Maximum = maximum;
        Choices = choices;
        this.getter = getter;
        this.setter = setter;
        DefaultValue = getter();
    }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>The control kind.</summary>
    public EffectParameterKind Kind { get; }

    /// <summary>Unit string for display (e.g. "dB", "Hz", "ms"); empty if none.</summary>
    public string Unit { get; }

    /// <summary>Minimum value (also lower display bound for a meter).</summary>
    public float Minimum { get; }

    /// <summary>Maximum value (also upper display bound for a meter).</summary>
    public float Maximum { get; }

    /// <summary>The value at the time the parameter was created.</summary>
    public float DefaultValue { get; }

    /// <summary>Choice labels for <see cref="EffectParameterKind.Choice"/>; otherwise null.</summary>
    public IReadOnlyList<string> Choices { get; }

    /// <summary>True for a <see cref="EffectParameterKind.Meter"/> (set is ignored).</summary>
    public bool IsReadOnly => setter == null;

    /// <summary>
    /// The live value, read from / written to the underlying effect property.
    /// Writes are clamped to [<see cref="Minimum"/>, <see cref="Maximum"/>]; writes
    /// to a meter are ignored. If a dispatch has been attached (the effect is
    /// running behind an audio thread) the clamped write is deferred and applied
    /// at the next block boundary on the audio thread; the getter then returns
    /// that just-requested value (optimistically) so a UI bound two-way does not
    /// snap back before the audio thread has applied it. Otherwise it is applied
    /// inline. See <see cref="ParameterDispatchQueue"/>.
    /// </summary>
    public float Value
    {
        get => dispatch != null && hasPending ? pendingValue : getter();
        set
        {
            if (setter == null)
                return;
            var v = value < Minimum ? Minimum : value > Maximum ? Maximum : value;
            var d = dispatch;
            if (d != null)
            {
                pendingValue = v;
                hasPending = true;
                d.Post(this, v);
            }
            else
            {
                hasPending = false;
                setter(v);
            }
        }
    }

    /// <summary>
    /// Routes writes through <paramref name="value"/> (deferred to the audio
    /// thread) or, when null, restores inline application. Internal: only a
    /// <see cref="ParameterDispatchQueue"/> manages this. Clears any optimistic
    /// pending value so the getter reflects the effect's real state until the
    /// next edit.
    /// </summary>
    internal void SetDispatch(IParameterDispatch value)
    {
        hasPending = false;
        dispatch = value;
    }

    /// <summary>
    /// Applies an already-clamped value directly to the effect. Called on the
    /// audio thread by <see cref="ParameterDispatchQueue.Drain"/>.
    /// </summary>
    internal void ApplyDeferred(float value) => setter?.Invoke(value);

    /// <summary>Creates a continuous numeric parameter.</summary>
    public static EffectParameter Continuous(string name, string unit, float minimum,
        float maximum, Func<float> getter, Action<float> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        return new EffectParameter(name, EffectParameterKind.Continuous, unit, minimum,
            maximum, null, getter, setter);
    }

    /// <summary>Creates an on/off parameter.</summary>
    public static EffectParameter Toggle(string name, Func<bool> getter, Action<bool> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        return new EffectParameter(name, EffectParameterKind.Toggle, string.Empty, 0f, 1f,
            null, () => getter() ? 1f : 0f, v => setter(v >= 0.5f));
    }

    /// <summary>Creates a one-of-N choice parameter (Value is the index).</summary>
    public static EffectParameter Choice(string name, IReadOnlyList<string> choices,
        Func<int> getter, Action<int> setter)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        if (choices.Count < 1)
            throw new ArgumentException("At least one choice is required.", nameof(choices));
        return new EffectParameter(name, EffectParameterKind.Choice, string.Empty, 0f,
            choices.Count - 1, choices, () => getter(),
            v => setter((int)MathF.Round(v)));
    }

    /// <summary>Creates a read-only meter parameter (e.g. live gain reduction).</summary>
    public static EffectParameter Meter(string name, string unit, float minimum,
        float maximum, Func<float> getter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        return new EffectParameter(name, EffectParameterKind.Meter, unit, minimum,
            maximum, null, getter, null);
    }
}
