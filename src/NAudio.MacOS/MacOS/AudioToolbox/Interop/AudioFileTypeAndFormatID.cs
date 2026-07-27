/*!
	@file		AudioFile.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
	@abstract	API's to read and write audio files in the filesystem or in memory.
*/

using System.Runtime.InteropServices;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// This is used as a specifier for kAudioFileGlobalInfo_AvailableStreamDescriptions
/// </summary>
/// <remarks>
/// This struct is used to specify a desired audio file type and data format ID so
/// that a list of stream descriptions of available formats can be obtained.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioFileTypeAndFormatID
{
    /// <summary>
    /// a four char code for the file type such as kAudioFileAIFFType, kAudioFileCAFType, etc.
    /// </summary>
    public AudioFileTypeID mFileType;
    /// <summary>
    /// a four char code for the format ID such as kAudioFormatLinearPCM, kAudioFormatMPEG4AAC, etc.
    /// </summary>
    public AudioFormatID mFormatID;
}