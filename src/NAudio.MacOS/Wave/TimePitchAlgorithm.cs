namespace NAudio.Wave;

/// <summary>
/// The time-pitch algorithms that an audio queue can use.
/// The values are the four-character codes defined by Audio Queue Services.
/// </summary>
public enum TimePitchAlgorithm : uint
{
    /// <summary>The highest quality and the most expensive algorithm (<c>spec</c>).</summary>
    Spectral = 0x73706563,

    /// <summary>A cheaper algorithm of modest quality, suitable for voice (<c>tido</c>).</summary>
    TimeDomain = 0x7469646F,

    /// <summary>High quality with no pitch correction, so pitch follows the rate (<c>vspd</c>).</summary>
    Varispeed = 0x76737064
}
