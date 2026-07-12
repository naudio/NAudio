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
/// AudioClockDevice Properties <br />
/// AudioObjectPropertySelector values provided by the AudioClockDevice class.
/// </summary>
/// <remarks>
/// The AudioClockDevice class is a subclass of the AudioObject class. 
/// The class has just the global scope, kAudioObjectPropertyScopeGlobal, and only a main element.
/// </remarks>
internal static class AudioClockDeviceProperties
{
    /// <summary>
    /// A CFString that contains a persistent identifier for the AudioClockDevice.
    /// An AudioClockDevice's UID is persistent across boots. The content of the UID
    /// string is a black box and may contain information that is unique to a
    /// particular instance of an clock's hardware or unique to the CPU. Therefore
    /// they are not suitable for passing between CPUs or for identifying similar
    /// models of hardware. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyDeviceUID                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("cuid"u8);
    /// <summary>
    /// A <see cref="TransportType"/> whose value indicates how the AudioClockDevice is connected to the CPU.
    /// Constants for some of the values for this property can be found in the enum <see cref="TransportType"/>.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyTransportType               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("tran"u8);
    /// <summary>
    /// A UInt32 whose value indicates the clock domain to which this AudioClockDevice belongs.
    /// AudioClockDevices and AudioDevices that have the same value for this property are able 
    /// to be synchronized in hardware.
    /// However, a value of 0 indicates that the clock domain for the device is
    /// unspecified and should be assumed to be separate from every other device's
    /// clock domain, even if they have the value of 0 as their clock domain as well.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyClockDomain                 = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("clkd"u8);
    /// <summary>
    /// A UInt32 where a value of 1 means the device is ready and available and 0
    /// means the device is usable and will most likely go away shortly.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyDeviceIsAlive               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("livn"u8);
    /// <summary>
    /// A UInt32 where a value of 0 means the AudioClockDevice is not providing
    /// times and a value of 1 means that it is. Note that the notification for this
    /// property is usually sent from the AudioClockDevice's IO thread.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyDeviceIsRunning             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("goin"u8);
    /// <summary>
    /// A UInt32 containing the number of frames of latency in the AudioClockDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyLatency                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ltnc"u8);
    /// <summary>
    /// An array of AudioObjectIDs that represent the AudioControls of the AudioClockDevice.
    /// Note that if a notification is received for this property, any cached AudioObjectIDs 
    /// for the device become invalid and need to be re-fetched.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyControlList                 = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ctrl"u8);
    /// <summary>
    /// A Float64 that indicates the current nominal sample rate of the AudioClockDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyNominalSampleRate           = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("nsrt"u8);
    /// <summary>
    /// An array of AudioValueRange structs that indicates the valid ranges for the 
    /// nominal sample rate of the AudioClockDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioClockDevicePropertyAvailableNominalSampleRates = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("nsr#"u8);
}