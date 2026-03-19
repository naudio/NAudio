#nullable enable
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Exception thrown by Core Audio API operations.
    /// Inherits from COMException for backwards compatibility with code that catches COMException.
    /// </summary>
    public class CoreAudioException : COMException
    {
        /// <summary>
        /// Creates a new CoreAudioException
        /// </summary>
        public CoreAudioException(int hresult)
            : base(GetMessageForHResult(hresult), hresult)
        {
        }

        /// <summary>
        /// Creates a new CoreAudioException with a message
        /// </summary>
        public CoreAudioException(string message, int hresult)
            : base(message, hresult)
        {
        }

        /// <summary>
        /// Throws a CoreAudioException if the HRESULT indicates failure.
        /// </summary>
        public static void ThrowIfFailed(int hresult)
        {
            if (hresult < 0)
                throw Create(hresult);
        }

        private static CoreAudioException Create(int hresult) => hresult switch
        {
            AudioClientErrorCode.DeviceInvalidated => new AudioDeviceDisconnectedException(hresult),
            AudioClientErrorCode.UnsupportedFormat => new AudioFormatNotSupportedException(hresult),
            AudioClientErrorCode.DeviceInUse => new AudioDeviceInUseException(hresult),
            AudioClientErrorCode.ExclusiveModeNotAllowed => new AudioExclusiveModeNotAllowedException(hresult),
            _ => new CoreAudioException(hresult)
        };

        private static string GetMessageForHResult(int hr) => hr switch
        {
            AudioClientErrorCode.NotInitialized => "The audio client has not been initialized.",
            AudioClientErrorCode.AlreadyInitialized => "The audio client is already initialized.",
            AudioClientErrorCode.WrongEndpointType => "The audio endpoint device type is wrong.",
            AudioClientErrorCode.DeviceInvalidated => "The audio device has been disconnected or the audio hardware has been reconfigured.",
            AudioClientErrorCode.NotStopped => "The audio stream was not stopped at the time of the request.",
            AudioClientErrorCode.BufferTooLarge => "The buffer is too large.",
            AudioClientErrorCode.OutOfOrder => "The audio data was written out of order.",
            AudioClientErrorCode.UnsupportedFormat => "The audio format is not supported by the audio endpoint device.",
            AudioClientErrorCode.InvalidSize => "The requested buffer size is invalid.",
            AudioClientErrorCode.DeviceInUse => "The audio device is already in exclusive use by another application.",
            AudioClientErrorCode.BufferOperationPending => "A buffer operation is pending.",
            AudioClientErrorCode.ThreadNotRegistered => "The thread is not registered.",
            AudioClientErrorCode.ExclusiveModeNotAllowed => "Exclusive mode is not allowed for this audio endpoint device.",
            AudioClientErrorCode.EndpointCreateFailed => "The endpoint creation failed.",
            AudioClientErrorCode.ServiceNotRunning => "The Windows audio service is not running.",
            AudioClientErrorCode.EventHandleNotExpected => "The event handle was not expected.",
            AudioClientErrorCode.ExclusiveModeOnly => "This operation is only supported in exclusive mode.",
            AudioClientErrorCode.BufferDurationPeriodNotEqual => "The buffer duration and period must be equal for exclusive event-driven mode.",
            AudioClientErrorCode.EventHandleNotSet => "The event handle has not been set.",
            AudioClientErrorCode.IncorrectBufferSize => "The buffer size is incorrect.",
            AudioClientErrorCode.BufferSizeError => "A buffer size error occurred.",
            AudioClientErrorCode.CpuUsageExceeded => "The CPU usage has exceeded the allowed limit.",
            AudioClientErrorCode.BufferError => "A buffer error occurred.",
            AudioClientErrorCode.BufferSizeNotAligned => "The buffer size is not aligned.",
            AudioClientErrorCode.InvalidDevicePeriod => "The device period is invalid.",
            AudioClientErrorCode.InvalidStreamFlag => "The stream flag is invalid.",
            AudioClientErrorCode.EndpointOffloadNotCapable => "The endpoint is not capable of offload.",
            AudioClientErrorCode.RawModeUnsupported => "Raw mode is not supported by this audio endpoint.",
            AudioClientErrorCode.EnginePeriodicityLocked => "The engine periodicity is locked.",
            AudioClientErrorCode.EngineFormatLocked => "The engine format is locked.",
            _ => $"Core Audio error 0x{hr:X8}"
        };
    }

    /// <summary>
    /// Exception thrown when the audio device has been disconnected or reconfigured.
    /// </summary>
    public class AudioDeviceDisconnectedException : CoreAudioException
    {
        /// <summary>
        /// Creates a new AudioDeviceDisconnectedException
        /// </summary>
        public AudioDeviceDisconnectedException(int hresult) : base(hresult) { }
    }

    /// <summary>
    /// Exception thrown when the audio format is not supported.
    /// </summary>
    public class AudioFormatNotSupportedException : CoreAudioException
    {
        /// <summary>
        /// Creates a new AudioFormatNotSupportedException
        /// </summary>
        public AudioFormatNotSupportedException(int hresult) : base(hresult) { }

        /// <summary>
        /// The format that was requested
        /// </summary>
        public Wave.WaveFormat? RequestedFormat { get; init; }

        /// <summary>
        /// The closest format the endpoint can support, if available
        /// </summary>
        public Wave.WaveFormat? SuggestedFormat { get; init; }
    }

    /// <summary>
    /// Exception thrown when the audio device is in exclusive use by another application.
    /// </summary>
    public class AudioDeviceInUseException : CoreAudioException
    {
        /// <summary>
        /// Creates a new AudioDeviceInUseException
        /// </summary>
        public AudioDeviceInUseException(int hresult) : base(hresult) { }
    }

    /// <summary>
    /// Exception thrown when exclusive mode is not allowed for the audio endpoint.
    /// </summary>
    public class AudioExclusiveModeNotAllowedException : CoreAudioException
    {
        /// <summary>
        /// Creates a new AudioExclusiveModeNotAllowedException
        /// </summary>
        public AudioExclusiveModeNotAllowedException(int hresult) : base(hresult) { }
    }
}
