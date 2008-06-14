using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// ACM Format
    /// </summary>
    public class AcmFormat
    {
        AcmFormatDetails formatDetails;

        internal AcmFormat(AcmFormatDetails formatDetails)
        {
            this.formatDetails = formatDetails;
        }

        /// <summary>
        /// Format Index
        /// </summary>
        public int FormatIndex
        {
            get { return formatDetails.formatIndex; }
        }

        /// <summary>
        /// Format Tag
        /// </summary>
        public WaveFormatEncoding FormatTag
        {
            get { return (WaveFormatEncoding)formatDetails.formatTag; }
        }
        /// <summary>
        /// Support Flags
        /// </summary>
        public AcmDriverDetailsSupportFlags SupportFlags
        {
            get { return formatDetails.supportFlags; }
        }
        /// <summary>
        /// WaveFormat
        /// </summary>    
        public WaveFormat WaveFormat
        {
            get 
            {
                WaveFormat waveFormat = new WaveFormat();
                Marshal.PtrToStructure(formatDetails.waveFormatPointer, waveFormat);
                return waveFormat; 
            }
        }
        /// <summary>
        /// WaveFormat Size
        /// </summary>
        public int WaveFormatByteSize
        {
            get { return formatDetails.waveFormatByteSize; }
        }
        /// <summary>
        /// Format Description
        /// </summary>        
        public string FormatDescription
        {
            get { return formatDetails.formatDescription; }
        }
    }
}
