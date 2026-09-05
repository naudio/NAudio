// This interop definition was derived from the file AudioHardware.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Power Hints <br />
/// The values for kAudioHardwarePropertyPowerHint
/// </summary>
/// <remarks>
/// The system object property, <see cref="AudioSystemObject.PowerHint"/>, allows a process to
/// to indicate how aggressive the system can be with optimizations that save power.
/// Note that the value of this property can be set in an application's info.plist
/// using the key, "AudioHardwarePowerHint". The values for this key are the strings
/// that correspond to the values in the enum.
/// </remarks>
public enum AudioHardwarePowerHint : uint
{
    // mdcdi1315: NOTE: The prefix kAudioHardwarePowerHint is omitted for brevity.

    /// <summary>
    /// This is the default value and it indicates that the system will not make any
    /// power optimizations that compromise latency or quality in order to save
    /// power. The info.plist value is "None" or the "AudioHardwarePowerHint" entry
    /// can be omitted entirely.
    /// </summary>
    None = 0,
    /// <summary>
    /// The system will choose to save power even at the expense of latency. The
    /// info.plist value is "Favor Saving Power"
    /// </summary>
    FavorSavingPower = 1
}