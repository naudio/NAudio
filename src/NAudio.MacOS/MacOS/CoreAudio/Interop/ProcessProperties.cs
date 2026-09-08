// This interop definition was derived from the file AudioHardware.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// Process Properties <br />
/// Processes AudioObjectPropertySelector values provided by the Process class.
/// </summary>
internal static class ProcessProperties
{
    /// <summary>A pid_t indicating the process ID associated with the process.</summary>
    public static readonly AudioObjectPropertySelector kAudioProcessPropertyPID             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ppid"); // 'ppid'
    /// <summary>
    /// A CFString that contains the bundle ID of the process. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioProcessPropertyBundleID        = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pbid"); // 'pbid'
    /// <summary>
    /// An array of AudioObjectIDs that represent the devices currently used by the
    /// process for input or used by the process for output. The scope will select
    /// the input or output device list.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioProcessPropertyDevices         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pdv#"); // 'pdv#'
    /// <summary>
    /// A UInt32 where a value of 0 indicates that there is not audio IO in progress
    /// in the process, and a value of 1 indicates that there is audio IO in progress
    /// in the process. Note that audio IO may in progress even if no input or output
    /// streams are active.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioProcessPropertyIsRunning       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("pir?"); // 'pir?'
    /// <summary>
    /// A UInt32 where a value of 0 indicates that the process is not running any
    /// IO or there is not any active input streams, and a value of 1 indicates that
    /// the process is running IO and there is at least one active input stream.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioProcessPropertyIsRunningInput  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("piri"); // 'piri'
    /// <summary>
    /// A UInt32 where a value of 0 indicates that the process is not running any
    /// IO or there is not any active output streams, and a value of 1 indicates that
    /// the process is running IO and there is at least one active output stream.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioProcessPropertyIsRunningOutput = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("piro"); // 'piro'
}

