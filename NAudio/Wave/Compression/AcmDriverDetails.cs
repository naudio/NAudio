using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Interop structure for ACM driver details (ACMDRIVERDETAILS)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct AcmDriverDetails
    {
        public int cbStruct;
        public UInt32 fccType;
        public UInt32 fccComp;
        public UInt16 wMid;
        public UInt16 wPid;
        public UInt32 vdwACM;
        public UInt32 vdwDriver;
        public UInt32 fdwSupport;
        public UInt32 cFormatTags;
        public UInt32 cFilterTags;
        public IntPtr hicon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ShortNameChars)]
        public string szShortName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LongNameChars)]
        public string szLongName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CopyrightChars)]
        public string szCopyright;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LicensingChars)]
        public string szLicensing;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FeaturesChars)]
        public string szFeatures;

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
