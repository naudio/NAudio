/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Contains:   API for communicating with audio hardware.

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the base type for implementing audio boolean controls 
/// (that is, boolean toggleable controls).
/// </summary>
public class AudioBooleanControl : AudioControl
{
    internal AudioBooleanControl(AudioObjectID objectId) : base(objectId) { }

    /// <summary>
    /// The value to toggle on/off.
    /// </summary>
    public bool Value
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioBooleanControlProperties.kAudioBooleanControlPropertyValue)) != 0;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioBooleanControlProperties.kAudioBooleanControlPropertyValue), value ? 1U : 0U);
    }

    /// <inheritdoc />
    public override string ToString() => $"AudioBooleanControl {{ Kind = {Kind}, Handle = 0x{GetHashCode():x2}, Scope = {Scope}, Element = {Element}, Value = {Value} }}";
}