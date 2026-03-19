using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AUDCLNT_STREAMFLAGS
    /// https://docs.microsoft.com/en-us/windows/win32/coreaudio/audclnt-streamflags-xxx-constants
    /// </summary>
    [Flags]
    public enum AudioClientStreamFlags : uint
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_CROSSPROCESS
        /// The audio stream will be a member of a cross-process audio session.
        /// </summary>
        CrossProcess = 0x00010000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_LOOPBACK
        /// The audio stream will operate in loopback mode
        /// </summary>
        Loopback = 0x00020000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_EVENTCALLBACK 
        /// Processing of the audio buffer by the client will be event driven
        /// </summary>
        EventCallback = 0x00040000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_NOPERSIST   
        /// The volume and mute settings for an audio session will not persist across application restarts
        /// </summary>
        NoPersist = 0x00080000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_RATEADJUST
        /// The sample rate of the stream is adjusted to a rate specified by an application.
        /// </summary>
        RateAdjust = 0x00100000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY
        /// When used with AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM, a sample rate converter with better quality 
        /// than the default conversion but with a higher performance cost is used. This should be used if 
        /// the audio is ultimately intended to be heard by humans as opposed to other scenarios such as 
        /// pumping silence or populating a meter.
        /// </summary>
        SrcDefaultQuality = 0x08000000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM
        /// A channel matrixer and a sample rate converter are inserted as necessary to convert between the uncompressed format supplied to IAudioClient::Initialize and the audio engine mix format.
        /// </summary>
        AutoConvertPcm = 0x80000000,
           
    }

    /// <summary>
    /// AUDIOCLIENT_ACTIVATION_PARAMS
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclientactivationparams/ns-audioclientactivationparams-audioclient_activation_params
    /// Used with ActivateAudioInterfaceAsync to capture audio from a specific process (Windows 10 2004+).
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct AudioClientActivationParams
    {
        /// <summary>
        /// The activation type.
        /// </summary>
        public AudioClientActivationType ActivationType;

        /// <summary>
        /// Parameters for process loopback activation.
        /// </summary>
        public AudioClientProcessLoopbackParams ProcessLoopbackParams;
    }

    /// <summary>
    /// AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclientactivationparams/ns-audioclientactivationparams-audioclient_process_loopback_params
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct AudioClientProcessLoopbackParams
    {
        /// <summary>
        /// The ID of the process for which the render streams, and the render streams of its
        /// child processes, will be included or excluded when activating the process loopback stream.
        /// </summary>
        public uint TargetProcessId;

        /// <summary>
        /// Whether to include or exclude the target process tree.
        /// </summary>
        public ProcessLoopbackMode ProcessLoopbackMode;
    }

    /// <summary>
    /// PROCESS_LOOPBACK_MODE
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclientactivationparams/ne-audioclientactivationparams-process_loopback_mode
    /// </summary>
    public enum ProcessLoopbackMode
    {
        /// <summary>
        /// PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE
        /// Render streams from the specified process and its child processes are included in the activated process loopback stream.
        /// </summary>
        IncludeTargetProcessTree,
        /// <summary>
        /// PROCESS_LOOPBACK_MODE_EXCLUDE_TARGET_PROCESS_TREE
        /// Render streams from the specified process and its child processes are excluded from the activated process loopback stream.
        /// </summary>
        ExcludeTargetProcessTree
    }

    /// <summary>
    /// AUDIOCLIENT_ACTIVATION_TYPE
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclientactivationparams/ne-audioclientactivationparams-audioclient_activation_type
    /// </summary>
    public enum AudioClientActivationType
    {
        /// <summary>
        /// AUDIOCLIENT_ACTIVATION_TYPE_DEFAULT
        /// Default activation.
        /// </summary>
        Default,
        /// <summary>
        /// AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK
        /// Process loopback activation, allowing for the inclusion or exclusion of audio rendered by the specified process and its child processes.
        /// </summary>
        ProcessLoopback
    };
}
