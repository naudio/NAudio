using System;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Audio Client WASAPI Error Codes (HResult)
    /// </summary>
    public static class AudioClientErrorCode
    {
        // All error codes are precalculated.
        // Calculation:
        // (winerror.h) 
        // #define FACILITY_AUDCLNT                 2185 
        // (audioclient.h)
        // #define AUDCLNTERR(n) MAKEHRESULT(SEVERITYERROR, FACILITYAUDCLNT, n)

        /// <summary>
        /// AUDCLNT_E_NOT_INITIALIZED
        /// </summary>
        public const int NotInitialized = unchecked((int)0x88890001);
        /// <summary>
        /// AUDCLNT_E_ALREADY_INITIALIZED
        /// </summary>
		public const int AlreadyInitialized = unchecked((int)0x88890002);
        /// <summary>
        /// AUDCLNT_E_WRONG_ENDPOINT_TYPE
        /// </summary>
		public const int WrongEndpointType = unchecked((int)0x88890003);
        /// <summary>
        /// AUDCLNT_E_DEVICE_INVALIDATED
        /// </summary>
		public const int DeviceInvalidated = unchecked((int)0x88890004);
        /// <summary>
        /// AUDCLNT_E_NOT_STOPPED
        /// </summary>
		public const int NotStopped = unchecked((int)0x88890005);
        /// <summary>
        /// AUDCLNT_E_BUFFER_TOO_LARGE
        /// </summary>
		public const int BufferTooLarge = unchecked((int)0x88890006);
        /// <summary>
        /// AUDCLNT_E_OUT_OF_ORDER
        /// </summary>
		public const int OutOfOrder = unchecked((int)0x88890007);
        /// <summary>
        /// AUDCLNT_E_UNSUPPORTED_FORMAT
        /// </summary>
		public const int UnsupportedFormat = unchecked((int)0x88890008);
        /// <summary>
        /// AUDCLNT_E_INVALID_SIZE
        /// </summary>
		public const int InvalidSize = unchecked((int)0x88890009);
        /// <summary>
        /// AUDCLNT_E_DEVICE_IN_USE
        /// </summary>
		public const int DeviceInUse = unchecked((int)0x8889000A);
        /// <summary>
        /// AUDCLNT_E_BUFFER_OPERATION_PENDING
        /// </summary>
		public const int BufferOperationPending = unchecked((int)0x8889000B);
        /// <summary>
        /// AUDCLNT_E_THREAD_NOT_REGISTERED
        /// </summary>
		public const int ThreadNotRegistered = unchecked((int)0x8889000C);
        /// <summary>
        /// AUDCLNT_E_NO_SINGLE_PROCESS
        /// </summary>
		public const int NoSingleProcess = unchecked((int)0x8889000D);
        /// <summary>
        /// AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED
        /// </summary>
		public const int ExclusiveModeNotAllowed = unchecked((int)0x8889000E);
        /// <summary>
        /// AUDCLNT_E_ENDPOINT_CREATE_FAILED
        /// </summary>
		public const int EndpointCreateFailed = unchecked((int)0x8889000F);
        /// <summary>
        /// AUDCLNT_E_SERVICE_NOT_RUNNING
        /// </summary>
		public const int ServiceNotRunning = unchecked((int)0x88890010);
        /// <summary>
        /// AUDCLNT_E_EVENTHANDLE_NOT_EXPECTED
        /// </summary>
		public const int EventHandleNotExpected = unchecked((int)0x88890011);
        /// <summary>
        /// AUDCLNT_E_EXCLUSIVE_MODE_ONLY
        /// </summary>
		public const int ExclusiveModeOnly = unchecked((int)0x88890012);
        /// <summary>
        /// AUDCLNT_E_BUFDURATION_PERIOD_NOT_EQUAL
        /// </summary>
		public const int BufferDurationPeriodNotEqual = unchecked((int)0x88890013);
        /// <summary>
        /// AUDCLNT_E_EVENTHANDLE_NOT_SET
        /// </summary>
		public const int EventHandleNotSet = unchecked((int)0x88890014);
        /// <summary>
        /// AUDCLNT_E_INCORRECT_BUFFER_SIZE
        /// </summary>
		public const int IncorrectBufferSize = unchecked((int)0x88890015);
        /// <summary>
        /// AUDCLNT_E_BUFFER_SIZE_ERROR
        /// </summary>
		public const int BufferSizeError = unchecked((int)0x88890016);
        /// <summary>
        /// AUDCLNT_E_CPUUSAGE_EXCEEDED
        /// </summary>
		public const int CpuUsageExceeded = unchecked((int)0x88890017);
        /// <summary>
        /// AUDCLNT_E_BUFFER_ERROR
        /// </summary>
		public const int BufferError = unchecked((int)0x88890018);
        /// <summary>
        /// AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED
        /// </summary>
		public const int BufferSizeNotAligned = unchecked((int)0x88890019);
        /// <summary>
        /// AUDCLNT_E_INVALID_DEVICE_PERIOD
        /// </summary>
		public const int InvalidDevicePeriod = unchecked((int)0x88890020);
        /// <summary>
        /// AUDCLNT_E_INVALID_STREAM_FLAG
        /// </summary>
		public const int InvalidStreamFlag = unchecked((int)0x88890021);
        /// <summary>
        /// AUDCLNT_E_ENDPOINT_OFFLOAD_NOT_CAPABLE
        /// </summary>
		public const int EndpointOffloadNotCapable = unchecked((int)0x88890022);
        /// <summary>
        /// AUDCLNT_E_OUT_OF_OFFLOAD_RESOURCES
        /// </summary>
		public const int OutOfOffloadResources = unchecked((int)0x88890023);
        /// <summary>
        /// AUDCLNT_E_OFFLOAD_MODE_ONLY
        /// </summary>
		public const int OffloadModeOnly = unchecked((int)0x88890024);
        /// <summary>
        /// AUDCLNT_E_NONOFFLOAD_MODE_ONLY
        /// </summary>
		public const int NonOffloadModeOnly = unchecked((int)0x88890025);
        /// <summary>
        /// AUDCLNT_E_RESOURCES_INVALIDATED
        /// </summary>
		public const int ResourcesInvalidated = unchecked((int)0x88890026);
        /// <summary>
        /// AUDCLNT_E_RAW_MODE_UNSUPPORTED
        /// </summary>
		public const int RawModeUnsupported = unchecked((int)0x88890027);
        /// <summary>
        /// AUDCLNT_E_ENGINE_PERIODICITY_LOCKED
        /// </summary>
		public const int EnginePeriodicityLocked = unchecked((int)0x88890028);
        /// <summary>
        /// AUDCLNT_E_ENGINE_FORMAT_LOCKED
        /// </summary>
		public const int EngineFormatLocked = unchecked((int)0x88890029);
        /// <summary>
        /// AUDCLNT_E_HEADTRACKING_ENABLED
        /// </summary>
		public const int HeadTrackingEnabled = unchecked((int)0x88890030);
        /// <summary>
        /// AUDCLNT_E_HEADTRACKING_UNSUPPORTED
        /// </summary>
		public const int HeadTrackingUnsupported = unchecked((int)0x88890040);
    }
}