using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Interop structure for ACM driver details (ACMDRIVERDETAILS)
    /// http://msdn.microsoft.com/en-us/library/dd742889%28VS.85%29.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack=2)]
    struct AcmDriverDetails
    {
        /// <summary>
        /// DWORD cbStruct
        /// </summary>
        public int structureSize;
        /// <summary>
        /// FOURCC fccType
        /// </summary>
        public UInt32 fccType;
        /// <summary>
        /// FOURCC fccComp
        /// </summary>
        public UInt32 fccComp;
        /// <summary>
        /// WORD   wMid; 
        /// </summary>
        public UInt16 manufacturerId;
        /// <summary>
        /// WORD wPid
        /// </summary>
        public UInt16 productId;
        /// <summary>
        /// DWORD vdwACM
        /// </summary>
        public UInt32 acmVersion;
        /// <summary>
        /// DWORD vdwDriver
        /// </summary>
        public UInt32 driverVersion;
        /// <summary>
        /// DWORD  fdwSupport;
        /// </summary>
        public AcmDriverDetailsSupportFlags supportFlags;
        /// <summary>
        /// DWORD cFormatTags
        /// </summary>
        public int formatTagsCount;
        /// <summary>
        /// DWORD cFilterTags
        /// </summary>
        public int filterTagsCount;
        /// <summary>
        /// HICON hicon
        /// </summary>
        public IntPtr hicon;
        /// <summary>
        /// TCHAR  szShortName[ACMDRIVERDETAILS_SHORTNAME_CHARS]; 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ShortNameChars)]
        public string shortName;
        /// <summary>
        /// TCHAR  szLongName[ACMDRIVERDETAILS_LONGNAME_CHARS];
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LongNameChars)]
        public string longName;
        /// <summary>
        /// TCHAR  szCopyright[ACMDRIVERDETAILS_COPYRIGHT_CHARS]; 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CopyrightChars)]
        public string copyright;
        /// <summary>
        /// TCHAR  szLicensing[ACMDRIVERDETAILS_LICENSING_CHARS]; 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LicensingChars)]
        public string licensing;
        /// <summary>
        /// TCHAR  szFeatures[ACMDRIVERDETAILS_FEATURES_CHARS];
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FeaturesChars)]
        public string features;

        /// <summary>
        /// ACMDRIVERDETAILS_SHORTNAME_CHARS
        /// </summary>
        private const int ShortNameChars = 32;
        /// <summary>
        /// ACMDRIVERDETAILS_LONGNAME_CHARS
        /// </summary>
        private const int LongNameChars = 128;
        /// <summary>
        /// ACMDRIVERDETAILS_COPYRIGHT_CHARS
        /// </summary>
        private const int CopyrightChars = 80;
        /// <summary>
        /// ACMDRIVERDETAILS_LICENSING_CHARS 
        /// </summary>
        private const int LicensingChars = 128;
        /// <summary>
        /// ACMDRIVERDETAILS_FEATURES_CHARS
        /// </summary>
        private const int FeaturesChars = 512;
    } 
}
