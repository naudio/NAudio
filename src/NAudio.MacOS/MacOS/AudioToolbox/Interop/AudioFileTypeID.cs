/*!
	@file		AudioFile.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
	@abstract	API's to read and write audio files in the filesystem or in memory.
*/

using NAudio.Utils;

#pragma warning disable IDE0055 // We want the audio file type ID constants to have a consistent view.

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>Identifier for an audio file type.</summary>
internal enum AudioFileTypeID : uint { }

/// <summary>Constants for the built-in audio file types.</summary>
/// <remarks>
/// These constants are used to indicate the type of file 
/// to be written, or as a hint to what type of file to 
/// expect from data provided.
/// </remarks>
internal static class AudioFileTypeIDs
{
    public static readonly AudioFileTypeID kAudioFileAIFFType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("AIFF");
    public static readonly AudioFileTypeID kAudioFileAIFCType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("AIFC");
    public static readonly AudioFileTypeID kAudioFileWAVEType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("WAVE");
    public static readonly AudioFileTypeID kAudioFileRF64Type               = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("RF64");
    public static readonly AudioFileTypeID kAudioFileBW64Type               = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("BW64");
    public static readonly AudioFileTypeID kAudioFileWave64Type             = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("W64f");
    public static readonly AudioFileTypeID kAudioFileSoundDesigner2Type	    = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("Sd2f");
    public static readonly AudioFileTypeID kAudioFileNextType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("NeXT");
    public static readonly AudioFileTypeID kAudioFileMP3Type				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("MPG3");	// mpeg layer 3
    public static readonly AudioFileTypeID kAudioFileMP2Type				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("MPG2");	// mpeg layer 2
    public static readonly AudioFileTypeID kAudioFileMP1Type				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("MPG1"); // mpeg layer 1
    public static readonly AudioFileTypeID kAudioFileAC3Type                = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("ac-3");
    public static readonly AudioFileTypeID kAudioFileAAC_ADTSType			= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("adts");
    public static readonly AudioFileTypeID kAudioFileMPEG4Type              = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("mp4f");
    public static readonly AudioFileTypeID kAudioFileM4AType                = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("m4af");
    public static readonly AudioFileTypeID kAudioFileM4BType                = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("m4bf");
    public static readonly AudioFileTypeID kAudioFileCAFType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("caff");
    public static readonly AudioFileTypeID kAudioFile3GPType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("3gpp");
    public static readonly AudioFileTypeID kAudioFile3GP2Type				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("3gp2");
    public static readonly AudioFileTypeID kAudioFileAMRType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("amrf");
    public static readonly AudioFileTypeID kAudioFileFLACType				= (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("flac");
    public static readonly AudioFileTypeID kAudioFileLATMInLOASType		    = (AudioFileTypeID)MacUtils.ConstructUIntConstantValueFromString("loas");
}