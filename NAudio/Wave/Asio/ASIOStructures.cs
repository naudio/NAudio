using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Asio
{
    // -------------------------------------------------------------------------------
    // Structures used by the ASIODriver and ASIODriverExt
    // -------------------------------------------------------------------------------
    
    // -------------------------------------------------------------------------------
    // Error and Exceptions
    // -------------------------------------------------------------------------------
    internal enum ASIOError
    {
        ASE_OK = 0,             // This value will be returned whenever the call succeeded
        ASE_SUCCESS = 0x3f4847a0,	// unique success return value for ASIOFuture calls
        ASE_NotPresent = -1000, // hardware input or output is not present or available
        ASE_HWMalfunction,      // hardware is malfunctioning (can be returned by any ASIO function)
        ASE_InvalidParameter,   // input parameter invalid
        ASE_InvalidMode,        // hardware is in a bad mode or used in a bad mode
        ASE_SPNotAdvancing,     // hardware is not running when sample position is inquired
        ASE_NoClock,            // sample clock or rate cannot be determined or is not present
        ASE_NoMemory            // not enough memory for completing the request
    }

    /// <summary>
    /// ASIO common Exception.
    /// </summary>
    internal class ASIOException : Exception
    {
        private ASIOError error;

        public ASIOException()
        {
        }

        public ASIOException(string message)
            : base(message)
        {
        }

        public ASIOException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ASIOError Error
        {
            get { return error; }
            set
            {
                error = value;
                Data["ASIOError"] = error;
            }
        }

        /// <summary>
        /// Gets the name of the error.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns>the name of the error</returns>
        static public String getErrorName(ASIOError error)
        {
            return Enum.GetName(typeof(ASIOError), error);            
        }
    }

    // -------------------------------------------------------------------------------
    // Channel Info, Buffer Info
    // -------------------------------------------------------------------------------

    

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    internal struct ASIOChannelInfo
    {
        public int channel; // on input, channel index
        public bool isInput; // on input
        public bool isActive; // on exit
        public int channelGroup; // dto
        [MarshalAs(UnmanagedType.U4)]
        public AsioSampleType type; // dto
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public String name; // dto
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ASIOBufferInfo
    {
        public bool isInput;       // on input:  ASIOTrue: input, else output
        public int channelNum;     // on input:  channel index
        public IntPtr pBuffer0;    // on output: double buffer addresses
        public IntPtr pBuffer1;    // on output: double buffer addresses

        public IntPtr Buffer(int bufferIndex)
        {
            return (bufferIndex == 0) ? pBuffer0 : pBuffer1;
        }

    }

    internal enum ASIOMessageSelector
    {
        kAsioSelectorSupported = 1,	// selector in <value>, returns 1L if supported,
        kAsioEngineVersion,			// returns engine (host) asio implementation version,
        kAsioResetRequest,			// request driver reset. if accepted, this
        kAsioBufferSizeChange,		// not yet supported, will currently always return 0L.
        kAsioResyncRequest,			// the driver went out of sync, such that
        kAsioLatenciesChanged, 		// the drivers latencies have changed. The engine
        kAsioSupportsTimeInfo,		// if host returns true here, it will expect the
        kAsioSupportsTimeCode,		// 
        kAsioMMCCommand,			// unused - value: number of commands, message points to mmc commands
        kAsioSupportsInputMonitor,	// kAsioSupportsXXX return 1 if host supports this
        kAsioSupportsInputGain,     // unused and undefined
        kAsioSupportsInputMeter,    // unused and undefined
        kAsioSupportsOutputGain,    // unused and undefined
        kAsioSupportsOutputMeter,   // unused and undefined
        kAsioOverload,              // driver detected an overload
    }

    // -------------------------------------------------------------------------------
    // Time structures
    // -------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    internal struct ASIOTimeCode
    {
        public double speed;                  // speed relation (fraction of nominal speed)
        // ASIOSamples     timeCodeSamples;        // time in samples
        public ASIO64Bit timeCodeSamples;        // time in samples
        public ASIOTimeCodeFlags flags;                  // some information flags (see below)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string future;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ASIO64Bit
    {
        public uint hi;
        public uint lo;
        // TODO: IMPLEMENT AN EASY WAY TO CONVERT THIS TO double  AND long
    };

    [Flags]
    internal enum ASIOTimeCodeFlags
    {
        kTcValid = 1,
        kTcRunning = 1 << 1,
        kTcReverse = 1 << 2,
        kTcOnspeed = 1 << 3,
        kTcStill = 1 << 4,
        kTcSpeedValid = 1 << 8
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    internal struct AsioTimeInfo
    {
        public double speed;                  // absolute speed (1. = nominal)
        public ASIO64Bit systemTime;             // system time related to samplePosition, in nanoseconds
        public ASIO64Bit samplePosition;
        public double sampleRate;             // current rate
        public AsioTimeInfoFlags flags;                    // (see below)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string reserved;
    }

    [Flags]
    internal enum AsioTimeInfoFlags
    {
        kSystemTimeValid = 1,            // must always be valid
        kSamplePositionValid = 1 << 1,       // must always be valid
        kSampleRateValid = 1 << 2,
        kSpeedValid = 1 << 3,
        kSampleRateChanged = 1 << 4,
        kClockSourceChanged = 1 << 5
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    internal struct ASIOTime
    {                         // both input/output
        public int reserved1;
        public int reserved2;
        public int reserved3;
        public int reserved4;
        public AsioTimeInfo timeInfo;       // required
        public ASIOTimeCode timeCode;       // optional, evaluated if (timeCode.flags & kTcValid)
    }

    // -------------------------------------------------------------------------------
    // Callbacks
    // -------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ASIOCallbacks
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ASIOBufferSwitchCallBack(int doubleBufferIndex, bool directProcess);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ASIOSampleRateDidChangeCallBack(double sRate);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ASIOAsioMessageCallBack(ASIOMessageSelector selector, int value, IntPtr message, IntPtr opt);
        // return ASIOTime*
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ASIOBufferSwitchTimeInfoCallBack(IntPtr asioTimeParam, int doubleBufferIndex, bool directProcess);
        //        internal delegate IntPtr ASIOBufferSwitchTimeInfoCallBack(ref ASIOTime asioTimeParam, int doubleBufferIndex, bool directProcess);

        //	void (*bufferSwitch) (long doubleBufferIndex, ASIOBool directProcess);
        public ASIOBufferSwitchCallBack pbufferSwitch;
        //    void (*sampleRateDidChange) (ASIOSampleRate sRate);
        public ASIOSampleRateDidChangeCallBack psampleRateDidChange;
        //	long (*asioMessage) (long selector, long value, void* message, double* opt);
        public ASIOAsioMessageCallBack pasioMessage;
        //	ASIOTime* (*bufferSwitchTimeInfo) (ASIOTime* params, long doubleBufferIndex, ASIOBool directProcess);
        public ASIOBufferSwitchTimeInfoCallBack pbufferSwitchTimeInfo;
    }
}
