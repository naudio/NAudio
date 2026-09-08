
using System;
using System.Diagnostics.CodeAnalysis;

using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the exception type for errors
/// originating from the usage of the
/// Extended Audio File Services API.
/// </summary>
public class ExtendedAudioFileException : AudioFileException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="ExtendedAudioFileException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <param name="osStatus">The status code.</param>
    public ExtendedAudioFileException(int osStatus) : base(osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="ExtendedAudioFileException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public ExtendedAudioFileException([AllowNull] string message, int osStatus) : base(message, osStatus) { }

    /// <summary>
    /// Throws an appropriate exception for the given OS status code.
    /// </summary>
    /// <param name="osStatus">The OS status code to translate to an exception.</param>
    /// <exception cref="AudioFileException" />
    /// <exception cref="NotSupportedException" />
    /// <exception cref="UnauthorizedAccessException" />
    /// <exception cref="InvalidOperationException" />
    /// <exception cref="OverflowException" />
    /// <exception cref="System.IO.InvalidDataException" />
    /// <exception cref="ObjectDisposedException" />
    /// <exception cref="System.IO.EndOfStreamException" />
    /// <exception cref="System.IO.FileNotFoundException" />
    public static new void ThrowIfError(int osStatus)
    {
        ThrowIfError(osStatus, out bool exceptionNotMapped);

        if (exceptionNotMapped)
        {
            if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_InvalidProperty)
            {
                throw new InvalidOperationException("Invalid property.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_InvalidPropertySize)
            {
                throw new ArgumentOutOfRangeException(null, "Invalid buffer size for the selected property.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_NonPCMClientFormat)
            {
                throw new InvalidOperationException("The client format was not a PCM format.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_InvalidChannelMap)
            {
                throw new InvalidOperationException("The number of channels in the format does not match the number of channels specified in the channel map.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_InvalidOperationOrder)
            {
                throw new InvalidOperationException("Invalid operation order.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_InvalidDataFormat)
            {
                throw new InvalidOperationException("Invalid target data format.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_MaxPacketSizeUnknown)
            {
                throw new NotSupportedException("Max packet size is unknown.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_InvalidSeek)
            {
                throw new InvalidOperationException("Invalid seek position.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_AsyncWriteTooLarge)
            {
                throw new InvalidOperationException("The specified asyncronous write was too large.");
            }
            else if (osStatus == ExtendedAudioFileErrors.kExtAudioFileError_AsyncWriteBufferOverflow)
            {
                throw new TimeoutException("The specified asyncronous write took too long to be executed.");
            }
            else
            {
                throw new ExtendedAudioFileException("Unknown error occurred.", osStatus);
            }
        }
    }
}
