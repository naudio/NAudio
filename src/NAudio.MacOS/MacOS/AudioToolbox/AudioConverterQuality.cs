// This interop definition was derived from the file AudioConverter.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Quality constants for audio converters <br />
/// Constants to be used with <see cref="Wave.MacAudioConverter.Quality"/> property.
/// </summary>
public enum AudioConverterQuality : uint
{
    /// <summary>maximum quality</summary>
    Max = 0x7F,
    /// <summary>high quality</summary>
    High = 0x60,
    /// <summary>medium quality</summary>
    Medium = 0x40,
    /// <summary>low quality</summary>
    Low = 0x20,
    /// <summary>minimum quality</summary>
    Min = 0
}