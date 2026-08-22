// This interop definition was derived from the file AudioHardwareBase.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioLevelControl Property Selectors
/// AudioObjectPropertySelector values provided by the AudioLevelControl class.
/// </summary>
/// <remarks>
/// The AudioLevelControl class is a subclass of the AudioControl class and has the
/// same scope and element structure.
/// </remarks>
internal static class AudioLevelControlProperties
{
    /// <summary>
    /// A Float32 that represents the value of the volume control. The range is
    /// between 0.0 and 1.0 (inclusive). Note that the set of all Float32 values
    /// between 0.0 and 1.0 inclusive is much larger than the set of actual values
    /// that the hardware can select. This means that the Float32 range has a many
    /// to one mapping with the underlying hardware values. As such, setting a
    /// scalar value will result in the control taking on the value nearest to what
    /// was set.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioLevelControlPropertyScalarValue               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lcsv");
    /// <summary>
    /// A Float32 that represents the value of the volume control in dB. Note that
    /// the set of all Float32 values in the dB range for the control is much larger
    /// than the set of actual values that the hardware can select. This means that
    /// the Float32 range has a many to one mapping with the underlying hardware
    /// values. As such, setting a dB value will result in the control taking on the
    /// value nearest to what was set.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioLevelControlPropertyDecibelValue              = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lcdv");
    /// <summary>
    /// An AudioValueRange that contains the minimum and maximum dB values the control can have.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioLevelControlPropertyDecibelRange              = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lcdr");
    /// <summary>
    /// A Float32 that on input contains a scalar volume value for the and on exit contains the equivalent dB value.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioLevelControlPropertyConvertScalarToDecibels   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lcsd");
    /// <summary>
    /// A Float32 that on input contains a dB volume value for the and on exit contains the equivalent scalar value.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioLevelControlPropertyConvertDecibelsToScalar   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lcds");
}