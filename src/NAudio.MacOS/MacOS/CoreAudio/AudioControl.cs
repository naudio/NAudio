/*==================================================================================================
     File:       CoreAudio/AudioHardware.h

     Contains:   API for communicating with audio hardware.

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// The AudioControl class is a subclass of the AudioObject class. The class has
/// just the global scope, kAudioObjectPropertyScopeGlobal, and only a main
/// element.
/// </summary>
public abstract class AudioControl : AudioObject
{
    private readonly AudioControlKind kind;

    internal AudioControl(AudioObjectID objectId) : base(objectId) { kind = ComputeKind(); }

    private AudioControlKind ComputeKind()
    {
        AudioClassID id = (AudioClassID)ClassID;
        if (id == AudioClassIDs.kAudioControlClassID)
        {
            return AudioControlKind.AudioControl;
        }
        else if (id == AudioClassIDs.kAudioBooleanControlClassID)
        {
            return AudioControlKind.BooleanControl;
        }
        else if (id == AudioClassIDs.kAudioSliderControlClassID)
        {
            return AudioControlKind.SliderControl;
        }
        else if (id == AudioClassIDs.kAudioSelectorControlClassID)
        {
            return AudioControlKind.SelectorControl;
        }
        else if (id == AudioClassIDs.kAudioStereoPanControlClassID)
        {
            return AudioControlKind.StereoPanControl;
        }
        else if (id == AudioClassIDs.kAudioLevelControlClassID)
        {
            return AudioControlKind.LevelControl;
        }
        else if (id == AudioClassIDs.kAudioVolumeControlClassID)
        {
            return AudioControlKind.VolumeControl;
        }
        else if (id == AudioClassIDs.kAudioLFEVolumeControlClassID)
        {
            return AudioControlKind.LFEVolumeControl;
        }
        else if (id == AudioClassIDs.kAudioMuteControlClassID)
        {
            return AudioControlKind.MuteControl;
        }
        else if (id == AudioClassIDs.kAudioSoloControlClassID)
        {
            return AudioControlKind.SoloControl;
        }
        else if (id == AudioClassIDs.kAudioJackControlClassID)
        {
            return AudioControlKind.JackControl;
        }
        else if (id == AudioClassIDs.kAudioLFEMuteControlClassID)
        {
            return AudioControlKind.LFEMuteControl;
        }
        else if (id == AudioClassIDs.kAudioPhantomPowerControlClassID)
        {
            return AudioControlKind.PhantomPowerControl;
        }
        else if (id == AudioClassIDs.kAudioPhaseInvertControlClassID)
        {
            return AudioControlKind.PhaseInvertControl;
        }
        else if (id == AudioClassIDs.kAudioClipLightControlClassID)
        {
            return AudioControlKind.ClipLightControl;
        }
        else if (id == AudioClassIDs.kAudioTalkbackControlClassID)
        {
            return AudioControlKind.TalkbackControl;
        }
        else if (id == AudioClassIDs.kAudioListenbackControlClassID)
        {
            return AudioControlKind.ListenbackControl;
        }
        else if (id == AudioClassIDs.kAudioDataSourceControlClassID)
        {
            return AudioControlKind.DataSourceControl;
        }
        else if (id == AudioClassIDs.kAudioDataDestinationControlClassID)
        {
            return AudioControlKind.DataDestinationControl;
        }
        else if (id == AudioClassIDs.kAudioClockSourceControlClassID)
        {
            return AudioControlKind.ClockSourceControl;
        }
        else if (id == AudioClassIDs.kAudioLineLevelControlClassID)
        {
            return AudioControlKind.LineLevelControl;
        }
        else if (id == AudioClassIDs.kAudioHighPassFilterControlClassID)
        {
            return AudioControlKind.HighPassFilterControl;
        }
        else
        {
            return AudioControlKind.Unknown;
        }
    }

    /// <summary>
    /// An <see cref="AudioObjectPropertyScope"/> that indicates which part of a device the control applies to.
    /// </summary>
    public AudioObjectPropertyScope Scope => (AudioObjectPropertyScope)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioControlProperties.kAudioControlPropertyScope));

    /// <summary>
    /// An property element value that indicates which element of the device the control applies to.
    /// </summary>
    public uint Element => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioControlProperties.kAudioControlPropertyElement));

    /// <summary>
    /// Gets the kind of this <see cref="AudioControl"/> instance.
    /// </summary>
    public AudioControlKind Kind => kind;

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name} {{ Kind = {kind}, Handle = 0x{GetHashCode():x2}, Scope = {Scope}, Element = {Element} }}";
}