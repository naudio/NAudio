/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

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
    public static readonly AudioStreamTerminalType Line = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("line"u8);
    /// <summary>
    /// The ID for a terminal type of stream from/to a digital audio interface as
    /// defined by ISO 60958 (aka SPDIF or AES/EBU). Note that this applies to both
    /// input streams and output streams
    /// </summary>
    public static readonly AudioStreamTerminalType DigitalAudioInterface = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("spdf"u8);
    /// <summary>The ID for a terminal type of a speaker.</summary>
    public static readonly AudioStreamTerminalType Speaker = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("spkr"u8);
    /// <summary>The ID for a terminal type of headphones.</summary>
    public static readonly AudioStreamTerminalType Headphones = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("hdph"u8);
    /// <summary>The ID for a terminal type of a speaker for low frequency effects.</summary>
    public static readonly AudioStreamTerminalType LFESpeaker = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("lfes"u8);
    /// <summary>The ID for a terminal type of a speaker on a telephone handset receiver.</summary>
    public static readonly AudioStreamTerminalType ReceiverSpeaker = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("rspk"u8);
    /// <summary>The ID for a terminal type of a microphone.</summary>
    public static readonly AudioStreamTerminalType Microphone = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("micr"u8);
    /// <summary>The ID for a terminal type of a microphone attached to an headset.</summary>
    public static readonly AudioStreamTerminalType HeadsetMicrophone = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("hmic"u8);
    /// <summary>The ID for a terminal type of a microphone on a telephone handset receiver.</summary>
    public static readonly AudioStreamTerminalType ReceiverMicrophone = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("rmic"u8);
    /// <summary>The ID for a terminal type of a device providing a TTY signal.</summary>
    public static readonly AudioStreamTerminalType TTY = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("tty_"u8);
    /// <summary>The ID for a terminal type of a stream from/to an HDMI port.</summary>
    public static readonly AudioStreamTerminalType HDMI = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("hdmi"u8);
    /// <summary>The ID for a terminal type of a stream from/to an DisplayPort port.</summary>
    public static readonly AudioStreamTerminalType DisplayPort = (AudioStreamTerminalType)MacUtils.ConstructUIntConstantValueFromString("dprt"u8);
}