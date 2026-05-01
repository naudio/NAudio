
using NAudio.Wave;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.Utils
{
    /// <summary>
    /// Provides the signature for audio format changed events. <br />
    /// Currently used in the <see cref="AudioDataBuffer"/> class.
    /// </summary>
    /// <param name="format">The new audio format that is enforced. Should not be <see langword="null"/>.</param>
    public delegate void AudioFormatChangedDelegate([DisallowNull] IAudioFormat format);
}
