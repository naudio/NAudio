// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// An <see cref="AudioObjectPropertySelector"/> is a four char code that identifies, along with
/// the <see cref="AudioObjectPropertyScope"/> and <see cref="AudioObjectPropertyElement"/>, a specific piece of
/// information about an AudioObject.
/// </summary>
/// <remarks>
/// The property selector specifies the general classification of the property such
/// as volume, stream format, latency, etc. Note that each class has a different set
/// of selectors. A subclass inherits its super class's set of selectors, although
/// it may not implement them all.
/// </remarks>
public enum AudioObjectPropertySelector : uint
{
    /// <summary>
    /// The wildcard value for AudioObjectPropertySelectors.
    /// </summary>
    kAudioObjectPropertySelectorWildcard = 0x2a2a2a2a // '****'
}