namespace NAudio.Wave.Compression
{
    /// <summary>
    /// ACM Format Tag
    /// </summary>
    public class AcmFormatTag
    {
        private AcmFormatTagDetails formatTagDetails;

        internal AcmFormatTag(AcmFormatTagDetails formatTagDetails)
        {
            this.formatTagDetails = formatTagDetails;
        }

        /// <summary>
        /// Format Tag Index
        /// </summary>
        public int FormatTagIndex
        {
            get { return formatTagDetails.formatTagIndex; }
        }

        /// <summary>
        /// Format Tag
        /// </summary>
        public WaveFormatEncoding FormatTag
        {
            get { return (WaveFormatEncoding)formatTagDetails.formatTag; }
        }

        /// <summary>
        /// Format Size
        /// </summary>
        public int FormatSize
        {
            get { return formatTagDetails.formatSize; }
        }

        /// <summary>
        /// Support Flags
        /// </summary>
        public AcmDriverDetailsSupportFlags SupportFlags
        {
            get { return formatTagDetails.supportFlags; }
        }

        /// <summary>
        /// Standard Formats Count
        /// </summary>
        public int StandardFormatsCount
        {
            get { return formatTagDetails.standardFormatsCount; }
        }

        /// <summary>
        /// Format Description
        /// </summary>
        public string FormatDescription
        {
            get { return formatTagDetails.formatDescription; }
        }


    }
}
