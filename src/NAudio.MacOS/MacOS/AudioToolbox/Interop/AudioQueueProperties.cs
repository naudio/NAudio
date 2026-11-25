// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

using System.Runtime.Versioning;

using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// Constants that identify properties for audio queues.
/// </summary>
internal static class AudioQueueProperties
{
    /// <summary>
    /// A read-only property whose value is a UInt32 that indicates whether or not the queue is
    /// running. A notification is sent when the audio device starts or stops, which is not
    /// necessarily when the start or stop function is called.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_IsRunning               = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqrn");       //!< value is UInt32

    /// <summary>
    /// A read-only property whose value is a Float64 that indicates the sampling rate of the
    /// associated audio device.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueDeviceProperty_SampleRate        = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqsr");       // value is Float64
    /// <summary>
    /// A read-only property whose value is a UInt32 that indicates the number of channels in
    /// the associated audio device.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueDeviceProperty_NumberChannels    = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqdc");       // value is UInt32
    /// <summary>
    /// A read/write property whose value is a CFStringRef that contains the unique identifier
    /// (UID) of the associated audio device.
	///	If the audio queue is tracking the default system device and the device changes, it will
	///	generate a property changed notification for this property. You can then query the HAL 
	///	for info on the new default system device.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_CurrentDevice           = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqcd");       // value is CFStringRef
    
    /// <summary>
    /// A read/write property whose value is an audio format magic cookie. If the audio format
    /// requires a magic cookie, you must set this property before enqueuing any buffers.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_MagicCookie             = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqmc");       // value is void*
    /// <summary>
    /// A read-only UInt32 that indicates the size in bytes of the largest single packet of
    /// data in the output format. This is mostly useful for recording/encoding variable bit rate
    /// compressed data.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_MaximumOutputPacketSize = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("xops");       // value is UInt32
    /// <summary>
    /// A read-only AudioStreamBasicDescription that indicates the queue's recording format.
    /// This is useful when recording, where you may specify a sample rate of 0 during
    /// construction.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_StreamDescription       = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqft");       // value is AudioStreamBasicDescription
    
    /// <summary>
    /// A read/write property whose value is an audio channel layout structure that describes
    /// the audio queue's channel layout. The number of channels must match the format of the
    /// queue. If more than two channels (for instance, a five-channel surround sound) exist in
    /// the queue, there may be a need to specify a channel layout. This layout indicates the
    /// specific order in which the channels do appear, such as left, then center, then right.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_ChannelLayout           = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqcl");       // value is AudioChannelLayout
    /// <summary>
    /// A read-write property whose value is a UInt32 that indicates whether metering of
    /// audio levels is enabled for the audio queue. (0=off, 1=on).
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_EnableLevelMetering     = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqme");       // value is UInt32
    /// <summary>
    /// A read-only property whose value is an array of AudioQueueLevelMeter structures, one
    /// array element per audio channel. The values in the AudioQueueLevelMeters are in the
    /// range 0-1.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_CurrentLevelMeter       = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqmv");       // value is array of AudioQueueLevelMeterState, 1 per channel
    /// <summary>
    /// A read-only property whose value is an array of AudioQueueLevelMeter structures, one
    /// array element per audio channel. The values in the AudioQueueLevelMeters are in
    /// decibels.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_CurrentLevelMeterDB     = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqmd");         // value is array of AudioQueueLevelMeterState, 1 per channel
    /// <summary>
    /// A read/write property whose value is a UInt32 that is the size of the buffer into which
    /// an output audio queue decodes buffers. A large buffer provides more reliability and
    /// better long-term performance at the expense of memory and decreased responsiveness
    /// in some situations.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_DecodeBufferSizeFrames  = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("dcbf");       // value is UInt32
    /// <summary>
    /// A read-only property whose value is a UInt32 indicating the most recent error (if any)
    /// encountered by the queue's internal encoding/decoding process.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_ConverterError          = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("qcve");       // value is UInt32

    /// <summary>
    /// A read/write property whose value is a UInt32 describing whether there is a time/pitch unit
    /// inserted into the queue's audio signal chain. This property may only be set while
    /// the queue is stopped.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_EnableTimePitch         = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("q_tp");       // value is UInt32, 0/1
    /// <summary>
    /// A read/write property whose value is a UInt32 describing the time/pitch algorithm in use.
    /// This property is only valid while a time/pitch has been inserted, and may only be changed
    /// when the queue is not running.
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_TimePitchAlgorithm      = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("qtpa");       // value is UInt32. See values in TimePitchAlgorithmConstants.
    /// <summary>
    /// A read/write property whose value is a UInt32 describing whether the time/pitch unit
    /// has been bypassed (1=bypassed, 0=not bypassed).
    /// </summary>
    public static readonly AudioQueuePropertyID kAudioQueueProperty_TimePitchBypass         = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("qtpb");       // value is UInt32, 1=bypassed

    /// <summary>
    /// A write-only property whose value is an array of AudioQueueChannelAssignment. There must be
    /// one array element for each channel of the queue's format as specified in the
    /// AudioStreamBasicDescription passed to AudioQueueNewOutput or AudioQueueNewInput.
    /// (New in iOS 6.0).
    /// </summary>
    [SupportedOSPlatform("ios6.0")]
    [SupportedOSPlatform("macos10.15")]
    public static readonly AudioQueuePropertyID kAudioQueueProperty_ChannelAssignments      = (AudioQueuePropertyID)MacUtils.ConstructUIntConstantValueFromString("aqca"); // value is an array of AudioQueueChannelAssignment
}