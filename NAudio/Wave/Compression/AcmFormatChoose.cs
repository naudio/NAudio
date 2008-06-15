using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// ACMFORMATCHOOSE
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct AcmFormatChoose
    {
        /// <summary>
        /// DWORD cbStruct; 
        /// </summary>
        public int structureSize;
        /// <summary>
        /// DWORD fdwStyle; 
        /// </summary>
        public AcmFormatChooseStyleFlags styleFlags;
        /// <summary>
        /// HWND hwndOwner; 
        /// </summary>
        public IntPtr ownerWindowHandle;
        /// <summary>
        /// LPWAVEFORMATEX pwfx; 
        /// </summary>
        public IntPtr selectedWaveFormatPointer;
        /// <summary>
        /// DWORD cbwfx; 
        /// </summary>
        public int selectedWaveFormatByteSize;
        /// <summary>
        /// LPCTSTR pszTitle; 
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string title; 
        /// <summary>
        /// TCHAR szFormatTag[ACMFORMATTAGDETAILS_FORMATTAG_CHARS]; 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=AcmFormatTagDetails.FormatTagDescriptionChars)]
        public string formatTagDescription;
        /// <summary>
        /// TCHAR szFormat[ACMFORMATDETAILS_FORMAT_CHARS]; 
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = AcmFormatDetails.FormatDescriptionChars)]
        public string formatDescription;
        /// <summary>
        /// LPTSTR pszName; 
        /// n.b. can be written into
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string name; 
        /// <summary>
        /// DWORD cchName
        /// Should be at least 128 unless name is zero
        /// </summary>
        public int nameByteSize;
        /// <summary>
        /// DWORD fdwEnum; 
        /// </summary>
        public AcmFormatEnumFlags formatEnumFlags;
        /// <summary>
        /// LPWAVEFORMATEX pwfxEnum; 
        /// </summary>
        public IntPtr waveFormatEnumPointer;
        /// <summary>
        /// HINSTANCE hInstance; 
        /// </summary>
        public IntPtr instanceHandle;
        /// <summary>
        /// LPCTSTR pszTemplateName; 
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string templateName;
        /// <summary>
        /// LPARAM lCustData; 
        /// </summary>
        public int customData;
        /// <summary>
        /// ACMFORMATCHOOSEHOOKPROC pfnHook; 
        /// </summary>
        public AcmInterop.AcmFormatChooseHookProc windowCallbackFunction;
    

    }
}
