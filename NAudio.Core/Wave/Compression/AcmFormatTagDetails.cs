using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct AcmFormatTagDetails
    {
        /// <summary>
        /// DWORD cbStruct; 
        /// </summary>
        public int structureSize;
        /// <summary>
        /// DWORD dwFormatTagIndex; 
        /// </summary>
        public int formatTagIndex;
        /// <summary>
        /// DWORD dwFormatTag; 
        /// </summary>
        public int formatTag;
        /// <summary>
        /// DWORD cbFormatSize; 
        /// </summary>
        public int formatSize;
        /// <summary>
        /// DWORD fdwSupport;
        /// </summary>
        public AcmDriverDetailsSupportFlags supportFlags;
        /// <summary>
        /// DWORD cStandardFormats; 
        /// </summary>
        public int standardFormatsCount;
        /// <summary>
        /// TCHAR szFormatTag[ACMFORMATTAGDETAILS_FORMATTAG_CHARS]; 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FormatTagDescriptionChars)]
        public string formatDescription;



        /// <summary>
        /// ACMFORMATTAGDETAILS_FORMATTAG_CHARS
        /// </summary>
        public const int FormatTagDescriptionChars = 48;

    }
}
