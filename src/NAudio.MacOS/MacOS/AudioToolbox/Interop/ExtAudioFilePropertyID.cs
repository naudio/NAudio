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

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// Defines the type for accessing Extended Audio File Services properties.
/// </summary>
internal enum ExtAudioFilePropertyID : uint { }