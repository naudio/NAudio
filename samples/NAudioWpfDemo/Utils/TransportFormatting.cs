using System;

namespace NAudioWpfDemo.Utils;

/// <summary>
/// Shared conversions and readout formatting for the demo transport controls (volume slider,
/// position indicator), so the panels that offer them all behave and read the same way.
/// </summary>
internal static class TransportFormatting
{
    /// <summary>
    /// Bottom of the volume slider range, in dB. Perceived loudness is logarithmic, so a dB
    /// slider gives equal audible change for equal slider travel; the bottom is treated as mute.
    /// </summary>
    public const double MinVolumeDb = -60;

    /// <summary>Converts a slider position in dB to the linear gain an NAudio provider wants.</summary>
    public static float GainFromDb(double db) =>
        db <= MinVolumeDb ? 0f : (float)Math.Pow(10.0, db / 20.0);

    /// <summary>The volume readout ("-12.0 dB", or "Mute" at the bottom of the range).</summary>
    public static string FormatVolume(double db) =>
        db <= MinVolumeDb ? "Mute" : $"{db:F1} dB";

    /// <summary>Formats a position/duration as m:ss.</summary>
    public static string FormatTime(TimeSpan time) =>
        (time < TimeSpan.Zero ? TimeSpan.Zero : time).ToString(@"m\:ss");
}
