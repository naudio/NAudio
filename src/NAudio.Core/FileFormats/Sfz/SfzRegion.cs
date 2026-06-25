using System.Collections.Generic;
using System.Globalization;

namespace NAudio.Sfz;

/// <summary>
/// A fully-resolved SFZ region: the merged opcode set that applies to it
/// (<c>&lt;global&gt;</c> → <c>&lt;master&gt;</c> → <c>&lt;group&gt;</c> →
/// <c>&lt;region&gt;</c>, most specific winning), with typed accessors. This is
/// the SFZ counterpart of <see cref="NAudio.SoundFont.SoundFontRegion"/>;
/// mapping these opcodes onto the format-neutral synthesis model is a later
/// step.
/// </summary>
public sealed class SfzRegion
{
    private readonly Dictionary<string, string> opcodes;

    internal SfzRegion(Dictionary<string, string> opcodes, string sample)
    {
        this.opcodes = opcodes;
        Sample = sample;
    }

    /// <summary>
    /// The region's sample path: the <c>sample</c> opcode combined with the
    /// <c>&lt;control&gt;</c> <c>default_path</c>, with backslashes normalised
    /// to forward slashes. Null if the region has no <c>sample</c> opcode.
    /// Resolving this to an absolute path and loading it is the loader's job.
    /// </summary>
    public string Sample { get; }

    /// <summary>The merged opcodes that apply to this region.</summary>
    public IReadOnlyDictionary<string, string> Opcodes => opcodes;

    /// <summary>Whether the given opcode is present.</summary>
    public bool Has(string opcode) => opcodes.ContainsKey(opcode);

    /// <summary>The raw string value of an opcode, or <paramref name="fallback"/> if absent.</summary>
    public string GetString(string opcode, string fallback = null) =>
        opcodes.TryGetValue(opcode, out var value) ? value : fallback;

    /// <summary>
    /// The integer value of an opcode, or <paramref name="fallback"/> if absent
    /// or not a valid integer.
    /// </summary>
    public int GetInt(string opcode, int fallback)
    {
        if (opcodes.TryGetValue(opcode, out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return fallback;
    }

    /// <summary>
    /// The floating-point value of an opcode, or <paramref name="fallback"/> if
    /// absent or not a valid number.
    /// </summary>
    public float GetFloat(string opcode, float fallback)
    {
        if (opcodes.TryGetValue(opcode, out var value) &&
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return fallback;
    }
}
