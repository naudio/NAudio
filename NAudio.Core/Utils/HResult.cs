using System;
using System.Runtime.InteropServices;

namespace NAudio.Utils
{
    /// <summary>
    /// HResult
    /// </summary>
    public static class HResult
    {
        /// <summary>
        /// S_OK
        /// </summary>
        public const int S_OK = 0;
        /// <summary>
        /// S_FALSE
        /// </summary>
        public const int S_FALSE = 1;
        /// <summary>
        /// E_INVALIDARG (from winerror.h)
        /// </summary>
        public const int E_INVALIDARG = unchecked((int)0x80000003);
        /// <summary>
        /// MAKE_HRESULT macro
        /// </summary>
        public static int MAKE_HRESULT(int sev, int fac, int code)
        {
            return (int) (((uint)sev) << 31 | ((uint)fac) << 16 | ((uint)code));
        }

        const int FACILITY_AAF = 18;
        const int FACILITY_ACS = 20;
        const int FACILITY_BACKGROUNDCOPY = 32;
        const int FACILITY_CERT = 11;  
        const int FACILITY_COMPLUS =          17;  
        const int FACILITY_CONFIGURATION =    33;  
        const int FACILITY_CONTROL          = 10;  
        const int FACILITY_DISPATCH          = 2;  
        const int FACILITY_DPLAY            = 21;  
        const int FACILITY_HTTP            =  25;  
        const int FACILITY_INTERNET         = 12;  
        const int FACILITY_ITF              =  4;  
        const int FACILITY_MEDIASERVER      = 13;  
        const int FACILITY_MSMQ             = 14;  
        const int FACILITY_NULL             =  0;  
        const int FACILITY_RPC              =  1;  
        const int FACILITY_SCARD            = 16;  
        const int FACILITY_SECURITY         =  9;  
        const int FACILITY_SETUPAPI         = 15;  
        const int FACILITY_SSPI             =  9;  
        const int FACILITY_STORAGE          =  3;  
        const int FACILITY_SXS              = 23;  
        const int FACILITY_UMI              = 22;  
        const int FACILITY_URT              = 19;  
        const int FACILITY_WIN32            =  7;  
        const int FACILITY_WINDOWS          =  8;  
        const int FACILITY_WINDOWS_CE       = 24; 

        /// <summary>
        /// Helper to deal with the fact that in Win Store apps,
        /// the HResult property name has changed
        /// </summary>
        /// <param name="exception">COM Exception</param>
        /// <returns>The HResult</returns>
        public static int GetHResult(this COMException exception)
        {
            return exception.ErrorCode;
        }
    }

}
