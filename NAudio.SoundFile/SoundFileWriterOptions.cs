namespace NAudio.SoundFile
{
    /// <summary>
    /// Optional configuration for a <see cref="SoundFileWriter"/>.
    /// </summary>
    /// <remarks>
    /// Compression quality is deliberately not part of the format value:
    /// libsndfile sets it <em>after</em> the file is opened via
    /// <c>sf_command</c>, so it lives here rather than on
    /// <see cref="SoundFileSubtype"/>.
    /// </remarks>
    public class SoundFileWriterOptions
    {
        /// <summary>
        /// The sample encoding stored in the file.
        /// <see cref="SoundFileSubtype.Default"/> (the default) lets the
        /// writer pick the natural subtype for the major format.
        /// </summary>
        public SoundFileSubtype Subtype { get; set; } = SoundFileSubtype.Default;

        /// <summary>
        /// VBR encoding quality for Vorbis/Opus, from 0.0 (smallest) to 1.0
        /// (best). When <c>null</c> the codec default is used. Applied via
        /// <c>SFC_SET_VBR_ENCODING_QUALITY</c> after the file is opened.
        /// </summary>
        public double? VbrQuality { get; set; }

        /// <summary>
        /// Compression level for FLAC, from 0.0 (fastest) to 1.0 (smallest).
        /// When <c>null</c> the codec default is used. Applied via
        /// <c>SFC_SET_COMPRESSION_LEVEL</c> after the file is opened.
        /// </summary>
        public double? CompressionLevel { get; set; }
    }
}
