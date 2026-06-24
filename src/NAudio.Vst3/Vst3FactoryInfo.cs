using System;
using System.Collections.Generic;

namespace NAudio.Vst3;

/// <summary>
/// Vendor / URL / email / flags returned by a VST 3 plug-in factory.
/// </summary>
public sealed record Vst3FactoryInfo(string Vendor, string Url, string Email, int Flags);

/// <summary>
/// Coarse classification of a VST 3 audio plug-in, derived from its sub-categories — the same
/// instrument-vs-effect split DAWs use to offer curated, type-filtered plug-in pickers.
/// </summary>
public enum Vst3PlugKind
{
    /// <summary>Not an instantiable audio plug-in (e.g. a component-controller or service class).</summary>
    Other,

    /// <summary>An audio effect — processes an audio input to an audio output.</summary>
    Effect,

    /// <summary>An instrument (VSTi) — generates audio from note/MIDI events.</summary>
    Instrument,
}

/// <summary>
/// Description of a single class exported by a VST 3 plug-in factory.
/// </summary>
/// <param name="ClassId">16-byte raw class identifier (TUID), hex-encoded.</param>
/// <param name="Category">Category string — <c>"Audio Module Class"</c> for processors,
/// <c>"Component Controller Class"</c> for controllers.</param>
/// <param name="Name">User-visible class name.</param>
/// <param name="Vendor">Vendor string (empty when only v1 class-info is available).</param>
/// <param name="Version">Version string (empty when only v1 class-info is available).</param>
/// <param name="SdkVersion">SDK version the class was built against (empty for v1-only).</param>
/// <param name="SubCategories">OR-combined sub-categories such as <c>"Fx|Reverb"</c> or
/// <c>"Instrument|Synth"</c> (empty when only v1 class-info is available).</param>
public sealed record Vst3ClassInfo(
    string ClassId,
    string Category,
    string Name,
    string Vendor,
    string Version,
    string SdkVersion,
    string SubCategories)
{
    /// <summary>The category string the SDK uses for an instantiable audio plug-in.</summary>
    public const string AudioModuleCategory = "Audio Module Class";

    /// <summary>The sub-category token that marks an instrument (VST 3 <c>PlugType</c>).</summary>
    public const string InstrumentSubCategory = "Instrument";

    /// <summary>
    /// <see cref="SubCategories"/> split into its individual tokens (e.g. <c>"Instrument|Synth"</c>
    /// → <c>["Instrument", "Synth"]</c>). Useful for finer filtering (by <c>"Reverb"</c>,
    /// <c>"Delay"</c>, <c>"Synth"</c>, …).
    /// </summary>
    public IReadOnlyList<string> SubCategoryList { get; } =
        SubCategories.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary><c>true</c> when this class is an instantiable audio plug-in (an "Audio Module Class").</summary>
    public bool IsAudioModule => string.Equals(Category, AudioModuleCategory, StringComparison.Ordinal);

    /// <summary>
    /// <c>true</c> for a VST instrument — an audio module whose sub-categories include
    /// <c>"Instrument"</c>. This is the convention DAWs (and JUCE) use to split synths from effects.
    /// </summary>
    public bool IsInstrument => IsAudioModule && HasSubCategory(InstrumentSubCategory);

    /// <summary><c>true</c> for an audio effect — an audio module that is not an instrument.</summary>
    public bool IsEffect => IsAudioModule && !IsInstrument;

    /// <summary>Coarse instrument / effect / other classification for type-filtered plug-in lists.</summary>
    public Vst3PlugKind Kind =>
        !IsAudioModule ? Vst3PlugKind.Other
        : IsInstrument ? Vst3PlugKind.Instrument
        : Vst3PlugKind.Effect;

    /// <summary><c>true</c> if <paramref name="token"/> is one of this class's sub-categories (case-insensitive).</summary>
    public bool HasSubCategory(string token)
    {
        foreach (var s in SubCategoryList)
        {
            if (string.Equals(s, token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
