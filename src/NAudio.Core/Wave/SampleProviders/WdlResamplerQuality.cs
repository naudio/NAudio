namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Resampling algorithm used by <see cref="WdlResamplingSampleProvider"/>
/// </summary>
/// <remarks>
/// The sinc modes need input from slightly ahead of each output sample (half the kernel). When a source
/// returns fewer samples than requested, <see cref="WdlResamplingSampleProvider"/> holds back the output
/// that still needs input the source hasn't delivered and returns it on a later read. Only when the
/// source returns nothing is the remaining output flushed, with silence standing in for the missing
/// input. So a source that ends the stream with a short read and then returns 0 (a file reader, for
/// example) gets its full tail, while an under-fed real-time source is not distorted by the padding.
/// </remarks>
public enum WdlResamplerQuality
{
    /// <summary>
    /// Linear interpolation followed by two cascaded IIR low-pass filters. The cheapest mode, and the
    /// only one before NAudio 3.3, but it rolls off high frequencies (about -2 dB at 10 kHz and
    /// -20 dB at 18 kHz for 44.1 kHz to 48 kHz) and lets some aliasing and imaging through.
    /// </summary>
    Linear,
    /// <summary>
    /// Windowed sinc interpolation with a 16-tap kernel. A cheaper middle ground: clean up to
    /// about 10 kHz, but rolls off the top of the audio band (about -0.8 dB at 18 kHz).
    /// </summary>
    Sinc16,
    /// <summary>
    /// Windowed sinc interpolation with a 64-tap kernel. Flat across the audio band with
    /// signal-to-error above 70 dB, for slightly more CPU and about 0.8 ms of added latency.
    /// This is the default.
    /// </summary>
    Sinc64,
}
