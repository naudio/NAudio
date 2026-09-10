
namespace NAudio.Wave;

/// <summary>
/// Extension interface for <see cref="IWavePlayer"/> implementations that can report position.
/// </summary>
public interface IWavePosition
{
    /// <summary>
    /// Position (in terms of bytes played - does not necessarily translate directly to the position within the source audio file)
    /// </summary>
    /// <returns>Position in bytes</returns>
    long GetPosition();

    /// <summary>
    /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
    /// </summary>
    WaveFormat OutputWaveFormat { get; }
}
