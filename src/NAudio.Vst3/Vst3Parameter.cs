namespace NAudio.Vst3;

/// <summary>
/// A single VST 3® parameter exposed by the plug-in's <see cref="Vst3Plugin.Parameters"/>
/// collection. Static metadata (id, title, units, step count) is captured at construction;
/// the value getters / setters round-trip through the plug-in's <c>IEditController</c> on every
/// call.
/// </summary>
/// <remarks>
/// <para>
/// VST 3 parameters live in two value spaces that the plug-in maps between non-linearly:
/// <list type="bullet">
/// <item><description><see cref="NormalizedValue"/> — always <c>[0.0, 1.0]</c>, uniform across
/// every parameter. This is what automation lanes record and what the plug-in stores
/// internally.</description></item>
/// <item><description><see cref="PlainValue"/> / <see cref="SetPlainValue"/> — the value in the
/// plug-in's chosen units (Hz, dB, %, etc.). The mapping is owned by the plug-in; the only
/// authoritative conversion is via <c>IEditController</c>, which this property delegates to.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// For UI display use <see cref="DisplayValue"/> or <see cref="FormatValue"/> — the plug-in
/// formats the value with its own units (<c>"8.4 kHz"</c>, <c>"+3.2 dB"</c>) and that's almost
/// always more useful than the raw <see cref="PlainValue"/> double.
/// </para>
/// <para>
/// <b>Threading:</b> this API is single-threaded — drive it from one thread (typically the UI
/// thread), as the VST 3 edit-controller contract requires. A write updates the controller
/// immediately (so a subsequent read reflects it) and is also queued for the plug-in's DSP at
/// offset 0 of the next <see cref="Vst3Plugin.Process"/> block via <c>IParameterChanges</c>.
/// </para>
/// </remarks>
public sealed class Vst3Parameter
{
    private readonly Vst3Plugin _plugin;

    internal Vst3Parameter(
        Vst3Plugin plugin,
        uint id,
        string title,
        string shortTitle,
        string units,
        int stepCount,
        double defaultNormalizedValue,
        int unitId,
        Vst3ParameterFlags flags)
    {
        _plugin = plugin;
        Id = id;
        Title = title;
        ShortTitle = shortTitle;
        Units = units;
        StepCount = stepCount;
        DefaultNormalizedValue = defaultNormalizedValue;
        UnitId = unitId;
        Flags = flags;
    }

    /// <summary>The plug-in's unique identifier for this parameter (<c>Vst::ParamID</c>).</summary>
    public uint Id { get; }

    /// <summary>UTF-16 title displayed in a host parameter list (e.g. <c>"Cutoff"</c>).</summary>
    public string Title { get; }

    /// <summary>Short title for tight UI surfaces; falls back to <see cref="Title"/> when empty.</summary>
    public string ShortTitle { get; }

    /// <summary>Units string (e.g. <c>"Hz"</c>, <c>"dB"</c>); empty for unitless parameters.</summary>
    public string Units { get; }

    /// <summary>
    /// <c>0</c> = continuous; <c>1</c> = toggle (two states); <c>&gt;1</c> = discrete steps (the
    /// normalised range is split into <c>StepCount + 1</c> values).
    /// </summary>
    public int StepCount { get; }

    /// <summary>Plug-in's default value in normalised <c>[0, 1]</c> form.</summary>
    public double DefaultNormalizedValue { get; }

    /// <summary>Owning unit id (<c>Vst::UnitID</c>); <c>0</c> = root unit.</summary>
    public int UnitId { get; }

    /// <summary>Combined flag bitmask.</summary>
    public Vst3ParameterFlags Flags { get; }

    /// <summary><c>true</c> if the parameter declared <see cref="Vst3ParameterFlags.CanAutomate"/>.</summary>
    public bool CanAutomate => (Flags & Vst3ParameterFlags.CanAutomate) != 0;

    /// <summary><c>true</c> if the parameter is read-only.</summary>
    public bool IsReadOnly => (Flags & Vst3ParameterFlags.IsReadOnly) != 0;

    /// <summary><c>true</c> for the bypass parameter (also surfaced as
    /// <see cref="Vst3ParameterCollection.BypassParameter"/>).</summary>
    public bool IsBypass => (Flags & Vst3ParameterFlags.IsBypass) != 0;

    /// <summary><c>true</c> if the parameter is hidden from generic parameter lists.</summary>
    public bool IsHidden => (Flags & Vst3ParameterFlags.IsHidden) != 0;

    /// <summary><c>true</c> if the parameter selects the active program for its owning unit
    /// (the target a MIDI program-change message drives).</summary>
    public bool IsProgramChange => (Flags & Vst3ParameterFlags.IsProgramChange) != 0;

    /// <summary><c>true</c> if <see cref="StepCount"/> is non-zero (toggle or discrete list).</summary>
    public bool IsDiscrete => StepCount > 0;

    /// <summary>
    /// Current value in normalised <c>[0, 1]</c> form. Reads via
    /// <c>IEditController::getParamNormalized</c>; writes are queued and applied to the DSP
    /// on the next <see cref="Vst3Plugin.Process"/> call.
    /// </summary>
    public double NormalizedValue
    {
        get => _plugin.GetParameterNormalized(Id);
        set => _plugin.SetParameterNormalized(Id, value);
    }

    /// <summary>
    /// Current value converted to the plug-in's plain units (Hz, dB, %, etc.). Returns a raw
    /// double with no unit attached — <see cref="DisplayValue"/> is usually what you want for
    /// display.
    /// </summary>
    public double PlainValue => _plugin.NormalizedToPlain(Id, NormalizedValue);

    /// <summary>Sets the parameter from a value in the plug-in's plain units.</summary>
    public void SetPlainValue(double plain)
        => NormalizedValue = _plugin.PlainToNormalized(Id, plain);

    /// <summary>
    /// Formats <paramref name="normalized"/> as the display string the plug-in's own UI would
    /// show (e.g. <c>"8.4 kHz"</c>, <c>"+3.2 dB"</c>, <c>"Lowpass"</c>).
    /// </summary>
    public string FormatValue(double normalized)
        => _plugin.FormatParameter(Id, normalized);

    /// <summary>Formatted current value — convenience for <c>FormatValue(NormalizedValue)</c>.</summary>
    public string DisplayValue => FormatValue(NormalizedValue);

    /// <inheritdoc/>
    public override string ToString() => $"{Title} = {DisplayValue}";
}
