using System.Text.Json;

namespace NAudio.Nam;

/// <summary>
/// Descriptive information a NAM trainer stores in a model file. Every field is
/// optional, so each property is null when the file does not supply it.
/// </summary>
public sealed class NamModelMetadata
{
    internal static readonly NamModelMetadata Empty = new();

    /// <summary>The capture's name.</summary>
    public string? Name { get; private init; }

    /// <summary>Who made the capture.</summary>
    public string? ModeledBy { get; private init; }

    /// <summary>The kind of gear captured, e.g. "amp", "pedal" or "amp_cab".</summary>
    public string? GearType { get; private init; }

    /// <summary>The captured gear's manufacturer.</summary>
    public string? GearMake { get; private init; }

    /// <summary>The captured gear's model.</summary>
    public string? GearModel { get; private init; }

    /// <summary>The tone character, e.g. "clean", "crunch" or "hi_gain".</summary>
    public string? ToneType { get; private init; }

    /// <summary>
    /// The model's output loudness in dB for a standard input, which a host can use
    /// to level-match different models.
    /// </summary>
    public double? Loudness { get; private init; }

    /// <summary>
    /// The trainer's estimate of how much gain the captured gear applies, from 0
    /// (clean) upwards.
    /// </summary>
    public double? Gain { get; private init; }

    /// <summary>
    /// The level in dBu of a full-scale input during capture. Matching an audio
    /// interface's input calibration to this reproduces the captured gear's drive.
    /// </summary>
    public double? InputLevelDbu { get; private init; }

    /// <summary>The level in dBu that a full-scale output corresponds to.</summary>
    public double? OutputLevelDbu { get; private init; }

    internal static NamModelMetadata Parse(JsonElement model)
    {
        if (!model.TryGetProperty("metadata", out var m) || m.ValueKind != JsonValueKind.Object)
            return Empty;
        return new NamModelMetadata
        {
            Name = GetString(m, "name"),
            ModeledBy = GetString(m, "modeled_by"),
            GearType = GetString(m, "gear_type"),
            GearMake = GetString(m, "gear_make"),
            GearModel = GetString(m, "gear_model"),
            ToneType = GetString(m, "tone_type"),
            Loudness = GetDouble(m, "loudness"),
            Gain = GetDouble(m, "gain"),
            InputLevelDbu = GetDouble(m, "input_level_dbu"),
            OutputLevelDbu = GetDouble(m, "output_level_dbu"),
        };
    }

    private static string? GetString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? GetDouble(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
}
