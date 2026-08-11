// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// AudioStream Terminal Types <br />
/// Various constants that describe the terminal type of an AudioStream.
/// </summary>
public enum AudioStreamTerminalType : uint
{
    // mdcdi1315: NOTE: The prefix kAudioStreamTerminalType is omitted for brevity.

    /// <summary>
    /// The ID used when the terminal type for the AudioStream is not known.
    /// </summary>
    Unknown = 0
}

/// <summary>
/// Provides common constant values for the <see cref="AudioStreamTerminalType"/> enumeration.
/// </summary>
public static class AudioStreamTerminalTypeConstants
{
    /// <summary>
    /// The ID for a terminal type of a line level stream. 
    /// Note that this applies to both input streams and output streams
    /// </summary>
    public static readonly AudioStreamTerminalType Line = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("line");
    /// <summary>
    /// The ID for a terminal type of stream from/to a digital audio interface as
    /// defined by ISO 60958 (aka SPDIF or AES/EBU). Note that this applies to both
    /// input streams and output streams
    /// </summary>
    public static readonly AudioStreamTerminalType DigitalAudioInterface = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("spdf");
    /// <summary>The ID for a terminal type of a speaker.</summary>
    public static readonly AudioStreamTerminalType Speaker = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("spkr");
    /// <summary>The ID for a terminal type of headphones.</summary>
    public static readonly AudioStreamTerminalType Headphones = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("hdph");
    /// <summary>The ID for a terminal type of a speaker for low frequency effects.</summary>
    public static readonly AudioStreamTerminalType LFESpeaker = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("lfes");
    /// <summary>The ID for a terminal type of a speaker on a telephone handset receiver.</summary>
    public static readonly AudioStreamTerminalType ReceiverSpeaker = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("rspk");
    /// <summary>The ID for a terminal type of a microphone.</summary>
    public static readonly AudioStreamTerminalType Microphone = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("micr");
    /// <summary>The ID for a terminal type of a microphone attached to an headset.</summary>
    public static readonly AudioStreamTerminalType HeadsetMicrophone = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("hmic");
    /// <summary>The ID for a terminal type of a microphone on a telephone handset receiver.</summary>
    public static readonly AudioStreamTerminalType ReceiverMicrophone = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("rmic");
    /// <summary>The ID for a terminal type of a device providing a TTY signal.</summary>
    public static readonly AudioStreamTerminalType TTY = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("tty_");
    /// <summary>The ID for a terminal type of a stream from/to an HDMI port.</summary>
    public static readonly AudioStreamTerminalType HDMI = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("hdmi");
    /// <summary>The ID for a terminal type of a stream from/to an DisplayPort port.</summary>
    public static readonly AudioStreamTerminalType DisplayPort = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("dprt");
}