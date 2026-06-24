namespace NAudio.Wave.Compression
{
    /// <summary>
    /// ACM Format
    /// </summary>
    public class AcmFormat
    {
        private readonly AcmFormatDetails formatDetails;

        internal AcmFormat(AcmFormatDetails formatDetails)
        {
            this.formatDetails = formatDetails;
            WaveFormat = WaveFormat.MarshalFromPtr(formatDetails.waveFormatPointer);
        }

        /// <summary>
        /// Format Index
        /// </summary>
        public int FormatIndex => formatDetails.formatIndex;

        /// <summary>
        /// Format Tag
        /// </summary>
        public WaveFormatEncoding FormatTag => (WaveFormatEncoding)formatDetails.formatTag;

        /// <summary>
        /// Support Flags
        /// </summary>
        public AcmDriverDetailsSupportFlags SupportFlags => formatDetails.supportFlags;

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// WaveFormat Size
        /// </summary>
        public int WaveFormatByteSize => formatDetails.waveFormatByteSize;

        /// <summary>
        /// Format Description
        /// </summary>
        public string FormatDescription => formatDetails.formatDescription;
    }
}
