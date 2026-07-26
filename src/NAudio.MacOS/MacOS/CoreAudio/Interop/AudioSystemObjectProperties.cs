/*==================================================================================================
     File:       CoreAudio/AudioHardware.h

     Contains:   API for communicating with audio hardware.

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioSystemObject Properties
/// AudioObjectPropertySelector values provided by the AudioSystemObject class.
/// The AudioSystemObject class is a subclass of the AudioObject class. the class
/// has just the global scope, kAudioObjectPropertyScopeGlobal, and only a main element.
/// </summary>
internal static class AudioSystemObjectProperties
{
    /// <summary>
    /// An array of the AudioObjectIDs that represent 
    /// all the devices currently available to the system.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyDevices                               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dev#"); // 'dev#',
    /// <summary>
    /// The AudioObjectID of the default input AudioDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyDefaultInputDevice                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dIn "); // 'dIn ',
    /// <summary>
    /// The AudioObjectID of the default output AudioDevice.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyDefaultOutputDevice                   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("dOut"); // 'dOut',
    /// <summary>
    /// The AudioObjectID of the output AudioDevice to use for system 
    /// related sound from the alert sound to digital call progress.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyDefaultSystemOutputDevice             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sOut"); // 'sOut',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the AudioDevice
    /// that has the given UID. The UID is passed in via the qualifier as a CFString
    /// while the AudioObjectID for the AudioDevice is returned to the caller as the
    /// property's data. Note that an error is not returned if the UID doesn't refer
    /// to any AudioDevices. Rather, this property will return kAudioObjectUnknown
    /// as the value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslateUIDToDevice                  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("uidd"); // 'uidd',
    /// <summary>
    /// A UInt32 where a value other than 0 indicates that AudioDevices should mix
    /// stereo signals down to mono. Note that the two channels on the device that
    /// comprise the stereo signal are defined on the device by
    /// kAudioDevicePropertyPreferredChannelsForStereo.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyMixStereoToMono                       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("stmo"); // 'stmo',
    /// <summary>
    /// An array of AudioObjectIDs that represent all the AudioPlugIn objects
    /// currently provided by the system
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyPlugInList                            = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("plg#"); // 'plg#',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the AudioPlugIn
    /// that has the given bundle ID. The bundle ID is passed in via the qualifier
    /// as a CFString while the AudioObjectID for the AudioPlugIn is returned to the
    /// caller as the property's data. Note that an error is not returned if the UID
    /// doesn't refer to any AudioPlugIns. Rather, this property will return
    /// kAudioObjectUnknown as the value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslateBundleIDToPlugIn             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("bidp"); // 'bidp',
    /// <summary>
    /// An array of the AudioObjectIDs for all the AudioTransportManager objects.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTransportManagerList                  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("tmg#"); // 'tmg#',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the
    /// AudioTransportManager whose bundle has the given bundle ID. The bundle ID is
    /// passed in via the qualifier as a CFString while the AudioObjectID for the
    /// AudioTransportManager is returned to the caller as the property's data. Note
    /// that an error is not returned if the bundle ID doesn't refer to any
    /// AudioTransportManagers. Rather, this property will return
    /// kAudioObjectUnknown as the value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslateBundleIDToTransportManager   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("tmbi"); // 'tmbi',
    /// <summary>
    /// An array of AudioObjectIDs that represent all the 
    /// AudioBox objects currently provided by the system.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyBoxList                               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("box#"); // 'box#',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the AudioBox
    /// that has the given UID. The UID is passed in via the qualifier as a CFString
    /// while the AudioObjectID for the AudioBox is returned to the caller as the
    /// property's data. Note that an error is not returned if the UID doesn't refer
    /// to any AudioBoxes. Rather, this property will return kAudioObjectUnknown
    /// as the value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslateUIDToBox                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("uidb"); // 'uidb',
    /// <summary>
    /// An array of AudioObjectIDs that represent all the AudioClockDevice objects 
    /// currently provided by the system.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyClockDeviceList                       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("clk#"); // 'clk#',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the AudioClockDevice
    /// that has the given UID. The UID is passed in via the qualifier as a CFString
    /// while the AudioObjectID for the AudioClockDevice is returned to the caller
    /// as the property's data. Note that an error is not returned if the UID doesn't
    /// refer to any AudioClockDevice. Rather, this property will return 
    /// kAudioObjectUnknown as the value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslateUIDToClockDevice             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("uidc"); // 'uidc',
    /// <summary>
    /// A UInt32 where 1 means that the current process contains the main instance
    /// of the HAL. The main instance of the HAL is the only instance in which
    /// plug-ins should save/restore their devices' settings.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyProcessIsMain                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("main"); // 'main',
    /// <summary>
    /// A UInt32 whose value will be non-zero if the HAL is either in the midst of
    /// initializing or in the midst of exiting the process.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyIsInitingOrExiting                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("inot"); // 'inot',
    /// <summary>
    /// This property exists so that clients can tell the HAL when they are changing
    /// the effective user ID of the process. The way it works is that a client will
    /// set the value of this property and the HAL will flush all its cached per-
    /// user preferences such as the default devices. The value of this property is
    /// a UInt32, but its value has no currently defined meaning and clients may
    /// pass any value when setting it to trigger the cache flush.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyUserIDChanged                         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("euid"); // 'euid',
    /// <summary>
    /// A UInt32 where a non-zero value indicates that all data coming into the process for all devices will be silent. 
    /// A value of 0 indicates that input data will be received normally.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyProcessInputMute                      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pmin"); // 'pmin',
    /// <summary>
    /// A UInt32 where a non-zero value indicates that the audio of the process will be heard. 
    /// A value of 0 indicates that all audio in the process will not be heard.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyProcessIsAudible                      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pmut"); // 'pmut',
    /// <summary>
    /// A UInt32 where 1 means that the process will allow the CPU to idle sleep
    /// even if there is audio IO in progress. 
    /// A 0 means that the CPU will not be allowed to idle sleep.
    /// Note that this property won't affect when the CPU is forced to sleep.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertySleepingIsAllowed                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("slep"); // 'slep',
    /// <summary>
    /// A UInt32 where 1 means that this process wants the HAL to unload itself
    /// after a period of inactivity where there are no IOProcs and no listeners
    /// registered with any AudioObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyUnloadingIsAllowed                    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("unld"); // 'unld',
    /// <summary>
    /// A UInt32 where 1 means that this process wants the HAL to automatically take
    /// hog mode and 0 means that the HAL should not automatically take hog mode on
    /// behalf of the process. Processes that only ever use the default device are
    /// the sort of that should set this property's value to 0.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyHogModeIsAllowed                      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("hogr"); // 'hogr',
    /// <summary>
    /// A UInt32 where a value other than 0 indicates that the login session of the
    /// user of the process is either an active console session or a headless
    /// session.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyUserSessionIsActiveOrHeadless         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("user"); // 'user',
    /// <summary>
    /// A UInt32 whose value has no meaning. Rather, this property exists so that
    /// clients can be informed when the service has been reset for some reason.
    /// When a reset happens, any state the client has, such as cached data or
    /// added listeners, must be re-established by the client.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyServiceRestarted                      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("srst"); // 'srst',
    /// <summary>
    /// A UInt32 whose values are drawn from the AudioHardwarePowerHint enum above.
    /// Only those values are allowed. This property allows a process to indicate how
    /// aggressive the system can be with optimizations that save power. The default
    /// value is kAudioHardwarePowerHintNone. Note that the value of this
    /// property can be set in an application's info.plist using the key,
    /// "AudioHardwarePowerHint". The values for this key are the strings that
    /// correspond to the values in the Power Hints enum.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyPowerHint                             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("powh"); // 'powh',
    /// <summary>
    /// An array of AudioObjectIDs that represent the Process objects 
    /// for all client processes currently connected to the system.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyProcessObjectList                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("prs#"); // 'prs#',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the Process object
    /// that has the given PID. The PID is passed in via the qualifier as a pid_t
    /// while the AudioObjectID for the Process is returned to the caller as the
    /// property's data. Note that an error is not returned if the PID doesn't refer
    /// to any Process. Rather, this property will return kAudioObjectUnknown
    /// as the value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslatePIDToProcessObject           = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("id2p"); // 'id2p',
    /// <summary>
    /// An array of AudioObjectIDs that represent the Tap objects on the system.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTapList                               = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("tps#"); // 'tps#',
    /// <summary>
    /// This property fetches the AudioObjectID that corresponds to the AudioTap
    /// that has the given UID. The UID is passed in via the qualifier as a CFString
    /// while the AudioObjectID for the AudioTap is returned to the caller as the 
    /// property's data. Note that an error is not returned if the UID doesn't refer 
    /// to any AudioTap. Rather, this property will return kAudioObjectUnknown as the 
    /// value of the property.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioHardwarePropertyTranslateUIDToTap                     = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("uidt"); // 'uidt',
}