using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
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
