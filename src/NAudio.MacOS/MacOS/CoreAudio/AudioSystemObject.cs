// This interop definition was derived from the file AudioHardware.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System;
using System.Diagnostics;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio.Interop;
using NAudio.MacOS.CoreFoundationApi;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the audio system object,
/// that is the base API where all other Core Audio API's are accessed from. <br />
/// Inherits from the <see cref="AudioObject"/> class, and only has a single instance,
/// queried by the <see cref="Instance"/> field.
/// </summary>
public sealed class AudioSystemObject : AudioObject
{
    private PropertyListenerHandle devicesListChanged;
    private PropertyListenerHandle defaultInputDeviceChanged;
    private PropertyListenerHandle defaultOutputDeviceChanged;
    private PropertyListenerHandle defaultSystemOutputDeviceChanged;

    private AudioSystemObject(AudioObjectID id)
        : base(id)
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        devicesListChanged = null;
        defaultInputDeviceChanged = null;
        defaultOutputDeviceChanged = null;
        defaultSystemOutputDeviceChanged = null;
        // The below listener does never itself invalidate - 
        // all the others will need a re-initialization, which will happen
        // below.
        var serviceRestartedHandle = CreateNotificationHandler(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyServiceRestarted
            )
        );
        OnServiceRestarted(this);
        serviceRestartedHandle.Event += OnServiceRestarted;
        // By default, the HAL assigns exclusive audio sessions to all the IO procedures - 
        // but because we are a lib, let the user decide what to do,
        // but if they want to, they can enable this global flag again.
        try { HogModeIsAllowed = false; } catch (CoreAudioException) { }
    }

    private void OnServiceRestarted(AudioObject thisObj)
    {
        try
        {
            MacUtils.EnsureDisposableObjectsDisposed(
                devicesListChanged,
                defaultInputDeviceChanged,
                defaultOutputDeviceChanged,
                defaultSystemOutputDeviceChanged
            );
        }
        catch (AggregateException ex)
        {
            // Report to debug any exception of any failure that occurs
            // while we dispose the older property listeners.
            Debug.WriteLine("[AudioSystemObject] Exception occurred while disposing the older property listeners: " + ex);
        }
        devicesListChanged = null;
        defaultInputDeviceChanged = null;
        defaultOutputDeviceChanged = null;
        defaultSystemOutputDeviceChanged = null;
        devicesListChanged = CreateNotificationHandler(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyDevices
            )
        );
        devicesListChanged.Event += InvokeDeviceListChanged;
        defaultInputDeviceChanged = CreateNotificationHandler(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyDefaultInputDevice
            )
        );
        defaultInputDeviceChanged.Event += InvokeDefaultInputDeviceChanged;
        defaultOutputDeviceChanged = CreateNotificationHandler(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyDefaultOutputDevice
            )
        );
        defaultOutputDeviceChanged.Event += InvokeDefaultOutputDeviceChanged;
        defaultSystemOutputDeviceChanged = CreateNotificationHandler(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyDefaultSystemOutputDevice
            )
        );
        defaultSystemOutputDeviceChanged.Event += InvokeDefaultSystemOutputDeviceChanged;
        // Finally, invoke the service restarted event. 
        // We do not care whether this invocation throws exception,
        // because all the critical code that was needed to be executed
        // it was executed.
        ServiceRestarted?.Invoke(thisObj);
    }

    /// <summary>
    /// Gets the single and only instance of the <see cref="AudioSystemObject"/> class.
    /// </summary>
    public static readonly AudioSystemObject Instance = new(AudioObjectID.SystemObject);

    /// <summary>
    /// The <see cref="AudioDevice"/> of the default input AudioDevice.
    /// </summary>
    public AudioDevice DefaultInputDevice => GetAudioObjectValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSystemObjectProperties.kAudioHardwarePropertyDefaultInputDevice
        ),
        AudioObjectsConstructors.ConstructDevice
    );

    /// <summary>
    /// The <see cref="AudioDevice"/> of the default output AudioDevice.
    /// </summary>
    public AudioDevice DefaultOutputDevice => GetAudioObjectValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSystemObjectProperties.kAudioHardwarePropertyDefaultOutputDevice
        ),
        AudioObjectsConstructors.ConstructDevice
    );

    /// <summary>
    /// The output <see cref="AudioDevice"/> to use for system 
    /// related sound from the alert sound to digital call progress.
    /// </summary>
    public AudioDevice DefaultSystemOutputDevice => GetAudioObjectValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSystemObjectProperties.kAudioHardwarePropertyDefaultSystemOutputDevice
        ),
        AudioObjectsConstructors.ConstructDevice
    );

    /// <summary>
    /// Gets an array of <see cref="AudioDevice"/> objects that represent 
    /// all the audio devices currently available in the system.
    /// </summary>
    public AudioDevice[] Devices => GetAudioObjectValues(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSystemObjectProperties.kAudioHardwarePropertyDevices
        ),
        AudioObjectsConstructors.ConstructDevice
    );

    /// <summary>
    /// This method fetches the <see cref="AudioDevice"/> that 
    /// corresponds to the physical audio device that has the given UID. <br />
    /// The UID is passed in via the <paramref name="uid"/> parameter
    /// while the <see cref="AudioDevice"/> is returned to the caller as the return value. <br />
    /// Note that an error is not returned if the UID doesn't refer to any AudioDevices. <br />
    /// Rather, this method will return <see langword="null"/>.
    /// </summary>
    /// <param name="uid">The UID of the device to query.</param>
    /// <returns>
    /// The <see cref="AudioDevice"/> instance of the device referred by the specified 
    /// <paramref name="uid"/>, or <see langword="null"/> if it does not refer to any device.
    /// </returns>
    public AudioDevice ConvertUIDToDevice(string uid)
    {
        ArgumentException.ThrowIfNullOrEmpty(uid);
        using var convertedCF = CFString.CreateFromString(uid);
        return GetAudioObjectValue(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyTranslateUIDToDevice
            ),
            AudioObjectsConstructors.ConstructDevice,
            [convertedCF.DangerousGetObject()]
        );
    }

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="true"/> indicates that <see cref="AudioDevice"/>s should mix
    /// stereo signals down to mono. Note that the two channels on the device that
    /// comprise the stereo signal are defined on the device by
    /// <see cref="AudioDevice.GetPreferredChannelsForStereo(AudioObjectPropertyScope)"/>.
    /// </summary>
    public bool MixStereoToMono
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyMixStereoToMono)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyMixStereoToMono), value ? 1U : 0U);
    }

    /// <summary>
    /// An array of <see cref="AudioClockDevice"/> that represent all 
    /// the audio clock device objects currently provided by the system.
    /// </summary>
    public AudioClockDevice[] ClockDeviceList => GetAudioObjectValues(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyClockDeviceList),
        AudioObjectsConstructors.ConstructClockDevice
    );

    /// <summary>
    /// This property fetches the <see cref="AudioClockDevice"/> that 
    /// corresponds to the audio clock device that has the given UID. <br />
    /// The UID is passed in via the <paramref name="uid"/> parameter
    /// while the <see cref="AudioClockDevice"/> is returned to the caller
    /// as the method's return value. <br />
    /// Note that an error is not returned if the UID doesn't refer to any <see cref="AudioClockDevice"/>. <br />
    /// Rather, this method will return <see langword="null"/>.
    /// </summary>
    public AudioClockDevice ConvertUIDToClockDevice(string uid)
    {
        ArgumentException.ThrowIfNullOrEmpty(uid);
        using var convertedCF = CFString.CreateFromString(uid);
        return GetAudioObjectValue(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyTranslateUIDToClockDevice
            ),
            AudioObjectsConstructors.ConstructClockDevice,
            [convertedCF.DangerousGetObject()]
        );
    }

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that 
    /// the current process contains the main instance of the HAL. 
    /// The main instance of the HAL is the only instance in which
    /// plug-ins should save/restore their devices' settings.
    /// </summary>
    public bool ProcessIsMain => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyProcessIsMain)) == 1U;

    /// <summary>
    /// A <see cref="bool"/> whose value will be <see langword="true"/> if the HAL is either in the midst of
    /// initializing or in the midst of exiting the process.
    /// </summary>
    public bool IsInitingOrExiting => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyIsInitingOrExiting)) != 0U;

    /// <summary>
    /// This method exists so that clients can tell the HAL when they are changing the effective user ID of the process. 
    /// The way it works is that a client will call this method and the HAL will flush all its cached per-user preferences such as the default devices. 
    /// Clients calling this are using it to trigger the cache flush.
    /// </summary>
    public void OnUserIDChanged()
    {
        SetUIntPropertyValue(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSystemObjectProperties.kAudioHardwarePropertyUserIDChanged
            ),
            0U
        );
    }

    /// <summary>
    /// A <see cref="bool"/> where a <see langword="true"/> value indicates that all data coming into the process for all devices will be silent. 
    /// A value of <see langword="false"/> indicates that input data will be received normally.
    /// </summary>
    public bool ProcessInputMute
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyProcessInputMute)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyProcessInputMute), value ? 1U : 0U);
    }

    /// <summary>
    /// A <see cref="bool"/> where a <see langword="true"/> value indicates that the audio of the process will be heard. 
    /// A value of <see langword="false"/> indicates that all audio in the process will not be heard.
    /// </summary>
    public bool ProcessIsAudible
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyProcessIsAudible)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyProcessIsAudible), value ? 1U : 0U);
    }

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that the process will allow the CPU to idle sleep
    /// even if there is audio IO in progress. 
    /// A <see langword="false"/> means that the CPU will not be allowed to idle sleep.
    /// Note that this property won't affect when the CPU is forced to sleep.
    /// </summary>
    public bool SleepingIsAllowed
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertySleepingIsAllowed)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertySleepingIsAllowed), value ? 1U : 0U);
    }

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that this process wants the HAL to unload itself
    /// after a period of inactivity where there are no IOProcs and no listeners
    /// registered with any AudioObject.
    /// </summary>
    public bool UnloadingIsAllowed
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyUnloadingIsAllowed)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyUnloadingIsAllowed), value ? 1U : 0U);
    }

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that this process wants the HAL to automatically take
    /// hog mode and <see langword="false"/> means that the HAL should not automatically take hog mode on
    /// behalf of the process. Processes that only ever use the default device are
    /// the sort of that should set this property's value to 0.
    /// </summary>
    /// <remarks>
    /// This property corresponds to Shared/Exclusive mode on WASAPI on Windows. 
    /// Setting this to <see langword="true"/> enables the exclusive mode for all audio sessions.
    /// </remarks>
    public bool HogModeIsAllowed
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyHogModeIsAllowed)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyHogModeIsAllowed), value ? 1U : 0U);
    }

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="true"/> indicates that the login session of the
    /// user of the process is either an active console session or a headless
    /// session.
    /// </summary>
    public bool UserSessionIsActiveOrHeadless
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyUserSessionIsActiveOrHeadless)) != 0U;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyUserSessionIsActiveOrHeadless), value ? 1U : 0U);
    }

    /// <summary>
    /// A UInt32 whose values are drawn from the AudioHardwarePowerHint enum above.
    /// Only those values are allowed. This property allows a process to indicate how
    /// aggressive the system can be with optimizations that save power. The default
    /// value is kAudioHardwarePowerHintNone. Note that the value of this
    /// property can be set in an application's info.plist using the key,
    /// "AudioHardwarePowerHint". The values for this key are the strings that
    /// correspond to the values in the Power Hints enum.
    /// </summary>
    public AudioHardwarePowerHint PowerHint
    {
        get => (AudioHardwarePowerHint)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyPowerHint));
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyPowerHint), (uint)value);
    }

    /// <summary>
    /// An array of <see cref="Process"/> objects for all client processes currently connected to the system.
    /// </summary>
    public Process[] ProcessObjectList => GetAudioObjectValues(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyProcessObjectList), AudioObjectsConstructors.ConstructProcess);

    /// <summary>
    /// This method fetches the <see cref="Process"/> object that has the given PID. 
    /// The PID is passed in to the <paramref name="pid"/> parameter while the <see cref="Process"/> is returned to the caller as the method's data. <br />
    /// Note that an error is not returned if the PID doesn't refer to any <see cref="Process"/>. <br />
    /// Rather, this method will return <see langword="null"/>.
    /// </summary>
    /// <param name="pid">The PID of the audio process object to query.</param>
    public Process ConvertPIDToProcessObject(int pid) => GetAudioObjectValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioSystemObjectProperties.kAudioHardwarePropertyTranslatePIDToProcessObject), AudioObjectsConstructors.ConstructProcess, [pid]);

    #region Events

    /// <summary>
    /// Event that clients can be informed when the service has been reset for some reason.
    /// When a reset happens, any state the client has, such as cached data or added listeners, must be re-established by the client.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate ServiceRestarted;

    private void InvokeDeviceListChanged(AudioObject o) => DeviceListChanged?.Invoke(o);

    private void InvokeDefaultInputDeviceChanged(AudioObject o) => DefaultInputDeviceChanged?.Invoke(o);

    private void InvokeDefaultOutputDeviceChanged(AudioObject o) => DefaultOutputDeviceChanged?.Invoke(o);

    private void InvokeDefaultSystemOutputDeviceChanged(AudioObject o) => DefaultSystemOutputDeviceChanged?.Invoke(o);

    /// <summary>
    /// Event that clients can be informed when the list of devices has been changed. <br />
    /// This particular list changes when a device is added or removed from the HAL.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate DeviceListChanged;

    /// <summary>
    /// Event that clients can be informed when the default input device is changed.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate DefaultInputDeviceChanged;

    /// <summary>
    /// Event that clients can be informed when the default output device is changed.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate DefaultOutputDeviceChanged;

    /// <summary>
    /// Event that clients can be informed when the default system output device is changed.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate DefaultSystemOutputDeviceChanged;

    #endregion
}