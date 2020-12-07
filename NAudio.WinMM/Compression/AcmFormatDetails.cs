using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// ACMFORMATDETAILS
    /// http://msdn.microsoft.com/en-us/library/dd742913%28VS.85%29.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack=4)]
    struct AcmFormatDetails
    {
        /// <summary>
        /// DWORD cbStruct; 
        /// </summary>
        public int structSize;
        /// <summary>
        /// DWORD dwFormatIndex; 
        /// </summary>
        public int formatIndex;
        /// <summary>
        /// DWORD dwFormatTag; 
        /// </summary>
        public int formatTag;
        /// <summary>
        /// DWORD fdwSupport; 
        /// </summary>
        public AcmDriverDetailsSupportFlags supportFlags;
        /// <summary>
        /// LPWAVEFORMATEX pwfx; 
        /// </summary>    
        public IntPtr waveFormatPointer;
        /// <summary>
        /// DWORD cbwfx; 
        /// </summary>
        public int waveFormatByteSize;
        /// <summary>
        /// TCHAR szFormat[ACMFORMATDETAILS_FORMAT_CHARS];
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FormatDescriptionChars)]
        public string formatDescription;

        /// <summary>
        /// ACMFORMATDETAILS_FORMAT_CHARS
        /// </summary>
        public const int FormatDescriptionChars = 128;
    }
}
