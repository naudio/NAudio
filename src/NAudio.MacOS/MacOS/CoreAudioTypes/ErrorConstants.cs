/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// General Audio error codes <br />
/// These are some of the error codes returned from the APIs found through Core Audio related frameworks.
/// </summary>
internal static class ErrorConstants /* OSStatus */
{
    /// <summary>Returned on success.</summary>
    public const int kAudio_NoError                    = 0;
    /// <summary>Unimplemented core routine.</summary>
    public const int kAudio_UnimplementedError         = -4;
    /// <summary>File not found.</summary>
    public const int kAudio_FileNotFoundError          = -43;
    /// <summary>
    /// File cannot be opened due to either file, directory, or sandbox permissions.
    /// </summary>
    public const int kAudio_FilePermissionError        = -54;
    /// <summary>
    /// File cannot be opened because too many files are already open.
    /// </summary>
    public const int kAudio_TooManyFilesOpenError      = -42;
    /// <summary>
    /// File cannot be opened because the specified path is malformed.
    /// </summary>
    public static readonly int kAudio_BadFilePathError = MacUtils.ConstructIntConstantValueFromString("!pth"); // '!pth', 561017960
    /// <summary>
    /// Error in user parameter list.
    /// </summary>
    public const int kAudio_ParamError                 = -50;
    /// <summary>
    /// Not enough room in heap zone.
    /// </summary>
    public const int kAudio_MemFullError               = -108;

    /// <summary>
    /// Provides the error message of the specified error code. <br />
    /// This method represents those error messages coming strictly from the Core Audio Types framework. <br />
    /// If the error code is outside the Core Audio Types framework codes, this function will return <see langword="null"/>.
    /// </summary>
    /// <param name="errorCode">The OSStatus error code to inspect.</param>
    /// <returns>A string describing the error message, or <see langword="null"/> if the error code is not Core Audio Types framework-specific.</returns>
    public static string GetErrorMessage(int errorCode)
    {
        if (errorCode == kAudio_NoError)
        {
            return "Success.";
        }
        else if (errorCode == kAudio_UnimplementedError)
        {
            return "Unimplemented core routine.";
        }
        else if (errorCode == kAudio_FileNotFoundError)
        {
            return "File not found.";
        }
        else if (errorCode == kAudio_FilePermissionError)
        {
            return "File cannot be opened due to either file, directory, or sandbox permissions.";
        }
        else if (errorCode == kAudio_TooManyFilesOpenError)
        {
            return "File cannot be opened because too many files are already open.";
        }
        else if (errorCode == kAudio_BadFilePathError)
        {
            return "File cannot be opened because the specified path is malformed.";
        }
        else if (errorCode == kAudio_ParamError)
        {
            return "Error in user parameter list.";
        }
        else if (errorCode == kAudio_MemFullError)
        {
            return "Not enough room in heap zone.";
        }
        else
        {
            return null;
        }
    }
}