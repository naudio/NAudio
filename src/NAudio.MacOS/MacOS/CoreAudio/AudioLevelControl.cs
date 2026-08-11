// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// The AudioLevelControl class is an <see cref="AudioControl"/> class
/// that level-related controls do inherit from.
/// </summary>
public class AudioLevelControl : AudioControl
{
    internal AudioLevelControl(AudioObjectID objectId) : base(objectId) { }

    /// <summary>
    /// A <see cref="float"/> that represents the value of the volume control. The range is
    /// between 0.0 and 1.0 (inclusive). Note that the set of all <see cref="float"/> values
    /// between 0.0 and 1.0 inclusive is much larger than the set of actual values
    /// that the hardware can select. This means that the <see cref="float"/> range has a many
    /// to one mapping with the underlying hardware values. As such, setting a
    /// scalar value will result in the control taking on the value nearest to what
    /// was set.
    /// </summary>
    public float ScalarValue
    {
        get => GetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioLevelControlProperties.kAudioLevelControlPropertyScalarValue));
        set => SetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioLevelControlProperties.kAudioLevelControlPropertyScalarValue), value);
    }

    /// <summary>
    /// A <see cref="float"/> that represents the value of the volume control in dB. Note that
    /// the set of all <see cref="float"/> values in the dB range for the control is much larger
    /// than the set of actual values that the hardware can select. This means that
    /// the <see cref="float"/> range has a many to one mapping with the underlying hardware
    /// values. As such, setting a dB value will result in the control taking on the
    /// value nearest to what was set.
    /// </summary>
    public float DecibelValue
    {
        get => GetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioLevelControlProperties.kAudioLevelControlPropertyDecibelValue));
        set => SetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioLevelControlProperties.kAudioLevelControlPropertyDecibelValue), value);
    }

    /// <summary>
    /// A tuple that contains the minimum and maximum dB values the control can have.
    /// </summary>
    public (double minimum, double maximum) DecibelRange
    {
        get
        {
            var range = GetAudioValueRangePropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioLevelControlProperties.kAudioLevelControlPropertyDecibelRange));
            return (range.mMinimum, range.mMaximum);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"AudioLevelControl {{ Kind = {Kind}, Handle = 0x{GetHashCode():x2}, Scope = {Scope}, Element = {Element}, DecibelValue = {DecibelValue}, DecibelRange = {DecibelRange} }}";
}

