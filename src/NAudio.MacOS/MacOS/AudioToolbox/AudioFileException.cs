
using System;
using System.Diagnostics.CodeAnalysis;

using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the base exception type for exceptions originating from the usage of the 
/// Audio File Services API.
/// </summary>
public class AudioFileException : AudioToolboxException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="AudioFileException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <param name="osStatus">The status code.</param>
    public AudioFileException(int osStatus) : base(osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="AudioFileException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public AudioFileException([AllowNull] string message, int osStatus) : base(message, osStatus) { }

    internal static void ThrowIfError(int osStatus, out bool couldNotMapException)
    {
        if (osStatus == 0) { couldNotMapException = false; return; }

        if (osStatus == AudioFileErrors.kAudioFileUnspecifiedError)
        {
            throw new AudioFileException("An unspecified error was occurred.", osStatus);
        }
        else if (osStatus == AudioFileErrors.kAudioFileUnsupportedFileTypeError)
        {
            throw new NotSupportedException("The specified file type is not supported.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileUnsupportedDataFormatError)
        {
            throw new NotSupportedException("The specified data format is not supported.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileUnsupportedPropertyError)
        {
            throw new NotSupportedException("The specified property is unsupported.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileBadPropertySizeError)
        {
            throw new AudioFileException("The specified size of the property is invalid.", osStatus);
        }
        else if (osStatus == AudioFileErrors.kAudioFilePermissionsError)
        {
            throw new UnauthorizedAccessException("The operation violated the file permissions.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileNotOptimizedError)
        {
            throw new InvalidOperationException("The file has not been optimized.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileInvalidChunkError)
        {
            throw new InvalidOperationException("The chunk does not exist in the file or is not supported by the file.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileDoesNotAllow64BitDataSizeError)
        {
            throw new OverflowException("The specified file offset was too large for the file type. AIFF and WAVE have a 32 bit file size limit. ");
        }
        else if (osStatus == AudioFileErrors.kAudioFileInvalidPacketOffsetError)
        {
            throw new InvalidOperationException("A packet offset was past the end of the file, " +
            "or not at the end of the file when writing a VBR format, or a corrupt packet size was " +
            "read when building the packet table.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileInvalidPacketDependencyError)
        {
            throw new InvalidOperationException("Either the packet dependency info that's necessary for the audio format has not been provided," +
                "or the provided packet dependency info indicates dependency on a packet that's unavailable.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileInvalidFileError)
        {
            throw new System.IO.InvalidDataException("The file is malformed, or otherwise not a valid instance of an audio file of its type. ");
        }
        else if (osStatus == AudioFileErrors.kAudioFileOperationNotSupportedError)
        {
            throw new NotSupportedException("The operation cannot be performed.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileNotOpenError)
        {
            throw new ObjectDisposedException(null, "The file is closed.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileEndOfFileError)
        {
            throw new System.IO.EndOfStreamException("End of file.");
        }
        else if (osStatus == AudioFileErrors.kAudioFilePositionError)
        {
            throw new InvalidOperationException("Invalid file position.");
        }
        else if (osStatus == AudioFileErrors.kAudioFileFileNotFoundError)
        {
            throw new System.IO.FileNotFoundException("File not found.");
        }
        else
        {
            couldNotMapException = true;
        }
    }

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
    public static void ThrowIfError(int osStatus)
    {
        ThrowIfError(osStatus, out bool exceptionNotMapped);
        if (exceptionNotMapped)
        {
            throw new AudioFileException("Unknown error occurred.", osStatus);
        }
    }
}