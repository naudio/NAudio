
using System;
using System.Diagnostics.CodeAnalysis;

using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the exception type for errors
/// originating from the usage of the
/// Audio Converter Services API.
/// </summary>
public sealed class AudioConverterException : AudioToolboxException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="AudioConverterException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <param name="osStatus">The status code.</param>
    public AudioConverterException(int osStatus) : base(osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="AudioConverterException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public AudioConverterException([AllowNull] string message, int osStatus) : base(message, osStatus) { }

    /// <summary>
    /// Throws an appropriate exception for the specified status code. <br />
    /// Does not throw if the specified code is not an error
    /// </summary>
    /// <param name="osStatus">The OS status code to throw an exception for</param>
    /// <exception cref="ArgumentException" />
    /// <exception cref="ArgumentOutOfRangeException" />
    /// <exception cref="NotSupportedException" />
    /// <exception cref="AudioConverterException" />
    public static void ThrowIfError(int osStatus)
    {
        if (osStatus == 0) { return; }

        if (osStatus == AudioConverterErrors.kAudioConverterErr_BadPropertySizeError)
        {
            throw new ArgumentException("Bad property size provided.");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_FormatNotSupported)
        {
            throw new ArgumentException("Specified audio format is not supported");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_InputSampleRateOutOfRange)
        {
            throw new ArgumentOutOfRangeException(null, "The specified sample rate in the input audio format is out of range of valid values.");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_InvalidInputSize)
        {
            throw new ArgumentException("Invalid input size.");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_InvalidOutputSize)
        {
            throw new ArgumentException("Invalid output size.");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_OperationNotSupported)
        {
            throw new NotSupportedException("The specified operation is not supported.");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_OutputSampleRateOutOfRange)
        {
            throw new ArgumentOutOfRangeException(null, "The specified sample rate in the output audio format is out of range of valid values.");
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_PropertyNotSupported)
        {
            throw new AudioConverterException("The specified property is not supported by this converter object.", osStatus);
        }
        else if (osStatus == AudioConverterErrors.kAudioConverterErr_RequiresPacketDescriptionsError)
        {
            throw new AudioConverterException("This converter object requires the packet descriptions to have been initialized.", osStatus);
        }
        else
        {
            throw new AudioConverterException("Unspecified error.", osStatus);
        }
    }
}