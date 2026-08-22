// This interop definition was derived from the file AudioHardwareBase.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// The error constants unique to the HAL. <br />
/// These are the error constants that are unique to the HAL. <br />
/// Note that the HAL's functions can and will return other codes that are not listed here. 
/// While these constants give a general idea of what might have gone wrong during the execution of an API call,
/// if an API call returns anything other than public static readonly int kAudioHardwareNoError it is to be viewed as the same failure regardless of what constant is actually returned.
/// </summary>
internal static class ErrorConstants /* OSStatus */
{
    /// <summary>The function call completed successfully.</summary>
    public const int kAudioHardwareNoError                             = 0;
    /// <summary>The function call requires that the hardware be running but it isn't.</summary>
    public static readonly int kAudioHardwareNotRunningError           = MacUtils.ConstructIntConstantValueFromString("stop");
    /// <summary>The function call failed while doing something that doesn't provide any error messages.</summary>
    public static readonly int kAudioHardwareUnspecifiedError          = MacUtils.ConstructIntConstantValueFromString("what");
    /// <summary>The AudioObject doesn't know about the property at the given address.</summary>
    public static readonly int kAudioHardwareUnknownPropertyError      = MacUtils.ConstructIntConstantValueFromString("who?");
    /// <summary>An improperly sized buffer was provided when accessing the data of a property.</summary>
    public static readonly int kAudioHardwareBadPropertySizeError      = MacUtils.ConstructIntConstantValueFromString("!siz");
    /// <summary>The requested operation couldn't be completed.</summary>
    public static readonly int kAudioHardwareIllegalOperationError     = MacUtils.ConstructIntConstantValueFromString("nope");
    /// <summary>The AudioObjectID passed to the function doesn't map to a valid AudioObject.</summary>
    public static readonly int kAudioHardwareBadObjectError            = MacUtils.ConstructIntConstantValueFromString("!obj");
    /// <summary>The AudioObjectID passed to the function doesn't map to a valid AudioDevice.</summary>
    public static readonly int kAudioHardwareBadDeviceError            = MacUtils.ConstructIntConstantValueFromString("!dev");
    /// <summary>The AudioObjectID passed to the function doesn't map to a valid AudioStream.</summary>
    public static readonly int kAudioHardwareBadStreamError            = MacUtils.ConstructIntConstantValueFromString("!str");
    /// <summary>The AudioObject doesn't support the requested operation.</summary>
    public static readonly int kAudioHardwareUnsupportedOperationError = MacUtils.ConstructIntConstantValueFromString("unop");
    /// <summary>The AudioObject isn't ready to do the requested operation.</summary>
    public static readonly int kAudioHardwareNotReadyError             = MacUtils.ConstructIntConstantValueFromString("nrdy");
    /// <summary>The AudioStream doesn't support the requested format.</summary>
    public static readonly int kAudioDeviceUnsupportedFormatError      = MacUtils.ConstructIntConstantValueFromString("!dat");
    /// <summary>The requested operation can't be completed because the process doesn't have permission.</summary>
    public static readonly int kAudioDevicePermissionsError            = MacUtils.ConstructIntConstantValueFromString("!hog");

    /// <summary>
    /// Provides the error message of the specified error code. <br />
    /// This method represents those error messages coming strictly from the Core Audio framework. <br />
    /// If the error code is outside the Core Audio framework codes, this function will return <see langword="null"/>.
    /// </summary>
    /// <param name="errorCode">The OSStatus error code to inspect.</param>
    /// <returns>A string describing the error message, or <see langword="null"/> if the error code is not Core Audio framework-specific.</returns>
    public static string GetErrorMessage(int errorCode)
    {
        if (errorCode == kAudioHardwareNoError)
        {
            return "The function call completed successfully.";
        }
        else if (errorCode == kAudioHardwareNotRunningError)
        {
            return "The function call requires that the hardware be running but it isn't.";
        }
        else if (errorCode == kAudioHardwareUnspecifiedError)
        {
            return "The function call failed while doing something that doesn't provide any error messages.";
        }
        else if (errorCode == kAudioHardwareUnknownPropertyError)
        {
            return "The AudioObject doesn't know about the property at the given address.";
        }
        else if (errorCode == kAudioHardwareBadPropertySizeError)
        {
            return "An improperly sized buffer was provided when accessing the data of a property.";
        }
        else if (errorCode == kAudioHardwareIllegalOperationError)
        {
            return "The requested operation couldn't be completed.";
        }
        else if (errorCode == kAudioHardwareBadObjectError)
        {
            return "The AudioObjectID passed to the function doesn't map to a valid AudioObject.";
        }
        else if (errorCode == kAudioHardwareBadDeviceError)
        {
            return "The AudioObjectID passed to the function doesn't map to a valid AudioDevice.";
        }
        else if (errorCode == kAudioHardwareBadStreamError)
        {
            return "The AudioObjectID passed to the function doesn't map to a valid AudioStream.";
        }
        else if (errorCode == kAudioHardwareUnsupportedOperationError)
        {
            return "The AudioObject doesn't support the requested operation.";
        }
        else if (errorCode == kAudioHardwareNotReadyError)
        {
            return "The AudioObject isn't ready to do the requested operation.";
        }
        else if (errorCode == kAudioDeviceUnsupportedFormatError)
        {
            return "The AudioStream doesn't support the requested format.";
        }
        else if (errorCode == kAudioDevicePermissionsError)
        {
            return "The requested operation can't be completed because the process doesn't have permission.";
        }
        else
        {
            return CoreAudioTypes.ErrorConstants.GetErrorMessage(errorCode);
        }
    }
}