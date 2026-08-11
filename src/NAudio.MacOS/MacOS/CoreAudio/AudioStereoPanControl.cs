// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// A control class that extends by the <see cref="AudioControl"/> class
/// and provides stereo panning between two channels.
/// </summary>
public class AudioStereoPanControl : AudioControl
{
    internal AudioStereoPanControl(AudioObjectID id) : base(id) { }

    /// <summary>
    /// A <see cref="float"/> where 0.0 is full left, 1.0 is full right, and 0.5 is center.
    /// </summary>
    public float Value
    {
        get => GetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStereoPanControlProperties.kAudioStereoPanControlPropertyValue));
        set => SetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStereoPanControlProperties.kAudioStereoPanControlPropertyValue), value);
    }

    /// <summary>
    /// An array of two <see cref="uint"/>s that indicate which elements of the device the signal is being panned between.
    /// </summary>
    public uint[] PanningChannels => GetArrayOfTPropertyValue<uint>(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStereoPanControlProperties.kAudioStereoPanControlPropertyPanningChannels), 2);

    /// <inheritdoc />
    public override string ToString() => $"AudioStereoPanControl {{ Kind = {Kind}, Handle = 0x{GetHashCode():x2}, Scope = {Scope}, Element = {Element}, Value = {Value}, PanningChannels = {PanningChannels} }}";
}

