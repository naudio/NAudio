using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi;

/// <summary>
/// Audio Endpoint Volume.
/// Volume change notifications from COM arrive on a background thread.
/// If a <see cref="SynchronizationContext"/> is captured at construction time,
/// notifications are marshaled to that context (e.g. the UI thread).
/// </summary>
public class AudioEndpointVolume : IDisposable
{
    private static readonly Guid IID_IAudioEndpointVolumeCallback = new("657804FA-D6AD-4496-8A60-352752AF4F89");

    private IAudioEndpointVolume? audioEndPointVolume;

    private IAudioEndpointVolume EndpointVolume => audioEndPointVolume ?? throw new ObjectDisposedException(nameof(AudioEndpointVolume));
    private readonly SynchronizationContext? syncContext;
    private AudioEndpointVolumeCallback? callBack;

    private Guid notificationGuid = Guid.Empty;

    /// <summary>
    /// GUID to pass to AudioEndpointVolumeCallback
    /// </summary>
    public Guid NotificationGuid
    {
        get => notificationGuid;
        set => notificationGuid = value;
    }

    /// <summary>
    /// On Volume Notification
    /// </summary>
    public event AudioEndpointVolumeNotificationDelegate? OnVolumeNotification;

    /// <summary>
    /// Volume Range
    /// </summary>
    public AudioEndpointVolumeVolumeRange VolumeRange { get; }

    /// <summary>
    /// Hardware Support
    /// </summary>
    public EEndpointHardwareSupport HardwareSupport { get; }

    /// <summary>
    /// Step Information
    /// </summary>
    public AudioEndpointVolumeStepInformation StepInformation { get; }

    /// <summary>
    /// Channels
    /// </summary>
    public AudioEndpointVolumeChannels Channels { get; }

    /// <summary>
    /// Master Volume Level
    /// </summary>
    public float MasterVolumeLevel
    {
        get
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.GetMasterVolumeLevel(out var result));
            return result;
        }
        set
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.SetMasterVolumeLevel(value, ref notificationGuid));
        }
    }

    /// <summary>
    /// Master Volume Level Scalar
    /// </summary>
    public float MasterVolumeLevelScalar
    {
        get
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.GetMasterVolumeLevelScalar(out var result));
            return result;
        }
        set
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.SetMasterVolumeLevelScalar(value, ref notificationGuid));
        }
    }

    /// <summary>
    /// Mute
    /// </summary>
    public bool Mute
    {
        get
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.GetMute(out var result));
            return result;
        }
        set
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.SetMute(value, ref notificationGuid));
        }
    }

    /// <summary>
    /// Volume Step Up
    /// </summary>
    public void VolumeStepUp()
    {
        CoreAudioException.ThrowIfFailed(EndpointVolume.VolumeStepUp(ref notificationGuid));
    }

    /// <summary>
    /// Volume Step Down
    /// </summary>
    public void VolumeStepDown()
    {
        CoreAudioException.ThrowIfFailed(EndpointVolume.VolumeStepDown(ref notificationGuid));
    }

    /// <summary>
    /// Creates a new Audio endpoint volume
    /// </summary>
    /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
    internal AudioEndpointVolume(IntPtr nativePointer)
    {
        syncContext = SynchronizationContext.Current;
        try
        {
            audioEndPointVolume = (IAudioEndpointVolume)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                nativePointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(nativePointer);
        }
        Channels = new AudioEndpointVolumeChannels(audioEndPointVolume);
        StepInformation = new AudioEndpointVolumeStepInformation(audioEndPointVolume);
        CoreAudioException.ThrowIfFailed(EndpointVolume.QueryHardwareSupport(out var hardwareSupp));
        HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
        VolumeRange = new AudioEndpointVolumeVolumeRange(audioEndPointVolume);
        callBack = new AudioEndpointVolumeCallback(this);
        var callBackPtr = QueryCallbackInterface(callBack);
        try
        {
            CoreAudioException.ThrowIfFailed(EndpointVolume.RegisterControlChangeNotify(callBackPtr));
        }
        finally
        {
            Marshal.Release(callBackPtr);
        }
    }

    internal void FireNotification(AudioVolumeNotificationData notificationData)
    {
        var handler = OnVolumeNotification;
        if (handler != null)
        {
            if (syncContext != null)
            {
                syncContext.Post(_ => handler(notificationData), null);
            }
            else
            {
                handler(notificationData);
            }
        }
    }

    // ComWrappers CCWs return a distinct IntPtr per interface (including a separate
    // vtable for IUnknown). WASAPI's RegisterControlChangeNotify expects a pointer
    // whose vtable starts with the IAudioEndpointVolumeCallback methods, not IUnknown,
    // so QI for the specific IID before handing the pointer to native — passing the
    // raw IUnknown pointer dispatches against the wrong vtable and access-violates on
    // the worker thread.
    private static IntPtr QueryCallbackInterface(AudioEndpointVolumeCallback callback)
    {
        var unknownPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(callback, CreateComInterfaceFlags.None);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknownPtr, in IID_IAudioEndpointVolumeCallback, out var ifacePtr));
            return ifacePtr;
        }
        finally
        {
            Marshal.Release(unknownPtr);
        }
    }

    #region IDisposable Members

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        if (callBack != null)
        {
            var callBackPtr = QueryCallbackInterface(callBack);
            try
            {
                EndpointVolume.UnregisterControlChangeNotify(callBackPtr);
            }
            finally
            {
                Marshal.Release(callBackPtr);
            }
            callBack = null;
        }
        // Deterministic release is important: in exclusive mode the device cannot be
        // re-opened until all COM references are released.
        if (audioEndPointVolume != null)
        {
            if ((object)audioEndPointVolume is ComObject co)
            {
                co.FinalRelease();
            }
            audioEndPointVolume = null;
        }
        GC.SuppressFinalize(this);
    }

    #endregion

}
