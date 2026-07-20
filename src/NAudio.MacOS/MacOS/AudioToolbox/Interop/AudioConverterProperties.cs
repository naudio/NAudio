/*!
	@file		AudioConverter.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
    @abstract   API's to perform audio format conversions.
    
	AudioConverters convert between various linear PCM and compressed
	audio formats. Supported transformations include:

	- PCM float/integer/bit depth conversions
	- PCM sample rate conversion
	- PCM interleaving and deinterleaving
	- encoding PCM to compressed formats
	- decoding compressed formats to PCM

	A single AudioConverter may perform more than one
	of the above transformations.
*/

using System.Runtime.Versioning;
using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox.Interop;

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

/// <summary>
/// Provides the property values for configuring audio converters.
/// </summary>
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("macos")]
internal static class AudioConverterProperties
{
    /// <summary>
    /// a UInt32 that indicates the size in bytes of the smallest buffer of input
    /// data that can be supplied via the AudioConverterInputProc or as the input to
    /// AudioConverterConvertBuffer
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyMinimumInputBufferSize       = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("mibs"u8);
    /// <summary>
    /// a UInt32 that indicates the size in bytes of the smallest buffer of output
    /// data that can be supplied to AudioConverterFillComplexBuffer or as the output to
    /// AudioConverterConvertBuffer
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyMinimumOutputBufferSize      = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("mobs"u8);
    /// <summary>
    /// a UInt32 that indicates the size in bytes of the largest single packet of
    /// data in the input format. This is mostly useful for variable bit rate
    /// compressed data (decoders).
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyMaximumInputPacketSize       = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("xips"u8);
    /// <summary>
    /// a UInt32 that indicates the size in bytes of the largest single packet of
    /// data in the output format. This is mostly useful for variable bit rate
    /// compressed data (encoders).
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyMaximumOutputPacketSize      = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("xops"u8);
    /// <summary>
    /// a UInt32 that on input holds a size in bytes that is desired for the output
    /// data. On output, it will hold the size in bytes of the input buffer required
    /// to generate that much output data. Note that some converters cannot do this
    /// calculation.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyCalculateInputBufferSize     = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("cibs"u8);
    /// <summary>
    /// a UInt32 that on input holds a size in bytes that is desired for the input
    /// data. On output, it will hold the size in bytes of the output buffer
    /// required to hold the output data that will be generated. Note that some
    /// converters cannot do this calculation.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyCalculateOutputBufferSize    = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("cobs"u8);
    /// <summary>
    /// The value of this property varies from format to format and is considered
    /// private to the format. It is treated as a buffer of untyped data.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyInputCodecParameters         = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("icdp"u8);
    /// <summary>
    /// The value of this property varies from format to format and is considered
    /// private to the format. It is treated as a buffer of untyped data.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyOutputCodecParameters        = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("ocdp"u8);
    /// <summary>
    /// An OSType that specifies the sample rate converter algorithm to use (as defined in AudioToolbox/AudioUnitProperties.h)
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterSampleRateConverterComplexity        = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("srca"u8);
    /// <summary>
    /// A UInt32 that specifies rendering quality of the sample rate converter (see enum constants below)
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterSampleRateConverterQuality           = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("srcq"u8);
    /// <summary>
    /// A Float64 with value 0.0 &lt;= x &lt; 1.0 giving the initial subsample position of the sample rate converter.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterSampleRateConverterInitialPhase      = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("srcp"u8);
    /// <summary>
    /// A UInt32 that specifies rendering quality of a codec (see enum constants below)
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterCodecQuality                         = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("cdqu"u8);
    /// <summary>
    /// a UInt32 specifying priming method (usually for sample-rate converter) see
    /// explanation for struct AudioConverterPrimeInfo below along with enum
    /// constants
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPrimeMethod                          = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("prmm"u8);
    /// <summary>
    /// A pointer to AudioConverterPrimeInfo (see explanation for struct udioConverterPrimeInfo below)
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPrimeInfo                            = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("prim"u8);
    /// <summary>
    /// An array of SInt32's.  The size of the array is the number of output
    /// channels, and each element specifies which input channel's data is routed to
    /// that output channel (using a 0-based index of the input channels), or -1 if
    /// no input channel is to be routed to that output channel.  The default
    /// behavior is as follows. I = number of input channels, O = number of output
    /// channels. When I > O, the first O inputs are routed to the first O outputs,
    /// and the remaining puts discarded.  When O > I, the first I inputs are routed
    /// to the first O outputs, and the remaining outputs are zeroed. <br /> <br />
    /// 
    /// A simple example for splitting mono input to stereo output (instead of routing
    /// the input to only the first output channel): 
    /// 
	/// <code>
	/// // this should be as large as the number of output channels:
	/// SInt32 channelMap[2] = { 0, 0 };
	/// AudioConverterSetProperty(theConverter, kAudioConverterChannelMap,
	/// sizeof(channelMap), channelMap);
	/// </code>
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterChannelMap                           = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("chmp"u8);
    /// <summary>
    /// A <c>void*</c> pointing to memory set up by the caller. 
    /// Required by some formats n order to decompress the input data.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterDecompressionMagicCookie             = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("dmgc"u8);
    /// <summary>
    /// A <c>void*</c> pointing to memory set up by the caller. Returned by the converter
    /// so that it may be stored along with the output data. It can then be passed
    /// back to the converter for decompression at a later time.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterCompressionMagicCookie               = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("cmgc"u8);
    /// <summary>
    /// A UInt32 containing the number of bits per second to aim for when encoding
    /// data. Some decoders will also allow you to get this property to discover the bit rate.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterEncodeBitRate                        = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("brat"u8);
    /// <summary>
    /// For encoders where the AudioConverter was created with an output sample rate
    /// of zero, and the codec can do rate conversion on its input, this provides a
    /// way to set the output sample rate. The property value is a <see cref="double"/>.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterEncodeAdjustableSampleRate           = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("ajsr"u8);
    /// <summary>
    /// The property value is an <see cref="CoreAudioTypes.AudioChannelLayout"/>.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterInputChannelLayout                   = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("icl "u8);
    /// <summary>
    /// The property value is an <see cref="CoreAudioTypes.AudioChannelLayout"/>.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterOutputChannelLayout                  = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("ocl "u8);
    /// <summary>
    /// The property value is an array of AudioValueRange describing applicable bit
    /// rates based on current settings.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterApplicableEncodeBitRates             = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("aebr"u8);
    /// <summary>
    /// The property value is an array of AudioValueRange describing available bit
    /// rates based on the input format. You can get all available bit rates from
    /// the AudioFormat API.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterAvailableEncodeBitRates              = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("vebr"u8);
    /// <summary>
    /// The property value is an array of AudioValueRange describing applicable
    /// sample rates based on current settings.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterApplicableEncodeSampleRates          = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("aesr"u8);
    /// <summary>
    /// The property value is an array of AudioChannelLayoutTags for the format and
    /// number of channels specified in the input format going to the encoder.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterAvailableEncodeSampleRates           = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("vesr"u8);
    /// <summary>
    /// The property value is an array of AudioChannelLayoutTags for the format and
    /// number of channels specified in the input format going to the encoder.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterAvailableEncodeChannelLayoutTags     = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("aecl"u8);
    /// <summary>
    /// Returns the current completely specified output AudioStreamBasicDescription.
    /// For example when encoding to AAC, your original output stream description
    /// will not have been completely filled out.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterCurrentOutputStreamDescription       = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("acod"u8);
    /// <summary>
    /// Returns the current completely specified input AudioStreamBasicDescription.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterCurrentInputStreamDescription        = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("acid"u8);
    /// <summary>
    /// Returns the a CFArray of property settings for converters.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertySettings                     = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("acps"u8);
    /// <summary>
    /// An SInt32 of the source bit depth to preserve. This is a hint to some
    /// encoders like lossless about how many bits to preserve in the input. The
    /// converter usually tries to preserve as many as possible, but a lossless
    /// encoder will do poorly if more bits are supplied than are desired in the
    /// output. The bit depth is expressed as a negative number if the source was floating point,
    /// e.g. -32 for float, -64 for double.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyBitDepthHint                 = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("acbd"u8);
    /// <summary>
    /// An array of AudioFormatListItem structs describing all the data formats produced by the
    /// encoder end of the AudioConverter. If the ioPropertyDataSize parameter indicates that
    /// outPropertyData is sizeof(AudioFormatListItem), then only the best format is returned.
    /// This property may be used for example to discover all the data formats produced by the AAC_HE2
    /// (AAC High Efficiency vers. 2) encoder.
    /// </summary>
    public static readonly AudioConverterPropertyID kAudioConverterPropertyFormatList                   = (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("flst"u8);
    /// <summary>
    /// A UInt32. Set to a value from the enum of dithering algorithms below. 
	/// Zero means no dithering and is the default. (macOS only.)
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    public static readonly AudioConverterPropertyID kAudioConverterPropertyDithering					= (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("dith"u8);
	/// <summary>
    /// A UInt32. Dither is applied at this bit depth.  (macOS only.)
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    public static readonly AudioConverterPropertyID kAudioConverterPropertyDitherBitDepth				= (AudioConverterPropertyID)MacUtils.ConstructUIntConstantValueFromString("dbit"u8);
}