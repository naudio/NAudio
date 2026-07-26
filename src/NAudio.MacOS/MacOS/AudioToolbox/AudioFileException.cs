
using System.Diagnostics.CodeAnalysis;

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
}