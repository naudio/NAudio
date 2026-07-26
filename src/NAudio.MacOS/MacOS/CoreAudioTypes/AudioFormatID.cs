/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// Format IDs <br />
/// The AudioFormatIDs used to identify individual formats of audio data.
/// </summary>
internal enum AudioFormatID : uint { }

/// <summary>
/// Format IDs <br />
/// The AudioFormatIDs used to identify individual formats of audio data.
/// </summary>
internal static class AudioFormatIDs
{
    /// <summary>Linear PCM, uses the standard flags.</summary>
    public static readonly AudioFormatID kAudioFormatLinearPCM               = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("lpcm");
    /// <summary>AC-3, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatAC3                     = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("ac-3");
    /// <summary>
    /// AC-3 packaged for transport over an IEC 60958 compliant 
    /// digital audio interface. Uses the standard flags.
    /// </summary>
    public static readonly AudioFormatID kAudioFormat60958AC3                = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("cac3");
    /// <summary>Apple's implementation of IMA 4:1 ADPCM, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatAppleIMA4               = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("ima4");
    /// <summary>MPEG-4 Low Complexity AAC audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC                = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aac ");
    /// <summary>MPEG-4 CELP audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4CELP               = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("celp");
    /// <summary>MPEG-4 HVXC audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4HVXC               = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("hvxc");
    /// <summary>MPEG-4 TwinVQ audio object type, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4TwinVQ             = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("twvq");
    /// <summary>MACE 3:1, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMACE3                   = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("MAC3");
    /// <summary>MACE 6:1, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMACE6                   = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("MAC6");
    /// <summary>µLaw 2:1, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatULaw                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("ulaw");
    /// <summary>aLaw 2:1, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatALaw                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("alaw");
    /// <summary>QDesign music, has no flags</summary>
    public static readonly AudioFormatID kAudioFormatQDesign                 = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("QDMC");
    /// <summary>QDesign2 music, has no flags</summary>
    public static readonly AudioFormatID kAudioFormatQDesign2                = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("QDM2");
    /// <summary>QUALCOMM PureVoice, has no flags</summary>
    public static readonly AudioFormatID kAudioFormatQUALCOMM                = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("Qclp");
    /// <summary>MPEG-1/2, Layer 1 audio, has no flags</summary>
    public static readonly AudioFormatID kAudioFormatMPEGLayer1              = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString(".mp1");
    /// <summary>MPEG-1/2, Layer 2 audio, has no flags</summary>
    public static readonly AudioFormatID kAudioFormatMPEGLayer2              = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString(".mp2");
    /// <summary>MPEG-1/2, Layer 3 audio, has no flags</summary>
    public static readonly AudioFormatID kAudioFormatMPEGLayer3              = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString(".mp3");
    /// <summary>
    /// A stream of IOAudioTimeStamps, uses the IOAudioTimeStamp 
    /// flags (see IOKit/audio/IOAudioTypes.h).
    /// </summary>
    public static readonly AudioFormatID kAudioFormatTimeCode                = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("time");
    /// <summary>
    /// A stream of MIDIPacketLists where the time stamps in the MIDIPacketList are
    /// sample offsets in the stream. The mSampleRate field is used to describe how
    /// time is passed in this kind of stream and an AudioUnit that receives or
    /// generates this stream can use this sample rate, the number of frames it is
    /// rendering and the sample offsets within the MIDIPacketList to define the
    /// time for any MIDI event within this list. It has no flags.
    /// </summary>
    public static readonly AudioFormatID kAudioFormatMIDIStream              = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("midi");
    /// <summary>
    /// A "side-chain" of Float32 data that can be fed or generated by an AudioUnit
    /// and is used to send a high density of parameter value control information.
    /// An AU will typically run a ParameterValueStream at either the sample rate of
    /// the AudioUnit's audio data, or some integer divisor of this (say a half or a
    /// third of the sample rate of the audio). The Sample Rate of the ASBD
    /// describes this relationship. It has no flags.
    /// </summary>
    public static readonly AudioFormatID kAudioFormatParameterValueStream    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("apvs");
    /// <summary>Apple Lossless, the flags indicate the bit depth of the source material.</summary>
    public static readonly AudioFormatID kAudioFormatAppleLossless           = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("alac");
    /// <summary>MPEG-4 High Efficiency AAC audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_HE             = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aach");
    /// <summary>MPEG-4 AAC Low Delay audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_LD             = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aacl");
    /// <summary>
    /// MPEG-4 AAC Enhanced Low Delay audio object, has no flags.
    /// This is the formatID of the base layer without the SBR extension. <br />
    /// See also <see cref="kAudioFormatMPEG4AAC_ELD_SBR"/>.
    /// </summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_ELD            = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aace");
    /// <summary>MPEG-4 AAC Enhanced Low Delay audio object with SBR extension layer, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_ELD_SBR        = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aacf");
    /// <summary></summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_ELD_V2         = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aacg");
    /// <summary>MPEG-4 High Efficiency AAC Version 2 audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_HE_V2          = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aacp");
    /// <summary>MPEG-4 Spatial Audio audio object, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEG4AAC_Spatial        = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aacs");
    /// <summary>MPEG-D Unified Speech and Audio Coding, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatMPEGD_USAC              = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("usac");
    /// <summary>The AMR Narrow Band speech codec.</summary>
    public static readonly AudioFormatID kAudioFormatAMR                     = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("samr");
    /// <summary>The AMR Wide Band speech codec.</summary>
    public static readonly AudioFormatID kAudioFormatAMR_WB                  = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("sawb");
    /// <summary>The format used for Audible audio books. It has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatAudible                 = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("AUDB");
    /// <summary>The iLBC narrow band speech codec. It has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatiLBC                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("ilbc");
    /// <summary>DVI/Intel IMA ADPCM - ACM code 17.</summary>
    public const AudioFormatID kAudioFormatDVIIntelIMA             = (AudioFormatID)0x6D730011;
    /// <summary>Microsoft GSM 6.10 - ACM code 49.</summary>
    public const AudioFormatID kAudioFormatMicrosoftGSM            = (AudioFormatID)0x6D730031;
    /// <summary>
    /// This format is defined by AES3-2003, and adopted into MXF and MPEG-2
    /// containers and SDTI transport streams with SMPTE specs 302M-2002 and
    /// 331M-2000. It has no flags.
    /// </summary>
    public static readonly AudioFormatID kAudioFormatAES3                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("aes3");
    /// <summary>Enhanced AC-3, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatEnhancedAC3             = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("ec-3");
    /// <summary>Free Lossless Audio Codec, the flags indicate the bit depth of the source material.</summary>
    public static readonly AudioFormatID kAudioFormatFLAC                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("flac");
    /// <summary>Opus codec, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatOpus                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("opus");
    /// <summary>Apple Positional Audio Codec, has no flags.</summary>
    public static readonly AudioFormatID kAudioFormatAPAC                    = (AudioFormatID)MacUtils.ConstructUIntConstantValueFromString("apac");
}