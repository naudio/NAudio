namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Resampling algorithm used by <see cref="WdlResamplingSampleProvider"/>
/// </summary>
/// <remarks>
/// The sinc modes are best used with a source that always fills each read (a file reader, or a
/// <see cref="BufferedWaveProvider"/> with <see cref="BufferedWaveProvider.ReadFully"/> left on).
/// When a source returns fewer samples than requested, the resampler pads with zeros to finish the
/// read, and in sinc mode the last few output samples of that read are computed partly from the
/// padding, adding a small error at every short read.
/// </remarks>
public enum WdlResamplerQuality
{
    /// <summary>
    /// Linear interpolation followed by two cascaded IIR low-pass filters. This is the default.
    /// It is the cheapest mode and adds no latency, but it rolls off high frequencies (about
    /// -2 dB at 10 kHz and -20 dB at 18 kHz for 44.1 kHz to 48 kHz) and lets some aliasing
    /// and imaging through.
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
    /// Recommended for quality-sensitive work.
    /// </summary>
    Sinc64,
}
