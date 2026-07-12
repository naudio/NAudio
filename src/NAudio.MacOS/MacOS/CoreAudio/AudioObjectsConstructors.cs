
using System.Diagnostics;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

// Class providing methods for constructing audio objects.
internal static class AudioObjectsConstructors
{
    public static Process ConstructProcess(AudioObjectID id) => new(id);

    public static AudioStream ConstructStream(AudioObjectID id) => new(id);

    public static AudioDevice ConstructDevice(AudioObjectID id) => new(id);

    public static AudioClockDevice ConstructClockDevice(AudioObjectID id) => new(id);

    public static AudioLevelControl ConstructAudioLevelControl(AudioObjectID id) => new(id);

    public static AudioSliderControl ConstructAudioSliderControl(AudioObjectID id) => new(id);

    public static AudioBooleanControl ConstructAudioBooleanControl(AudioObjectID id) => new(id);

    public static AudioSelectorControl ConstructAudioSelectorControl(AudioObjectID id) => new(id);

    public static AudioStereoPanControl ConstructAudioStereoPanControl(AudioObjectID id) => new(id);

    [StackTraceHidden]
    [DebuggerStepThrough]
    private static void ThrowOnStatusError(AudioObjectPropertyAddress address, int status)
    {
        if (status == Interop.ErrorConstants.kAudioHardwareUnknownPropertyError)
        {
            throw new CoreAudioPropertyNotFoundException((uint)address.mSelector);
        }
        else
        {
            CoreAudioException.ThrowIfError(status);
        }
    }

    private static unsafe AudioClassID QueryClassID(AudioObjectID id, bool baseClass = false)
    {
        uint returnValue;
        uint size = sizeof(uint);
        var address = AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    baseClass ? AudioObjectProperties.kAudioObjectPropertyBaseClass : AudioObjectProperties.kAudioObjectPropertyClass
                );
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(id, address, ref size, new(&returnValue))
        );
        return (AudioClassID)returnValue;
    }

    private static AudioControl ConstructAudioControlInternal(AudioClassID classID, AudioObjectID id)
    {
        if (
            classID == AudioClassIDs.kAudioBooleanControlClassID ||
            classID == AudioClassIDs.kAudioMuteControlClassID ||
            classID == AudioClassIDs.kAudioLFEMuteControlClassID ||
            classID == AudioClassIDs.kAudioSoloControlClassID ||
            classID == AudioClassIDs.kAudioJackControlClassID ||
            classID == AudioClassIDs.kAudioPhantomPowerControlClassID ||
            classID == AudioClassIDs.kAudioPhaseInvertControlClassID ||
            classID == AudioClassIDs.kAudioClipLightControlClassID ||
            classID == AudioClassIDs.kAudioTalkbackControlClassID ||
            classID == AudioClassIDs.kAudioListenbackControlClassID
        )
        {
            return ConstructAudioBooleanControl(id);
        }
        else if (
            classID == AudioClassIDs.kAudioLevelControlClassID ||
            classID == AudioClassIDs.kAudioVolumeControlClassID ||
            classID == AudioClassIDs.kAudioLFEVolumeControlClassID
        )
        {
            return ConstructAudioLevelControl(id);
        }
        else if (
            classID == AudioClassIDs.kAudioSelectorControlClassID ||
            classID == AudioClassIDs.kAudioDataSourceControlClassID ||
            classID == AudioClassIDs.kAudioDataDestinationControlClassID ||
            classID == AudioClassIDs.kAudioClockSourceControlClassID ||
            classID == AudioClassIDs.kAudioLineLevelControlClassID ||
            classID == AudioClassIDs.kAudioHighPassFilterControlClassID
        )
        {
            return ConstructAudioSelectorControl(id);
        }
        else if (
            classID == AudioClassIDs.kAudioSliderControlClassID
        )
        {
            return ConstructAudioSliderControl(id);
        }
        else if (
            classID == AudioClassIDs.kAudioStereoPanControlClassID
        )
        {
            return ConstructAudioStereoPanControl(id);
        }
        else
        {
            return new UnknownAudioControl(id);
        }
    }

    public static AudioControl ConstructAudioControl(AudioObjectID id)
    {
        var ac = ConstructAudioControlInternal(QueryClassID(id), id);
        return ac ?? ConstructAudioControlInternal(QueryClassID(id, true), id);
    }

    public static AudioObject ConstructAudioObject(AudioObjectID id)
    {
        AudioClassID classID = QueryClassID(id);
        if (classID == AudioClassIDs.kAudioSystemObjectClassID)
        {
            return AudioSystemObject.Instance;
        }
        else if (classID == AudioClassIDs.kAudioClockDeviceClassID)
        {
            return ConstructClockDevice(id);
        }
        else if (classID == AudioClassIDs.kAudioDeviceClassID)
        {
            return ConstructDevice(id);
        }
        else if (classID == AudioClassIDs.kAudioStreamClassID)
        {
            return ConstructStream(id);
        }
        else if (classID == AudioClassIDs.kAudioProcessClassID)
        {
            return ConstructProcess(id);
        }
        else
        {
            return ConstructAudioControl(id);
        }
    }

    private sealed class UnknownAudioControl : AudioControl
    {
        public UnknownAudioControl(AudioObjectID objectId) : base(objectId) { }
    }
}