using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Interop definitions for Windows ACM (Audio Compression Manager) API
    /// </summary>
    class AcmInterop
    {
        public delegate bool AcmDriverEnumCallback(int hAcmDriverId, int instance, AcmDriverDetailsSupportFlags flags);

        public delegate bool AcmFormatEnumCallback(int hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

        public delegate bool AcmFormatTagEnumCallback(int hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

        /// <summary>
        /// UINT ACMFORMATCHOOSEHOOKPROC acmFormatChooseHookProc(
        ///   HWND hwnd,     
        ///   UINT uMsg,     
        ///   WPARAM wParam, 
        ///   LPARAM lParam  
        /// </summary>        
        public delegate bool AcmFormatChooseHookProc(IntPtr windowHandle, int message, int wParam, int lParam);

        // not done:
        // acmDriverAdd
        // acmDriverID
        // acmDriverMessage
        // acmDriverRemove
        // acmFilterChoose
        // acmFilterChooseHookProc
        // acmFilterDetails
        // acmFilterEnum -acmFilterEnumCallback
        // acmFilterTagDetails
        // acmFilterTagEnum
        // acmFormatDetails        
        // acmFormatTagDetails
        // acmGetVersion
        // acmStreamMessage

        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverClose(IntPtr hAcmDriver, int closeFlags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverEnum(AcmDriverEnumCallback fnCallback, int dwInstance, AcmDriverEnumFlags flags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverDetails(int hAcmDriver, ref AcmDriverDetails driverDetails, int reserved);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverOpen(out IntPtr pAcmDriver, int hAcmDriverId, int openFlags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatChoose(ref AcmFormatChoose formatChoose);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatEnum(IntPtr hAcmDriver, ref AcmFormatDetails formatDetails, AcmFormatEnumCallback callback, IntPtr instance, AcmFormatEnumFlags flags);
        /// <summary>
        /// MMRESULT acmFormatSuggest(
        /// HACMDRIVER had,          
        /// LPWAVEFORMATEX pwfxSrc,  
        /// LPWAVEFORMATEX pwfxDst,  
        /// DWORD cbwfxDst,          
        /// DWORD fdwSuggest);
        /// </summary>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatSuggest(
            IntPtr hAcmDriver,
            [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "NAudio.Wave.WaveFormatCustomMarshaler")] 
            WaveFormat sourceFormat,
            [In, Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "NAudio.Wave.WaveFormatCustomMarshaler")] 
            WaveFormat destFormat, 
            int sizeDestFormat, 
            AcmFormatSuggestFlags suggestFlags);

        [DllImport("Msacm32.dll",EntryPoint="acmFormatSuggest")]
        public static extern MmResult acmFormatSuggest2(
            IntPtr hAcmDriver,
            IntPtr sourceFormatPointer,
            IntPtr destFormatPointer,
            int sizeDestFormat,
            AcmFormatSuggestFlags suggestFlags);
        
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatTagEnum(IntPtr hAcmDriver, ref AcmFormatTagDetails formatTagDetails, AcmFormatTagEnumCallback callback, IntPtr instance, int reserved);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmMetrics(IntPtr hAcmObject, AcmMetrics metric, out int output);
        
        /// <summary>
        /// MMRESULT acmStreamOpen(
        ///   LPHACMSTREAM    phas,       
        ///   HACMDRIVER      had,        
        ///   LPWAVEFORMATEX  pwfxSrc,    
        ///   LPWAVEFORMATEX  pwfxDst,    
        ///   LPWAVEFILTER    pwfltr,     
        ///   DWORD_PTR       dwCallback, 
        ///   DWORD_PTR       dwInstance, 
        ///   DWORD           fdwOpen     
        /// </summary>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamOpen(
            out IntPtr hAcmStream, 
            IntPtr hAcmDriver, 
            [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "NAudio.Wave.WaveFormatCustomMarshaler")] 
            WaveFormat sourceFormat,
            [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "NAudio.Wave.WaveFormatCustomMarshaler")] 
            WaveFormat destFormat, 
            [In] WaveFilter waveFilter, 
            int callback, 
            int instance, 
            AcmStreamOpenFlags openFlags);

        /// <summary>
        /// A version with pointers for troubleshooting
        /// </summary>
        [DllImport("Msacm32.dll",EntryPoint="acmStreamOpen")]
        public static extern MmResult acmStreamOpen2(
            out IntPtr hAcmStream,
            IntPtr hAcmDriver,
            IntPtr sourceFormatPointer,
            IntPtr destFormatPointer,
            [In] WaveFilter waveFilter,
            int callback,
            int instance,
            AcmStreamOpenFlags openFlags);

        
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamClose(IntPtr hAcmStream, int closeFlags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamConvert(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, AcmStreamConvertFlags streamConvertFlags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamPrepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int prepareFlags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamReset(IntPtr hAcmStream, int resetFlags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamSize(IntPtr hAcmStream, int inputBufferSize, out int outputBufferSize, AcmStreamSizeFlags flags);
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamUnprepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int flags);

    }
}
