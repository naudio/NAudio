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
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyConfigurationApplication        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("capp"); // 'capp'
    /// <summary>
    /// A CFString that contains a persistent identifier for the AudioDevice. 
    /// An AudioDevice's UID is persistent across boots. 
    /// The content of the UID string is a black box and may contain information
    /// that is unique to a particular instance of an AudioDevice's hardware or unique to the CPU.
    /// Therefore they are not suitable for passing between CPUs or for identifying similar models
    /// of hardware. The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceUID                       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("uid "); // 'uid '
    /// <summary>
    /// A CFString that contains a persistent identifier for the model of an AudioDevice. 
    /// The identifier is unique such that the identifier from two
    /// AudioDevices are equal if and only if the two AudioDevices are the exact same model from the same manufacturer. 
    /// Further, the identifier has to be the same no matter on what machine the AudioDevice appears. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyModelUID                        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("muid"); // 'muid'
    /// <summary>
    /// A <see cref="TransportType"/> whose value indicates how the AudioDevice is connected to the CPU.
    /// Constants for some of the values for this property can be found in the enum <see cref="TransportTypeConstants"/>.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyTransportType                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("tran"); // 'tran'
    /// <summary>
    /// An array of AudioDeviceIDs for devices related to the AudioDevice. 
    /// For IOAudio-based devices, AudioDevices are related if they share the same IOAudioDevice object.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyRelatedDevices                  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("akin"); // 'akin'
    /// <summary>
    /// A UInt32 whose value indicates the clock domain to which this AudioDevice belongs. 
    /// AudioDevices that have the same value for this property are able to be synchronized in hardware.
    /// However, a value of 0 indicates that the clock domain for the device is unspecified and should 
    /// be assumed to be separate from every other device's clock domain, even if they have the value 
    /// of 0 as their clock domain as well.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyClockDomain                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("clkd"); // 'clkd'
    /// <summary>
    /// A UInt32 where a value of 1 means the device is ready and available and 
    /// 0 means the device is unusable and will most likely go away shortly.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceIsAlive                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("livn"); // 'livn'
    /// <summary>
    /// A UInt32 where a value of 0 means the AudioDevice is not performing IO and
    /// a value of 1 means that it is. Note that the device can be running even if
    /// there are no active IOProcs such as by calling AudioDeviceStart() and
    /// passing a NULL IOProc. Note that the notification for this property is
    /// usually sent from the AudioDevice's IO thread.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceIsRunning                 = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("goin"); // 'goin'
    /// <summary>
    /// A UInt32 where 1 means that the AudioDevice is a possible selection for
    /// kAudioHardwarePropertyDefaultInputDevice or
    /// kAudioHardwarePropertyDefaultOutputDevice depending on the scope.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceCanBeDefaultDevice        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dflt"); // 'dflt'
    /// <summary>
    /// A UInt32 where 1 means that the AudioDevice is a possible selection for
    /// kAudioHardwarePropertyDefaultSystemOutputDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceCanBeDefaultSystemDevice  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sflt"); // 'sflt'
    /// <summary>
    /// A UInt32 containing the number of frames of latency in the AudioDevice. Note
    /// that input and output latency may differ. Further, the AudioDevice's
    /// AudioStreams may have additional latency so they should be queried as well.
    /// If both the device and the stream say they have latency, then the total
    /// latency for the stream is the device latency summed with the stream latency.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyLatency                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ltnc"); // 'ltnc'
    /// <summary>
    /// An array of AudioStreamIDs that represent the AudioStreams of the AudioDevice. 
    /// Note that if a notification is received for this property, any cached AudioStreamIDs 
    /// for the device become invalid and need to be re-fetched.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyStreams                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("stm#"); // 'stm#'
    /// <summary>
    /// An array of AudioObjectIDs that represent the AudioControls of the AudioDevice.
    /// Note that if a notification is received for this property, any cached 
    /// AudioObjectIDs for the device become invalid and need to be re-fetched.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyControlList                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ctrl"); // 'ctrl'
    /// <summary>
    /// A UInt32 whose value indicates the number for frames in ahead (for output) 
    /// or behind (for input the current hardware position that is safe to do IO.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertySafetyOffset                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("saft"); // 'saft'
    /// <summary>
    /// A Float64 that indicates the current nominal sample rate of the AudioDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyNominalSampleRate               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("nsrt"); // 'nsrt'
    /// <summary>
    /// An array of AudioValueRange structs that indicates the valid
    /// ranges for the nominal sample rate of the AudioDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyAvailableNominalSampleRates     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("nsr#"); // 'nsr#'
    /// <summary>
    /// A CFURLRef that indicates an image file that can be used to represent the device visually. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIcon                            = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("icon"); // 'icon'
    /// <summary>
    /// A UInt32 where a non-zero value indicates that the device is not included 
    /// in the normal list of devices provided by kAudioHardwarePropertyDevices nor 
    /// can it be the default device. Hidden devices can only be discovered by
    /// knowing their UID and using kAudioHardwarePropertyDeviceForUID.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIsHidden                        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("hidn"); // 'hidn'
    /// <summary>
    /// An array of two UInt32s, the first for the left channel, the second for the
    /// right channel, that indicate the channel numbers to use for stereo IO on the
    /// device. The value of this property can be different for input and output and
    /// there are no restrictions on the channel numbers that can be used.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyPreferredChannelsForStereo      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dch2"); // 'dch2'
    /// <summary>
    /// An AudioChannelLayout that indicates how each channel of the AudioDevice should be used.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyPreferredChannelLayout          = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("srnd"); // 'srnd'
    /// <summary>
    /// An OSStatus that contains any error codes generated by loading the IOAudio
    /// driver plug-in for the AudioDevice or kAudioHardwareNoError if the plug-in
    /// loaded successfully. This property only exists for IOAudio-based
    /// AudioDevices whose driver has specified a plug-in to load.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyPlugIn                          = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("plug");
    /// <summary>
    /// The type of this property is a UInt32, but its value has no meaning. This
    /// property exists so that clients can listen to it and be told when the
    /// configuration of the AudioDevice has changed in ways that cannot otherwise
    /// be conveyed through other notifications. In response to this notification,
    /// clients should re-evaluate everything they need to know about the device,
    /// particularly the layout and values of the controls.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceHasChanged                = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("diff");
    /// <summary>
    /// A UInt32 where 1 means that the AudioDevice is running in at least one
    /// process on the system and 0 means that it isn't running at all.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyDeviceIsRunningSomewhere        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("gone");
    /// <summary>
    /// A UInt32 where the value has no meaning. This property exists so that
    /// clients can be notified when the AudioDevice detects that an IO cycle has
    /// run past its deadline. Note that the notification for this property is
    /// usually sent from the AudioDevice's IO thread.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDeviceProcessorOverload                       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("over");
    /// <summary>
    /// A UInt32 where the value has no meaning. This property exists so that
    /// clients can be notified when IO on the device has stopped outside of the
    /// normal mechanisms. This typically comes up when IO is stopped after
    /// AudioDeviceStart has returned successfully but prior to the notification for
    /// kAudioDevicePropertyIsRunning being sent.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIOStoppedAbnormally             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("stpd");
    /// <summary>
    /// A pid_t indicating the process that currently owns exclusive access to the
    /// AudioDevice or a value of -1 indicating that the device is currently
    /// available to all processes. If the AudioDevice is in a non-mixable mode,
    /// the HAL will automatically take hog mode on behalf of the first process to
    /// start an IOProc.
    /// Note that when setting this property, the value passed in is ignored. If
    /// another process owns exclusive access, that remains unchanged. If the
    /// current process owns exclusive access, it is released and made available to
    /// all processes again. If no process has exclusive access (meaning the current
    /// value is -1), this process gains ownership of exclusive access.  On return,
    /// the pid_t pointed to by inPropertyData will contain the new value of the
    /// property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyHogMode                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("oink");
    /// <summary>
    /// A UInt32 whose value indicates the number of frames in the IO buffers.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyBufferFrameSize                 = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("fsiz");
    /// <summary>
    /// An AudioValueRange indicating the minimum and maximum values, inclusive, for
    /// kAudioDevicePropertyBufferFrameSize.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyBufferFrameSizeRange            = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("fsz#");
    /// <summary>
    /// A UInt32 that, if implemented by a device, indicates that the sizes of the
    /// buffers passed to an IOProc will vary by a small amount. The value of this
    /// property will indicate the largest buffer that will be passed and
    /// kAudioDevicePropertyBufferFrameSize will indicate the smallest buffer that
    /// will get passed to the IOProc. The usage of this property is narrowed to
    /// only allow for devices whose buffer sizes vary by small amounts greater than
    /// kAudioDevicePropertyBufferFrameSize. It is not intended to be a license for
    /// devices to be able to send buffers however they please. Rather, it is
    /// intended to allow for hardware whose natural rhythms lead to this necessity.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyUsesVariableBufferFrameSizes    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("vfsz");
    /// <summary>
    /// A Float32 whose range is from 0 to 1. This value indicates how much of the
    /// client portion of the IO cycle the process will use. The client portion of
    /// the IO cycle is the portion of the cycle in which the device calls the
    /// IOProcs so this property does not the apply to the duration of the entire
    /// cycle.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIOCycleUsage                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ncyc");
    /// <summary>
    /// This property returns the stream configuration of the device in an
    /// AudioBufferList (with the buffer pointers set to NULL) which describes the
    /// list of streams and the number of channels in each stream. This corresponds
    /// to what will be passed into the IOProc.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyStreamConfiguration             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("slay");
    /// <summary>
    /// An AudioHardwareIOProcStreamUsage structure which details the stream usage
    /// of a given IO proc. If a stream is marked as not being used, the given
    /// IOProc will see a corresponding NULL buffer pointer in the AudioBufferList
    /// passed to its IO proc. Note that the number of streams detailed in the
    /// AudioHardwareIOProcStreamUsage must include all the streams of that
    /// direction on the device. Also, when getting the value of the property, one
    /// must fill out the mIOProc field of the AudioHardwareIOProcStreamUsage with
    /// the address of the of the IOProc whose stream usage is to be retrieved.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIOProcStreamUsage               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("suse");
    /// <summary>
    /// A Float64 that indicates the current actual sample rate of the AudioDevice
    /// as measured by its time stamps.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyActualSampleRate                = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("asrt");
    /// <summary>
    /// A CFString that contains the UID for the AudioClockDevice that is currently
    /// serving as the main time base of the device. The caller is responsible
    /// for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyClockDevice                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("apcd");
    /// <summary>
    /// An os_workgroup_t that represents the thread workgroup the AudioDevice's
    /// IO thread belongs to. The caller is responsible for releasing the returned
    /// object.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyIOThreadOSWorkgroup             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("oswg");
    /// <summary>
    /// A UInt32 where a non-zero value indicates that the current process's audio
	/// will be zeroed out by the system. Note that this property does not apply to
	/// aggregate devices, just real, physical devices.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioDevicePropertyProcessMute					   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("appm");
}