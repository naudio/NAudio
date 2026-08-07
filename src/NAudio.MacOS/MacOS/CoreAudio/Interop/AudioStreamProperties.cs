// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioStream Properties <br />
/// AudioObjectPropertySelector values provided by the AudioStream class.
/// </summary>
/// <remarks>
/// AudioStream is a subclass of AudioObject and has only the single scope,
/// kAudioObjectPropertyScopeGlobal. They have a main element and an element for
/// each channel in the stream numbered upward from 1.
/// </remarks>
internal static class AudioStreamProperties
{
    /// <summary>
    /// A UInt32 where a non-zero value indicates that the stream is enabled and doing IO.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyIsActive                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sact"); // 'sact',
    /// <summary>
    /// A UInt32 where a value of 0 means that this AudioStream is an 
    /// output stream and a value of 1 means that it is an input stream.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyDirection                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sdir"); // 'sdir',
    /// <summary>
    /// A UInt32 whose value describes the general kind of functionality attached to the AudioStream.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyTerminalType                = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("term"); // 'term',
    /// <summary>
    /// A UInt32 that specifies the first element in the owning 
    /// device that corresponds to element one of this stream.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyStartingChannel             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("schn"); // 'schn',
    /// <summary>
    /// A UInt32 containing the number of frames of latency in the AudioStream. Note
    /// that the owning AudioDevice may have additional latency so it should be
    /// queried as well. If both the device and the stream say they have latency,
    /// then the total latency for the stream is the device latency summed with the
    /// stream latency.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyLatency                     = AudioDeviceProperties.kAudioDevicePropertyLatency;
    /// <summary>
    /// An AudioStreamBasicDescription that describes the current data format for
    /// the AudioStream. The virtual format refers to the data format in which all
    /// IOProcs for the owning AudioDevice will perform IO transactions.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyVirtualFormat               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sfmt"); // 'sfmt',
    /// <summary>
    /// An array of AudioStreamRangedDescriptions that describe the available data
    /// formats for the AudioStream. The virtual format refers to the data format in
    /// which all IOProcs for the owning AudioDevice will perform IO transactions.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyAvailableVirtualFormats     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sfma"); // 'sfma',
    /// <summary>
    /// An AudioStreamBasicDescription that describes the current data format for
    /// the AudioStream. The physical format refers to the data format in which the
    /// hardware for the owning AudioDevice performs its IO transactions.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyPhysicalFormat              = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pft "); // 'pft ',
    /// <summary>
    /// An array of AudioStreamRangedDescriptions that describe the available data
    /// formats for the AudioStream. The physical format refers to the data format
    /// in which the hardware for the owning AudioDevice performs its IO
    /// transactions.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStreamPropertyAvailablePhysicalFormats    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pfta"); // 'pfta'
}