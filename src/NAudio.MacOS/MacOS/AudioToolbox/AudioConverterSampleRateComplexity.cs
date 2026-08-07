// This interop definition was derived from the file AudioConverter.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>Sample Rate Converter Complexity</summary>
public enum AudioConverterSampleRateComplexity : uint { }

/// <summary>
/// Provides constants for common values of the <see cref="AudioConverterSampleRateComplexity"/> enumeration.
/// </summary>
public static class AudioConverterSampleRateComplexityConstants
{
    /// <summary>
    /// Linear interpolation. lowest quality, cheapest.
	/// InitialPhase and PrimeMethod properties are not operative with this mode.
    /// </summary>
    public static readonly AudioConverterSampleRateComplexity Linear             = (AudioConverterSampleRateComplexity)MacUtils.ConstructUIntConstantValueFromString("line");  // linear interpolation
    /// <summary>
    /// Normal quality sample rate conversion.
    /// </summary>
    public static readonly AudioConverterSampleRateComplexity Normal             = (AudioConverterSampleRateComplexity)MacUtils.ConstructUIntConstantValueFromString("norm");  // normal quality range, the default
    /// <summary>
    /// Mastering quality sample rate conversion. More expensive.
    /// </summary>
    public static readonly AudioConverterSampleRateComplexity Mastering          = (AudioConverterSampleRateComplexity)MacUtils.ConstructUIntConstantValueFromString("bats");  // higher quality range, more expensive
    /// <summary>
    /// Minimum phase impulse response. 
    /// Stopband attenuation varies with quality setting. <br />
    /// The InitialPhase and PrimeMethod properties are not operative with this mode. <br />
    /// There are three levels of quality provided.
    /// <list type="bullet">
    ///     <item><see cref="AudioConverterQuality.Low"/> (or <see cref="AudioConverterQuality.Min"/>): noise floor to -96 dB</item>
    ///     <item><see cref="AudioConverterQuality.Medium"/>: noise floor to -144 dB</item>
    ///     <item><see cref="AudioConverterQuality.High"/> (or <see cref="AudioConverterQuality.Max"/>): noise floor to -160 dB (this uses double precision internally)</item>
    /// </list>
	/// Quality equivalences to the other complexity modes are very roughly as follows:
    /// 
	/// MinimumPhase Low    is somewhat better than Normal Medium.
	/// MinimumPhase Medium is similar to Normal Max.
	/// MinimumPhase High   is similar to Mastering Low.
	/// In general, MinimumPhase performs better than Normal and Mastering for the equivalent qualities listed above.
	/// MinimumPhase High is several times faster than Mastering Low.
    /// </summary>
    public static readonly AudioConverterSampleRateComplexity MinimumPhase       = (AudioConverterSampleRateComplexity)MacUtils.ConstructUIntConstantValueFromString("minp");	// minimum phase impulse response.
}