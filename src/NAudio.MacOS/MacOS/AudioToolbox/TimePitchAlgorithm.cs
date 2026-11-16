// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// The time-pitch algorithms that an audio queue can use. <br />
/// The actual algorithms are provided by the <see cref="TimePitchAlgorithmConstants"/> class,
/// because their values are four character codes.
/// </summary>
/// <seealso cref="TimePitchAlgorithmConstants"/>
public enum TimePitchAlgorithm : uint
{
    /// <summary>No algorithm is requested, so the audio queue uses the default of the system.</summary>
    Default = 0,
}

/// <summary>
/// Provides common constants for the <see cref="TimePitchAlgorithm"/> enumeration.
/// </summary>
public static class TimePitchAlgorithmConstants
{
    /// <summary>The highest quality and the most computationally expensive algorithm.</summary>
    public static readonly TimePitchAlgorithm Spectral = (TimePitchAlgorithm)MacUtils.ConstructUIntConstantValueFromString("spec");
    /// <summary>A cheaper algorithm of modest quality, suitable for voice.</summary>
    public static readonly TimePitchAlgorithm TimeDomain = (TimePitchAlgorithm)MacUtils.ConstructUIntConstantValueFromString("tido");
    /// <summary>A high quality algorithm that applies no pitch correction, so the pitch follows the playback rate.</summary>
    public static readonly TimePitchAlgorithm Varispeed = (TimePitchAlgorithm)MacUtils.ConstructUIntConstantValueFromString("vspd");
}
