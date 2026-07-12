
using System;
using System.Diagnostics;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the base class for exceptions thrown by the Core Audio framework wrappers.
/// </summary>
public class CoreAudioException : MacException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="MacException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <remarks>This constructor variant will attempt to lookup the error message for the given status code, if possible.</remarks>
    /// <param name="osStatus">The status code.</param>
    public CoreAudioException(int osStatus) : base(ErrorConstants.GetErrorMessage(osStatus), osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="CoreAudioException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public CoreAudioException(string message, int osStatus) : base(message, osStatus) { }

    // Throws a more appropriate exception for the specified native status code.
    // Internal utility method.
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static void ThrowIfError(int osStatus)
    {
        if (osStatus == ErrorConstants.kAudioHardwareNoError) { return; }

        if (osStatus == ErrorConstants.kAudioHardwareIllegalOperationError)
        {
            throw new InvalidOperationException("The requested operation could not be completed.");
        }
        else if (osStatus == ErrorConstants.kAudioHardwareUnsupportedOperationError)
        {
            throw new NotSupportedException("The Audio Object does not support the requested operation.");
        }
        else if (osStatus == ErrorConstants.kAudioDevicePermissionsError)
        {
            throw new UnauthorizedAccessException("The requested operation could not be completed because the process does not have permission.");
        }
        else if (osStatus == CoreAudioTypes.ErrorConstants.kAudio_MemFullError)
        {
            throw new InsufficientMemoryException("Not enough room in heap zone.");
        }
        else if (osStatus == CoreAudioTypes.ErrorConstants.kAudio_ParamError)
        {
            throw new ArgumentException("Error in user parameter list.");
        }
        else if (osStatus == CoreAudioTypes.ErrorConstants.kAudio_UnimplementedError)
        {
            throw new NotImplementedException("Unimplemented core routine.");
        }
        else if (
            osStatus == CoreAudioTypes.ErrorConstants.kAudio_BadFilePathError ||
            osStatus == CoreAudioTypes.ErrorConstants.kAudio_FileNotFoundError ||
            osStatus == CoreAudioTypes.ErrorConstants.kAudio_FilePermissionError
        )
        {
            throw new System.IO.FileNotFoundException(CoreAudioTypes.ErrorConstants.GetErrorMessage(osStatus));
        }
        else
        {
            throw new CoreAudioException(osStatus);
        }
    }
}