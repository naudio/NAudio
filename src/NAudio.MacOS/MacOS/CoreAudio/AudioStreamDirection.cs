
namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the values that the <see cref="AudioStream.Direction"/> property can have.
/// </summary>
public enum AudioStreamDirection : uint
{
    /// <summary>
    /// The current <see cref="AudioStream"/> is an output stream.
    /// </summary>
    Output,
    /// <summary>
    /// The current <see cref="AudioStream"/> is an input stream.
    /// </summary>
    Input
}