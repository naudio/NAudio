using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Optional configuration for <see cref="WaveFileWriter"/>. All properties are
    /// <c>init</c>-only so the captured configuration is immutable for the lifetime
    /// of the writer.
    /// </summary>
    public sealed class WaveFileWriterOptions
    {
        /// <summary>
        /// When true, the writer reserves a 28-byte <c>JUNK</c> placeholder immediately after
        /// the RIFF/WAVE header so it can promote the file to RF64 at close time if the data
        /// chunk exceeds 4 GB. When false (default), attempts to write more than 4 GB of audio
        /// throw <see cref="System.ArgumentException"/>.
        /// </summary>
        public bool EnableRf64 { get; init; }

        /// <summary>
        /// Threshold (in bytes) at or above which the writer promotes the header to RF64,
        /// provided <see cref="EnableRf64"/> is true. Defaults to <see cref="uint.MaxValue"/>
        /// (4 GB − 1), the RIFF ceiling. Intended for tests that want to exercise promotion
        /// without writing 4 GB of audio; production callers should leave this at the default.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public long Rf64PromotionThreshold { get; init; } = uint.MaxValue;
    }
}
