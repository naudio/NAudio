
using System;

namespace NAudio.Wave;

/// <summary>
/// Like the <see cref="IWaveProvider"/> interface, the <see cref="ISampleProvider"/> interface is 
/// also a low-level primitive that is an audio source, but all the audio samples are provided
/// as 32-bit floating-point. <br />
/// <b>Why to use <see cref="ISampleProvider"/> instead of <see cref="IWaveProvider"/>?</b> <br /> <br />
///
/// While <see cref="IWaveProvider"/> gives you the raw audio data as is, you cannot 
/// just 'magically' read the samples. That happens because the bit depth is typically different on
/// each audio source, for example you may see a source that provides 8 bits per sample, while other
/// one provides 24 bits per sample. <br /> <br />
///
/// As such, even creating a simple gain effect can prove extremely difficult as you must literally
/// implement 5 different Read helpers to implement the rather simple effect for 8, 16, 24, 32 and 64 bit depth sources,
/// and this is not all the possible bit depths that a source may be into, because there can also be bit depths of 10, or 12,
/// which they are not powers of two and as such it becomes even worse to decode them. <br /> <br />
///
/// So, the <see cref="ISampleProvider"/> interface takes that pain away, and instead, it always gives you
/// 32-bit floating-point samples that are really much easier to work with for any effect that you want to create.
/// It is also very useful if you want to implement a metering control or something else that analyzes the samples
/// as you are getting them.
/// To be noted, the <see cref="WaveExtensionMethods"/> class provides extension methods to many
/// converters that make the conversion of an <see cref="IWaveProvider"/> 
/// to an <see cref="ISampleProvider"/> look effortless, and vice-versa. <br /> <br />
/// 
/// Using floating-point values to represent the sample values allow to: <br />
/// <list type="bullet">
///     <item>Cleanly separate each channel. Each <see cref="float"/> value in the buffer is a sample for a single channel.</item>
///     <item>Do complex floating-point arithmetic operations without the restrictions that integers do have.</item>
///     <item>
///         Provide several performance optimizations in several cases, such as using tensor primitive operations.
///         The <see cref="SampleProviders.VolumeSampleProvider"/> uses this to implement the gain.
///     </item>
/// </list>
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
