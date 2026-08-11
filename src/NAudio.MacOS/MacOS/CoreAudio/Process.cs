// This interop definition was derived from the file AudioHardware.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// The Process class contains information about a client process connected to the HAL.
/// </summary>
public sealed class Process : AudioObject
{
    internal Process(AudioObjectID objectID) : base(objectID) { }

    /// <summary>A pid indicating the process ID associated with the process.</summary>
    public int PID => unchecked((int)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(ProcessProperties.kAudioProcessPropertyPID)));

    /// <summary>A <see cref="string"/> that contains the bundle ID of the process.</summary>
    public string BundleID => GetStringPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(ProcessProperties.kAudioProcessPropertyBundleID));

    /// <summary>
    /// An array of <see cref="AudioDevice"/>s that represent the devices currently used by the
    /// process for input or used by the process for output. The scope will select
    /// the input or output device list.
    /// </summary>
    public AudioDevice[] GetDevices(AudioObjectPropertyScope scope)
    {
        if (scope != AudioObjectPropertyScopeConstants.Input &&
            scope != AudioObjectPropertyScopeConstants.Output)
        {
            throw new ArgumentException("The scope can only be Input or Output", "scope");
        }
        else
        {
            return GetAudioObjectValues(
                AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                    ProcessProperties.kAudioProcessPropertyDevices,
                    scope
                ),
                AudioObjectsConstructors.ConstructDevice
            );
        }
    }

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="false"/> indicates that the process is not running any
    /// IO or there is not any active input streams, and a value of <see langword="true"/> indicates that
    /// the process is running IO and there is at least one active input stream.
    /// </summary>
    public bool IsRunning => GetUIntPropertyValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            ProcessProperties.kAudioProcessPropertyIsRunning
        )) == 1U;

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="false"/> indicates that the process is not running any
    /// IO or there is not any active input streams, and a value of <see langword="true"/> indicates that
    /// the process is running IO and there is at least one active input stream.
    /// </summary>
    public bool IsRunningInput => GetUIntPropertyValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            ProcessProperties.kAudioProcessPropertyIsRunningInput
        )) == 1U;

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="false"/> indicates that the process is not running any
    /// IO or there is not any active output streams, and a value of <see langword="true"/> indicates that
    /// the process is running IO and there is at least one active output stream.
    /// </summary>
    public bool IsRunningOutput => GetUIntPropertyValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            ProcessProperties.kAudioProcessPropertyIsRunningOutput
        )) == 1U;
}