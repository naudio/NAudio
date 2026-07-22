/*!
	@file		AudioFile.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
	@abstract	API's to read and write audio files in the filesystem or in memory.
*/

using NAudio.Utils;

#pragma warning disable IDE0055 // We want the error constants to have a consistent view.

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// These are the error codes returned from the AudioFile API.
/// </summary>
internal static class AudioFileErrors
{
    /// <summary>An unspecified error has occurred.</summary>
    public static readonly int kAudioFileUnspecifiedError						= MacUtils.ConstructIntConstantValueFromString("wht?"u8);		// 0x7768743F, 2003334207
    /// <summary>The file type is not supported.</summary>
    public static readonly int kAudioFileUnsupportedFileTypeError 				= MacUtils.ConstructIntConstantValueFromString("typ?"u8);		// 0x7479703F, 1954115647
    /// <summary>The data format is not supported by this file type.</summary>
    public static readonly int kAudioFileUnsupportedDataFormatError 			= MacUtils.ConstructIntConstantValueFromString("fmt?"u8);		// 0x666D743F, 1718449215
    /// <summary>The property is not supported.</summary>
    public static readonly int kAudioFileUnsupportedPropertyError 				= MacUtils.ConstructIntConstantValueFromString("pty?"u8);		// 0x7074793F, 1886681407
    /// <summary>The size of the property data was not correct.</summary>
    public static readonly int kAudioFileBadPropertySizeError 					= MacUtils.ConstructIntConstantValueFromString("!siz"u8);		// 0x2173697A,  561211770
    /// <summary>
    /// The operation violated the file permissions.
    /// For example, trying to write to a file opened with kAudioFileReadPermission.
    /// </summary>
    public static readonly int kAudioFilePermissionsError	 					= MacUtils.ConstructIntConstantValueFromString("prm?"u8);		// 0x70726D3F, 1886547263
    /// <summary>
    /// There are chunks following the audio data chunk that prevent extending the audio data chunk. 
    /// The file must be optimized in order to write more audio data.
    /// </summary>
    public static readonly int kAudioFileNotOptimizedError						= MacUtils.ConstructIntConstantValueFromString("optm"u8);       // 0x6F70746D, 1869640813

    // file format specific error codes

    /// <summary>
    /// The chunk does not exist in the file or is not supported by the file. 
    /// </summary>
    public static readonly int kAudioFileInvalidChunkError						= MacUtils.ConstructIntConstantValueFromString("chk?"u8);		// 0x63686B3F, 1667787583
    /// <summary>
    /// The a file offset was too large for the file type. AIFF and WAVE have a 32 bit file size limit. 
    /// </summary>
    public static readonly int kAudioFileDoesNotAllow64BitDataSizeError		    = MacUtils.ConstructIntConstantValueFromString("off?"u8);		// 0x6F66663F, 1868981823
    /// <summary>
    /// A packet offset was past the end of the file, or not at the end 
    /// of the file when writing a VBR format, or a corrupt packet size 
    /// was read when building the packet table. 
    /// </summary>
    public static readonly int kAudioFileInvalidPacketOffsetError				= MacUtils.ConstructIntConstantValueFromString("pck?"u8);		// 0x70636B3F, 1885563711
    /// <summary>
    /// Either the packet dependency info that's necessary for the audio format has not been provided,
    /// or the provided packet dependency info indicates dependency on a packet that's unavailable.
    /// </summary>
    public static readonly int kAudioFileInvalidPacketDependencyError			= MacUtils.ConstructIntConstantValueFromString("dep?"u8);		// 0x6465703F, 1684369471
    /// <summary>
    /// The file is malformed, or otherwise not a valid instance of an audio file of its type. 
    /// </summary>
    public static readonly int kAudioFileInvalidFileError						= MacUtils.ConstructIntConstantValueFromString("dta?"u8);		// 0x6474613F, 1685348671
    /// <summary>
    /// The operation cannot be performed. For example, setting kAudioFilePropertyAudioDataByteCount to increase 
    /// the size of the audio data in a file is not a supported operation. Write the data instead.
    /// </summary>
    public static readonly int kAudioFileOperationNotSupportedError			    = MacUtils.ConstructIntConstantValueFromString("op??"u8);       // 0x6F703F3F, integer used because of trigraph

    // general file error codes

    /// <summary>The file is closed.</summary>
    public const int kAudioFileNotOpenError							            = -38;
    /// <summary>End of file.</summary>
    public const int kAudioFileEndOfFileError						            = -39;
    /// <summary>Invalid file position.</summary>
    public const int kAudioFilePositionError							        = -40; 
    /// <summary>File not found.</summary>
    public const int kAudioFileFileNotFoundError						        = -43;
}