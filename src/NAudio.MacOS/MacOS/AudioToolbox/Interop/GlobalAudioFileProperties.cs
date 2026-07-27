/*!
	@file		AudioFile.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
	@abstract	API's to read and write audio files in the filesystem or in memory.
*/

using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox.Interop;

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

/// <summary>
/// constants for AudioFileGetGlobalInfo properties
/// </summary>
internal static class GlobalAudioFileProperties
{
    /// <summary>
    /// No specifier needed. Must be set to NULL.
	/// Returns an array of UInt32 containing the file types 
	/// (i.e. AIFF, WAVE, etc) that can be opened for reading.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_ReadableTypes                        = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("afrf");
    /// <summary>
    /// No specifier needed. Must be set to NULL.
	/// Returns an array of UInt32 containing the file types 
	/// (i.e. AIFF, WAVE, etc) that can be opened for writing.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_WritableTypes                        = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("afwf");
    /// <summary>
    /// Specifier is a pointer to a AudioFileTypeID containing a file type.
	/// Returns a CFString containing the name for the file type. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_FileTypeName                         = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("ftnm");
    /// <summary>
    /// Specifier is a pointer to a <see cref="AudioFileTypeAndFormatID"/> struct.
	/// Returns an array of AudioStreamBasicDescriptions which have all of the 
	/// formats for a particular file type and format ID. The AudioStreamBasicDescriptions
	/// have the following fields filled in: mFormatID, mFormatFlags, mBitsPerChannel
	/// writing new files.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_AvailableStreamDescriptionsForFormat = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("sdid");
    /// <summary>
    /// Specifier is a pointer to a AudioFileTypeID containing a file type.
	/// Returns a array of format IDs for formats that can be read. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_AvailableFormatIDs                   = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("fmid");

    /// <summary>
    /// No specifier needed. Must be set to NULL.
	/// Returns a CFArray of CFStrings containing all file extensions 
	/// that are recognized. The array be used when creating an NSOpenPanel.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_AllExtensions                        = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("alxt");
    /// <summary>
    /// No specifier needed. Must be set to NULL.
	/// Returns an array of HFSTypeCode's containing 
    /// all HFSTypeCodes that are recognized.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_AllHFSTypeCodes                      = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("ahfs");
    /// <summary>
    /// No specifier needed. Must be set to NULL.
	/// Returns a CFArray of CFString of all Universal Type Identifiers
	/// that are recognized by AudioFile. 
	/// The caller is responsible for releasing the CFArray.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_AllUTIs                              = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("auti");
    /// <summary>
    /// No specifier needed. Must be set to NULL.
	/// Returns a CFArray of CFString of all MIME types
	/// that are recognized by AudioFile. 
	/// The caller is responsible for releasing the CFArray.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_AllMIMETypes                         = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("amim");

    /// <summary>
    /// Specifier is a pointer to a AudioFileTypeID containing a file type.
	/// Returns a CFArray of CFStrings containing the file extensions 
	/// that are recognized for this file type. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_ExtensionsForType                    = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("fext");
    /// <summary>
    /// Specifier is a pointer to an AudioFileTypeID.
	/// Returns an array of HFSTypeCodes corresponding to that file type.
	/// The first type in the array is the preferred one for use when
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_HFSTypeCodesForType                  = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("fhfs");
    /// <summary>
    /// Specifier is a pointer to an AudioFileTypeID.
	/// Returns a CFArray of CFString of all Universal Type Identifiers
	/// that are recognized by the file type. 
	/// The caller is responsible for releasing the CFArray.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_UTIsForType                          = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("futi");
    /// <summary>
    /// Specifier is a pointer to an AudioFileTypeID.
	/// Returns a CFArray of CFString of all MIME types
	/// that are recognized by the file type. 
    /// The caller is responsible for releasing the CFArray.
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_MIMETypesForType                     = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("fmim");

    /// <summary>
    /// Specifier is a CFStringRef containing a MIME Type.
	/// Returns an array of all AudioFileTypeIDs that support the MIME type. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_TypesForMIMEType                     = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("tmim");
    /// <summary>
    /// Specifier is a CFStringRef containing a Universal Type Identifier.
    /// Returns an array of all AudioFileTypeIDs that support the UTI. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_TypesForUTI                          = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("tuti");
    /// <summary>
    /// Specifier is an HFSTypeCode.
	/// Returns an array of all AudioFileTypeIDs that support the HFSTypeCode. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_TypesForHFSTypeCode                  = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("thfs");
    /// <summary>
    /// Specifier is a CFStringRef containing a file extension.
	/// Returns an array of all AudioFileTypeIDs that support the extension. 
    /// </summary>
    public static readonly AudioFilePropertyID kAudioFileGlobalInfo_TypesForExtension                    = (AudioFilePropertyID)MacUtils.ConstructUIntConstantValueFromString("text");
}