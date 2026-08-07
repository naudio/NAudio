/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Defines the audio slider control, which is a subclass of the <see cref="AudioControl" />
/// class used for a value modified in a fixed range of values.
/// </summary>
public class AudioSliderControl : AudioControl
{
    internal AudioSliderControl(AudioObjectID objectId) : base(objectId) { }

    /// <summary>
    /// A <see cref="uint"/> that represents the value of the slider control.
    /// </summary>
    public uint Value
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSliderControlProperties.kAudioSliderControlPropertyValue));
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSliderControlProperties.kAudioSliderControlPropertyValue), value);
    }

    /// <summary>
    /// An array of two <see cref="uint"/>s that represents the inclusive range of values the slider control can take.
    /// </summary>
    public uint[] Range => GetArrayOfTPropertyValue<uint>(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSliderControlProperties.kAudioSliderControlPropertyRange), 2);

    /// <inheritdoc />
    public override string ToString()
    {
        var range = Range;
        return $"AudioSliderControl {{ Kind = {Kind}, Handle = 0x{GetHashCode():x2}, Scope = {Scope}, Element = {Element}, Value = {Value}, Range = {range[0]}..{range[1]} }}";
    }
}