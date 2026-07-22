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

#pragma warning disable IDE0055 // We want the error constants to have a consistent view.

namespace NAudio.MacOS.AudioToolbox.Interop;

internal static class ExtendedAudioFileErrors
{
    public const int kExtAudioFileError_InvalidProperty			 = -66561;
    public const int kExtAudioFileError_InvalidPropertySize		 = -66562;
    public const int kExtAudioFileError_NonPCMClientFormat		 = -66563;
    public const int kExtAudioFileError_InvalidChannelMap		 = -66564;	// number of channels doesn't match format
    public const int kExtAudioFileError_InvalidOperationOrder	 = -66565;
    public const int kExtAudioFileError_InvalidDataFormat		 = -66566;
    public const int kExtAudioFileError_MaxPacketSizeUnknown	 = -66567;
    public const int kExtAudioFileError_InvalidSeek				 = -66568;	// writing, or offset out of bounds
    public const int kExtAudioFileError_AsyncWriteTooLarge		 = -66569;
    public const int kExtAudioFileError_AsyncWriteBufferOverflow = -66570;	// an async write could not be completed in time
}