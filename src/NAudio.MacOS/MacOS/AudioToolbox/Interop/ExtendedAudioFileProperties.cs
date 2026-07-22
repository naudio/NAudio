/*!
	@file		ExtendedAudioFile.h
	@framework	AudioToolbox.framework
	@copyright	(c) 2004-2015 by Apple, Inc., all rights reserved.
	@abstract	API's to support reading and writing files in encoded audio formats.

	@discussion

				The ExtendedAudioFile provides high-level audio file access, building
				on top of the AudioFile and AudioConverter API sets. It provides a single
				unified interface to reading and writing both encoded and unencoded files.
*/

using System.Runtime.Versioning;

using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox.Interop;

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

/// <summary>
/// Provides the property constant values for 
/// Extended Audio File Services.
/// </summary>
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("macos")]
internal static class ExtendedAudioFileProperties
{
    /// <summary>
    /// An AudioStreamBasicDescription. 
    /// Represents the file's actual data format. 
    /// Read-only.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_FileDataFormat		    = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("ffmt"u8);   // AudioStreamBasicDescription
	/// <summary>
    /// An AudioChannelLayout. <br /> <br />
    ///
	/// If writing: the channel layout is written to the file, if the format
	/// supports the layout. If the format does not support the layout, the channel
	///	layout is still interpreted as the destination layout when performing
	///	conversion from the client channel layout, if any. <br /> <br />
    /// 
    ///	If reading: the specified layout overrides the one read from the file, if
	///	any. <br /> <br />
    /// 
	///	When setting this, it must be set before the client format or channel
	///	layout.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_FileChannelLayout		= (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("fclo"u8);   // AudioChannelLayout
	/// <summary>
    /// An AudioStreamBasicDescription. <br /> <br />
    /// 
	///	The format must be linear PCM (kAudioFormatLinearPCM).
    /// 
	///	You must set this in order to encode or decode a non-PCM file data format.
	///	You may set this on PCM files to specify the data format used in your calls
	///	to read/write.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_ClientDataFormat		= (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("cfmt"u8);   // AudioStreamBasicDescription
	/// <summary>
    /// An AudioChannelLayout. Specifies the channel layout of the
	///	AudioBufferList's passed to ExtAudioFileRead() and
	///	ExtAudioFileWrite(). The layout may be different from the file's
	///	channel layout, in which the ExtAudioFileRef's underlying AudioConverter
	///	performs the remapping. This must be set after ClientDataFormat, and the
	///	number of channels in the layout must match.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_ClientChannelLayout	    = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("cclo"u8);   // AudioChannelLayout
	/// <summary>
    /// A UInt32 specifying the manufacturer of the codec to be used. This must be 
	///	specified before setting kExtAudioFileProperty_ClientDataFormat, which
	///	triggers the creation of the codec. This can be used on iOS
	///	to choose between a hardware or software encoder, by specifying 
	///	kAppleHardwareAudioCodecManufacturer or kAppleSoftwareAudioCodecManufacturer.
    /// 
	///	Available starting on macOS version 10.7 and iOS version 4.0.
    /// </summary>
    [SupportedOSPlatform("ios4.0")]
    [SupportedOSPlatform("macos10.7")]
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_CodecManufacturer		= (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("cman"u8);	// UInt32

    /// <summary>
    /// AudioConverterRef. The underlying AudioConverterRef, if any. Read-only.
	///
	///	Note: If you alter any properties of the AudioConverterRef, for example,
	///	an encoder's bit rate, you must set the kExtAudioFileProperty_ConverterConfig
	///	property on the ExtAudioFileRef afterwards. A NULL configuration is 
	///	sufficient. This will ensure that the output file's data format is consistent
	///	with the format being produced by the converter.
	///	
	///	<code>
    /// CFArrayRef config = NULL;
	///	err = ExtAudioFileSetProperty(myExtAF, kExtAudioFileProperty_ConverterConfig,
	///	sizeof(config), &amp;config);
    /// </code>
    /// </summary>
	// read-only:
	public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_AudioConverter		    = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("acnv"u8);	// AudioConverterRef
	/// <summary>
    /// The underlying AudioFileID. Read-only.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_AudioFile				= (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("afil"u8);	// AudioFileID
	/// <summary>
    /// UInt32 representing the file data format's maximum packet size in bytes.
	/// Read-only.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_FileMaxPacketSize		= (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("fmps"u8);	// UInt32
	/// <summary>
    /// UInt32 representing the client data format's maximum packet size in bytes.
	/// Read-only.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_ClientMaxPacketSize	    = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("cmps"u8);	// UInt32
	/// <summary>
    /// SInt64 representing the file's length in sample frames. Read-only on 
	/// non-PCM formats; writable for files in PCM formats.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_FileLengthFrames		= (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("#frm"u8);	// SInt64
	
    /// <summary>
    /// CFArrayRef representing the underlying AudioConverter's configuration, as
	/// specified by kAudioConverterPropertySettings.
    /// 
	/// This may be NULL to force resynchronization of the converter's output format
	/// with the file's data format.
    /// </summary>
	// writable:
	public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_ConverterConfig         = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("accf"u8);   // CFPropertyListRef
	/// <summary>
    /// UInt32 representing the size of the buffer through which the converter
	/// reads/writes the audio file (when there is an AudioConverter).
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_IOBufferSizeBytes       = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("iobs"u8);	// UInt32
	/// <summary>
    /// void *. This is the memory buffer which the ExtAudioFileRef will use for
	/// disk I/O when there is a conversion between the client and file data
	/// formats. A client may be able to share buffers between multiple
	/// ExtAudioFileRef instances, in which case, it can set this property to point
	/// to its own buffer. After setting this property, the client must
	/// subsequently set the kExtAudioFileProperty_IOBufferSizeBytes property. Note
	/// that a pointer to a pointer should be passed to ExtAudioFileSetProperty.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_IOBuffer                = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("iobf"u8);	// void *
	/// <summary>
    /// This AudioFilePacketTableInfo can be used both to override the priming and
	/// remainder information in an audio file and to retrieve the current priming
    /// and remainder frames information for a given ExtAudioFile object. If the
    /// underlying file type does not provide packet table info, the Get call will
    /// return an error.
    /// 
    /// If you set this, then you can override the setting for these values in the
    /// file to ones that you want to use. When setting this, you can use
    /// kExtAudioFilePacketTableInfoOverride_UseFileValue (-1) for either the
    /// priming or remainder frames to signal that the value currently stored in
    /// the file should be used. If you set this to a non-negative number (or zero)
    /// then that value will override whatever value is stored in the file that
    /// you are reading. Retrieving the value of the property will always retrieve
    /// the value the ExtAudioFile object is using (whether this is derived from
    /// the file, or from your override). If you want to determine what the value
    /// is in the file, you should use the AudioFile property:
    /// kAudioFilePropertyPacketTableInfo
    /// 
    /// If the value of mNumberValidFrames is positive, it will be used to override
    /// the count of valid frames stored in the file. If you wish to override only
    /// the priming and remainder frame values, you should set mNumberValidFrames
    /// to zero.
    /// 
    /// For example, a file encoded using AAC may have 2112 samples of priming at
    /// the start of the file and a remainder of 823 samples at the end. When
    /// ExtAudioFile returns decoded samples to you, it will trim `mPrimingFrames`
    /// at the start of the file, and `mRemainderFrames` at the end. It will get
    /// these numbers initially from the file. A common use case for overriding this
    /// would be to set the priming and remainder samples to 0, so in this example
    /// you would retrieve an additional 2112 samples of silence from the start of
    /// the file and 823 samples of silence at the end of the file (silence, because
    /// the encoders use silence to pad out these priming and remainder samples)
    /// 
    /// A value of kExtAudioFilePacketTableInfoOverride_UseFileValueIfValid (-2)
    /// for priming, remainder, or valid frames will cause the corresponding value
    /// stored in the file to be used if the total number of frames produced by the
    /// file matches the total frames accounted for by the packet table info stored
    /// in the file. If these do not match, for priming or remainder frames a value
    /// of 0 will be used instead, and for valid frames a value will be calculated
    /// that causes the total frames accounted for by the overriding packet table
    /// info to match the count of frames produced by the file.
    /// </summary>
    public static readonly ExtAudioFilePropertyID kExtAudioFileProperty_PacketTable             = (ExtAudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("xpti"u8);	// AudioFilePacketTableInfo
}