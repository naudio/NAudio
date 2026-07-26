
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the base exception class for all the exceptions related to the Audio Toolbox framework. <br />
/// An instance of this class will NEVER be thrown explicitly by this library, and it is only
/// provided for identifying from which subsystem the exception has came from.
/// </summary>
public class AudioToolboxException : MacException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="AudioToolboxException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <param name="osStatus">The status code.</param>
    public AudioToolboxException(int osStatus) : base(osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="AudioToolboxException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public AudioToolboxException([AllowNull] string message, int osStatus) : base(message, osStatus) { }
}