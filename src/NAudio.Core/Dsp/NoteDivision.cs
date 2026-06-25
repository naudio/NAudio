using System;

namespace NAudio.Dsp;

/// <summary>
/// A musical note length, used to derive a time (for tempo-synced delays) or a rate
/// (for tempo-synced modulation) from a tempo in BPM.
/// </summary>
public enum NoteDivision
{
    /// <summary>Whole note (4 beats).</summary>
    Whole,
    /// <summary>Half note (2 beats).</summary>
    Half,
    /// <summary>Quarter note (1 beat).</summary>
    Quarter,
    /// <summary>Eighth note.</summary>
    Eighth,
    /// <summary>Sixteenth note.</summary>
    Sixteenth,
    /// <summary>Thirty-second note.</summary>
    ThirtySecond,
    /// <summary>Dotted half note.</summary>
    DottedHalf,
    /// <summary>Dotted quarter note.</summary>
    DottedQuarter,
    /// <summary>Dotted eighth note.</summary>
    DottedEighth,
    /// <summary>Quarter-note triplet.</summary>
    TripletQuarter,
    /// <summary>Eighth-note triplet.</summary>
    TripletEighth,
    /// <summary>Sixteenth-note triplet.</summary>
    TripletSixteenth
}

/// <summary>
/// Converts a <see cref="NoteDivision"/> at a given tempo into seconds or Hz.
/// </summary>
public static class TempoTime
{
    /// <summary>
    /// Number of quarter-note beats a division spans.
    /// </summary>
    public static double Beats(NoteDivision division) => division switch
    {
        NoteDivision.Whole => 4.0,
        NoteDivision.Half => 2.0,
        NoteDivision.Quarter => 1.0,
        NoteDivision.Eighth => 0.5,
        NoteDivision.Sixteenth => 0.25,
        NoteDivision.ThirtySecond => 0.125,
        NoteDivision.DottedHalf => 3.0,
        NoteDivision.DottedQuarter => 1.5,
        NoteDivision.DottedEighth => 0.75,
        NoteDivision.TripletQuarter => 2.0 / 3.0,
        NoteDivision.TripletEighth => 1.0 / 3.0,
        NoteDivision.TripletSixteenth => 1.0 / 6.0,
        _ => 1.0
    };

    /// <summary>
    /// Length of the division in seconds at <paramref name="bpm"/>.
    /// </summary>
    public static double Seconds(double bpm, NoteDivision division)
    {
        if (bpm <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(bpm), "Tempo must be positive");
        return 60.0 / bpm * Beats(division);
    }

    /// <summary>
    /// Rate of the division in Hz at <paramref name="bpm"/> (1 / <see cref="Seconds"/>).
    /// </summary>
    public static double Hertz(double bpm, NoteDivision division)
        => 1.0 / Seconds(bpm, division);
}
