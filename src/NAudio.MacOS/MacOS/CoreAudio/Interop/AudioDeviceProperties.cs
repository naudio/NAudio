/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioDevice Properties <br />
/// AudioObjectPropertySelector values provided by the AudioDevice class.
/// </summary>
/// <remarks>
/// The AudioDevice class is a subclass of the AudioObjectClass. The class has four
/// scopes, kAudioObjectPropertyScopeGlobal, kAudioObjectPropertyScopeInput,
/// kAudioObjectPropertyScopeOutput, and kAudioObjectPropertyScopePlayThrough. The
/// class has a main element and an element for each channel in each stream
/// numbered according to the starting channel number of each stream.
/// </remarks>
internal static class AudioDeviceProperties
{
    /// <summary>
    /// A CFString that contains the bundle ID for an application that provides a GUI for configuring the AudioDevice. 
    /// By default, the value of this property is the bundle ID for Audio MIDI Setup. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyConfigurationApplication        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("capp"u8); // 'capp'
    /// <summary>
    /// A CFString that contains a persistent identifier for the AudioDevice. 
    /// An AudioDevice's UID is persistent across boots. 
    /// The content of the UID string is a black box and may contain information
    /// that is unique to a particular instance of an AudioDevice's hardware or unique to the CPU.
    /// Therefore they are not suitable for passing between CPUs or for identifying similar models
    /// of hardware. The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceUID                       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("uid "u8); // 'uid '
    /// <summary>
    /// A CFString that contains a persistent identifier for the model of an AudioDevice. 
    /// The identifier is unique such that the identifier from two
    /// AudioDevices are equal if and only if the two AudioDevices are the exact same model from the same manufacturer. 
    /// Further, the identifier has to be the same no matter on what machine the AudioDevice appears. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyModelUID                        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("muid"u8); // 'muid'
    /// <summary>
    /// A <see cref="TransportType"/> whose value indicates how the AudioDevice is connected to the CPU.
    /// Constants for some of the values for this property can be found in the enum <see cref="TransportTypeConstants"/>.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyTransportType                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("tran"u8); // 'tran'
    /// <summary>
    /// An array of AudioDeviceIDs for devices related to the AudioDevice. 
    /// For IOAudio-based devices, AudioDevices are related if they share the same IOAudioDevice object.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyRelatedDevices                  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("akin"u8); // 'akin'
    /// <summary>
    /// A UInt32 whose value indicates the clock domain to which this AudioDevice belongs. 
    /// AudioDevices that have the same value for this property are able to be synchronized in hardware.
    /// However, a value of 0 indicates that the clock domain for the device is unspecified and should 
    /// be assumed to be separate from every other device's clock domain, even if they have the value 
    /// of 0 as their clock domain as well.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyClockDomain                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("clkd"u8); // 'clkd'
    /// <summary>
    /// A UInt32 where a value of 1 means the device is ready and available and 
    /// 0 means the device is unusable and will most likely go away shortly.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceIsAlive                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("livn"u8); // 'livn'
    /// <summary>
    /// A UInt32 where a value of 0 means the AudioDevice is not performing IO and
    /// a value of 1 means that it is. Note that the device can be running even if
    /// there are no active IOProcs such as by calling AudioDeviceStart() and
    /// passing a NULL IOProc. Note that the notification for this property is
    /// usually sent from the AudioDevice's IO thread.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceIsRunning                 = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("goin"u8); // 'goin'
    /// <summary>
    /// A UInt32 where 1 means that the AudioDevice is a possible selection for
    /// kAudioHardwarePropertyDefaultInputDevice or
    /// kAudioHardwarePropertyDefaultOutputDevice depending on the scope.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceCanBeDefaultDevice        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dflt"u8); // 'dflt'
    /// <summary>
    /// A UInt32 where 1 means that the AudioDevice is a possible selection for
    /// kAudioHardwarePropertyDefaultSystemOutputDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceCanBeDefaultSystemDevice  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sflt"u8); // 'sflt'
    /// <summary>
    /// A UInt32 containing the number of frames of latency in the AudioDevice. Note
    /// that input and output latency may differ. Further, the AudioDevice's
    /// AudioStreams may have additional latency so they should be queried as well.
    /// If both the device and the stream say they have latency, then the total
    /// latency for the stream is the device latency summed with the stream latency.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyLatency                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ltnc"u8); // 'ltnc'
    /// <summary>
    /// An array of AudioStreamIDs that represent the AudioStreams of the AudioDevice. 
    /// Note that if a notification is received for this property, any cached AudioStreamIDs 
    /// for the device become invalid and need to be re-fetched.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyStreams                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("stm#"u8); // 'stm#'
    /// <summary>
    /// An array of AudioObjectIDs that represent the AudioControls of the AudioDevice.
    /// Note that if a notification is received for this property, any cached 
    /// AudioObjectIDs for the device become invalid and need to be re-fetched.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyControlList                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ctrl"u8); // 'ctrl'
    /// <summary>
    /// A UInt32 whose value indicates the number for frames in ahead (for output) 
    /// or behind (for input the current hardware position that is safe to do IO.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertySafetyOffset                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("saft"u8); // 'saft'
    /// <summary>
    /// A Float64 that indicates the current nominal sample rate of the AudioDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyNominalSampleRate               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("nsrt"u8); // 'nsrt'
    /// <summary>
    /// An array of AudioValueRange structs that indicates the valid
    /// ranges for the nominal sample rate of the AudioDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyAvailableNominalSampleRates     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("nsr#"u8); // 'nsr#'
    /// <summary>
    /// A CFURLRef that indicates an image file that can be used to represent the device visually. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIcon                            = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("icon"u8); // 'icon'
    /// <summary>
    /// A UInt32 where a non-zero value indicates that the device is not included 
    /// in the normal list of devices provided by kAudioHardwarePropertyDevices nor 
    /// can it be the default device. Hidden devices can only be discovered by
    /// knowing their UID and using kAudioHardwarePropertyDeviceForUID.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIsHidden                        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("hidn"u8); // 'hidn'
    /// <summary>
    /// An array of two UInt32s, the first for the left channel, the second for the
    /// right channel, that indicate the channel numbers to use for stereo IO on the
    /// device. The value of this property can be different for input and output and
    /// there are no restrictions on the channel numbers that can be used.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyPreferredChannelsForStereo      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dch2"u8); // 'dch2'
    /// <summary>
    /// An AudioChannelLayout that indicates how each channel of the AudioDevice should be used.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyPreferredChannelLayout          = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("srnd"u8); // 'srnd'
}