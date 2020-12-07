using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Interop definitions for Windows ACM (Audio Compression Manager) API
    /// </summary>
    class AcmInterop
    {
        // http://msdn.microsoft.com/en-us/library/dd742891%28VS.85%29.aspx
        public delegate bool AcmDriverEnumCallback(IntPtr hAcmDriverId, IntPtr instance, AcmDriverDetailsSupportFlags flags);

        public delegate bool AcmFormatEnumCallback(IntPtr hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

        public delegate bool AcmFormatTagEnumCallback(IntPtr hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/dd742910%28VS.85%29.aspx
        /// UINT ACMFORMATCHOOSEHOOKPROC acmFormatChooseHookProc(
        ///   HWND hwnd,     
        ///   UINT uMsg,     
        ///   WPARAM wParam, 
        ///   LPARAM lParam  
        /// </summary>        
        public delegate bool AcmFormatChooseHookProc(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

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

        // http://msdn.microsoft.com/en-us/library/windows/desktop/dd742885%28v=vs.85%29.aspx
        // MMRESULT acmDriverAdd(
        //        LPHACMDRIVERID phadid,
        //        HINSTANCE hinstModule,
        //        LPARAM lParam,
        //        DWORD dwPriority,
        //        DWORD fdwAdd)
        [DllImport("msacm32.dll")]
        public static extern MmResult acmDriverAdd(out IntPtr driverHandle,
            IntPtr driverModule,
            IntPtr driverFunctionAddress,
            int priority,
            AcmDriverAddFlags flags);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/dd742897%28v=vs.85%29.aspx
        [DllImport("msacm32.dll")]
        public static extern MmResult acmDriverRemove(IntPtr driverHandle,
            int removeFlags);

        // http://msdn.microsoft.com/en-us/library/dd742886%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverClose(IntPtr hAcmDriver, int closeFlags);
        
        // http://msdn.microsoft.com/en-us/library/dd742890%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverEnum(AcmDriverEnumCallback fnCallback, IntPtr dwInstance, AcmDriverEnumFlags flags);

        // http://msdn.microsoft.com/en-us/library/dd742887%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverDetails(IntPtr hAcmDriver, ref AcmDriverDetails driverDetails, int reserved);

        // http://msdn.microsoft.com/en-us/library/dd742894%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverOpen(out IntPtr pAcmDriver, IntPtr hAcmDriverId, int openFlags);

        // http://msdn.microsoft.com/en-us/library/dd742909%28VS.85%29.aspx
        [DllImport("Msacm32.dll", EntryPoint = "acmFormatChooseW")]
        public static extern MmResult acmFormatChoose(ref AcmFormatChoose formatChoose);

        // http://msdn.microsoft.com/en-us/library/dd742914%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatEnum(IntPtr hAcmDriver, ref AcmFormatDetails formatDetails, AcmFormatEnumCallback callback, IntPtr instance, AcmFormatEnumFlags flags);

#if NET35
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/dd742916%28VS.85%29.aspx
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
#endif

        [DllImport("Msacm32.dll",EntryPoint="acmFormatSuggest")]
        public static extern MmResult acmFormatSuggest2(
            IntPtr hAcmDriver,
            IntPtr sourceFormatPointer,
            IntPtr destFormatPointer,
            int sizeDestFormat,
            AcmFormatSuggestFlags suggestFlags);

        // http://msdn.microsoft.com/en-us/library/dd742919%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatTagEnum(IntPtr hAcmDriver, ref AcmFormatTagDetails formatTagDetails, AcmFormatTagEnumCallback callback, IntPtr instance, int reserved);

        // http://msdn.microsoft.com/en-us/library/dd742922%28VS.85%29.aspx
        // this version of the prototype is for metrics that output a single integer
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmMetrics(IntPtr hAcmObject, AcmMetrics metric, out int output);

#if NET35
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/dd742928%28VS.85%29.aspx
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
            IntPtr callback, 
            IntPtr instance, 
            AcmStreamOpenFlags openFlags);
#endif

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
            IntPtr callback,
            IntPtr instance,
            AcmStreamOpenFlags openFlags);

        // http://msdn.microsoft.com/en-us/library/dd742923%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamClose(IntPtr hAcmStream, int closeFlags);

        // http://msdn.microsoft.com/en-us/library/dd742924%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamConvert(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, AcmStreamConvertFlags streamConvertFlags);

        // http://msdn.microsoft.com/en-us/library/dd742929%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamPrepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int prepareFlags);

        // http://msdn.microsoft.com/en-us/library/dd742929%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamReset(IntPtr hAcmStream, int resetFlags);
        
        // http://msdn.microsoft.com/en-us/library/dd742931%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamSize(IntPtr hAcmStream, int inputBufferSize, out int outputBufferSize, AcmStreamSizeFlags flags);

        // http://msdn.microsoft.com/en-us/library/dd742932%28VS.85%29.aspx
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamUnprepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int flags);
    }
}
