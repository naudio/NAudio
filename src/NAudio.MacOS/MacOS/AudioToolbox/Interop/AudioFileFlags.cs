/*!
	@file		AudioFile.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
	@abstract	API's to read and write audio files in the filesystem or in memory.
*/

using System;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>These are flags that can be used with the CreateURL API call</summary>
[Flags]
internal enum AudioFileFlags : uint
{
    /// <summary>No flags defined.</summary>
    None = 0,
    /// <summary>
    /// If set, then the CreateURL call will erase the contents of an existing file
	/// If not set, then the CreateURL call will fail if the file already exists
    /// </summary>
    EraseFile = 1,
    /// <summary>
    /// Normally, newly created and optimized files will have padding added in order to page align 
	/// the data to 4KB boundaries. This makes reading the data more efficient. 
	/// When disk space is a concern, this flag can be set so that the padding will not be added.
    /// </summary>
	DontPageAlignAudioData = 2
}