
using System;

namespace NAudio.Wave;

/// <summary>
/// Like the <see cref="IWaveProvider"/> interface, the <see cref="ISampleProvider"/> interface is 
/// also a low-level primitive that is an audio source, but all the audio samples are provided
/// as 32-bit floating-point. <br />
/// This was primarily useful back in .NET Framework when the Span API still not existed and reinterpreting
/// samples as their numeric types needed loads of code in the Read implementation. For example, applying
/// just a gain (volume) modifier on PCM 16 bit rate audio would require the following code:
/// <code>
/// float gain = 0.7f;
/// IWaveProvider sourceProvider;
/// 
/// int Read(byte[] buffer, int offset, int count)
/// {
///     int read = sourceProvider.Read(buffer, offset, count);
///
///     for (int I = 0; I &lt; read; I += 2)
///     {
///         short sample = BitConverter.ToInt16(buffer, I + offset);
///         sample = (short)(sample * gain);
///         Array.Copy(BitConverter.GetBytes(sample), 0, buffer, I + offset, 2);
///     }
///
///     return read;
/// }
/// </code>
/// This example is only for 16-bit audio, and there are a lot of 
/// bit rates to be covered, for example 8/24/32/64 bit audio. <br />
/// Despite it's historical meaning, and the fact that the Span
/// API's are now existing, this interface is still relevant today as
/// just implementing gain (and any other audio effect) on all possible bit rates is impractical, 
/// and using floating-point values to represent the sample values allow to: <br />
/// <list type="bullet">
///     <item>Cleanly separate each channel. Each <see cref="float"/> value in the buffer is a sample for a single channel.</item>
///     <item>Do complex floating-point arithmetic operations without the restrictions that integers do have.</item>
///     <item>
///         Provide several performance optimizations in several cases, such as using tensor primitive operations.
///         The <see cref="SampleProviders.VolumeSampleProvider"/> uses this to implement the gain.
///     </item>
/// </list>
/// Note that the <see cref="WaveExtensionMethods"/> class provides extension methods that make it 
/// just as easy to convert an <see cref="IWaveProvider"/> to an <see cref="ISampleProvider"/>, and vice-versa.
/// </summary>
public interface ISampleProvider
{
    /// <summary>
    /// Gets the wave format of this sample provider.
    /// </summary>
    /// <returns>The wave format.</returns>
    /// <remarks>
    /// The audio format describes the wave data that are provided by successive <see cref="Read"/> calls. <br />
    /// This must always be an IEEE 32-bit floating-point format, which can be created with the
    /// <see cref="WaveFormat.CreateIeeeFloatWaveFormat(int, int)"/> helper method.
    /// </remarks>
    WaveFormat WaveFormat { get; }

    /// <summary>
    /// Fills the specified buffer with 32 bit floating point samples
    /// </summary>
    /// <param name="buffer">The buffer to fill with samples.</param>
    /// <returns>The number of samples written. Return 0 to signal end of stream.</returns>
    int Read(Span<float> buffer);
}
