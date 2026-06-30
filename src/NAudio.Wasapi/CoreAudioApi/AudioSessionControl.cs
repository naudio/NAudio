// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi;

/// <summary>
/// AudioSessionControl object for information
/// regarding an audio session
/// </summary>
public class AudioSessionControl : IDisposable
{
    private static readonly Guid IID_IAudioSessionEvents = new("24918ACC-64B3-37C1-8CA9-74A66E9957A8");

    private IAudioSessionControl audioSessionControlInterface;
    private IAudioSessionControl2 audioSessionControlInterface2;
    private readonly bool ownsInterface;
    private readonly Dictionary<IAudioSessionEventsHandler, AudioSessionEventsCallback> audioSessionEventCallbacks = new();
    private readonly object eventCallbackLock = new();

    // ComWrappers CCWs return a distinct IntPtr per interface (and a separate vtable
    // for IUnknown). Registration APIs that expect IAudioSessionEvents* must receive
    // the QI'd interface pointer, not the raw IUnknown — passing IUnknown dispatches
    // against the wrong vtable on the WASAPI worker thread (STATUS_ACCESS_VIOLATION).
    private static IntPtr QueryEventsInterface(AudioSessionEventsCallback callback)
    {
        var unknownPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(callback, CreateComInterfaceFlags.None);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknownPtr, in IID_IAudioSessionEvents, out var ifacePtr));
            return ifacePtr;
        }
        finally
        {
            Marshal.Release(unknownPtr);
        }
    }

    /// <summary>
    /// Creates a new AudioSessionControl — ownership of the COM pointer is transferred.
    /// </summary>
    /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
    internal AudioSessionControl(IntPtr nativePointer)
    {
        try
        {
            audioSessionControlInterface = (IAudioSessionControl)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                nativePointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(nativePointer);
        }
        ownsInterface = true;
        audioSessionControlInterface2 = audioSessionControlInterface as IAudioSessionControl2;

        if (audioSessionControlInterface is IAudioMeterInformation meters)
            AudioMeterInformation = new AudioMeterInformation(meters);
        if (audioSessionControlInterface is ISimpleAudioVolume volume)
            SimpleAudioVolume = new SimpleAudioVolume(volume);
    }

    /// <summary>
    /// Creates a new AudioSessionControl from a borrowed interface (e.g. COM callback).
    /// This instance does not own the COM pointer.
    /// </summary>
    /// <param name="borrowed">IAudioSessionControl obtained from a COM callback</param>
    internal AudioSessionControl(IAudioSessionControl borrowed)
    {
        audioSessionControlInterface = borrowed;
        audioSessionControlInterface2 = borrowed as IAudioSessionControl2;
        ownsInterface = false;

        if (audioSessionControlInterface is IAudioMeterInformation meters)
            AudioMeterInformation = new AudioMeterInformation(meters);
        if (audioSessionControlInterface is ISimpleAudioVolume volume)
            SimpleAudioVolume = new SimpleAudioVolume(volume);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        lock (eventCallbackLock)
        {
            foreach (var callback in audioSessionEventCallbacks.Values)
            {
                var ptr = QueryEventsInterface(callback);
                try
                {
                    audioSessionControlInterface.UnregisterAudioSessionNotification(ptr);
                }
                finally
                {
                    Marshal.Release(ptr);
                }
            }
            audioSessionEventCallbacks.Clear();
        }
        if (audioSessionControlInterface != null)
        {
            if (ownsInterface && (object)audioSessionControlInterface is ComObject co)
            {
                co.FinalRelease();
            }
            audioSessionControlInterface = null;
            audioSessionControlInterface2 = null;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Audio meter information of the audio session.
    /// </summary>
    public AudioMeterInformation AudioMeterInformation { get; }

    /// <summary>
    /// Simple audio volume of the audio session (for volume and mute status).
    /// </summary>
    public SimpleAudioVolume SimpleAudioVolume { get; }

    /// <summary>
    /// The current state of the audio session.
    /// </summary>
    public AudioSessionState State
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetState(out var state));

            return state;
        }
    }

    /// <summary>
    /// The name of the audio session.
    /// </summary>
    public string DisplayName
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetDisplayName(out var displayName));

            return displayName;
        }
        set
        {
            if (value != String.Empty)
            {
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.SetDisplayName(value, Guid.Empty));
            }
        }
    }

    /// <summary>
    /// the path to the icon shown in the mixer.
    /// </summary>
    public string IconPath
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetIconPath(out var iconPath));

            return iconPath;
        }
        set
        {
            if (value != String.Empty)
            {
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.SetIconPath(value, Guid.Empty));
            }
        }
    }

    /// <summary>
    /// The session identifier of the audio session.
    /// </summary>
    public string GetSessionIdentifier
    {
        get
        {
            if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.GetSessionIdentifier(out var str));
            return str;
        }
    }

    /// <summary>
    /// The session instance identifier of the audio session.
    /// </summary>
    public string GetSessionInstanceIdentifier
    {
        get
        {
            if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.GetSessionInstanceIdentifier(out var str));
            return str;
        }
    }

    /// <summary>
    /// The process identifier of the audio session.
    /// </summary>
    public uint GetProcessID
    {
        get
        {
            if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.GetProcessId(out var pid));
            return pid;
        }
    }

    /// <summary>
    /// Is the session a system sounds session.
    /// </summary>
    public bool IsSystemSoundsSession
    {
        get
        {
            if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
            return (audioSessionControlInterface2.IsSystemSoundsSession() == 0);
        }
    }

    /// <summary>
    /// Enables or disables the default stream attenuation (auto-ducking) that the system
    /// applies to other audio streams when this is a communications session. By default
    /// Windows ducks other streams while a communications session is active; pass
    /// <c>true</c> to opt out so that other streams (for example separate communication
    /// devices) are not attenuated.
    /// </summary>
    /// <param name="optOut"><c>true</c> to disable the system ducking experience for this
    /// session; <c>false</c> to use the default system ducking behaviour.</param>
    /// <remarks>Requires <c>IAudioSessionControl2</c> (Windows 7 and later). Per the
    /// Windows SDK, this should be called before the session is made active.</remarks>
    public void SetDuckingPreference(bool optOut)
    {
        if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
        CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.SetDuckingPreference(optOut));
    }

    /// <summary>
    /// the grouping param for an audio session grouping
    /// </summary>
    /// <returns></returns>
    public Guid GetGroupingParam()
    {
        CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetGroupingParam(out var groupingId));

        return groupingId;
    }

    /// <summary>
    /// For chanigng the grouping param and supplying the context of said change
    /// </summary>
    /// <param name="groupingId"></param>
    /// <param name="context"></param>
    public void SetGroupingParam(Guid groupingId, Guid context)
    {
        CoreAudioException.ThrowIfFailed(audioSessionControlInterface.SetGroupingParam(groupingId, context));
    }

    /// <summary>
    /// Registers an event client for callbacks. Multiple clients can be registered;
    /// registering a client that is already registered has no effect.
    /// </summary>
    /// <param name="eventClient">The handler to register.</param>
    public void RegisterEventClient(IAudioSessionEventsHandler eventClient)
    {
        lock (eventCallbackLock)
        {
            if (audioSessionEventCallbacks.ContainsKey(eventClient))
            {
                return;
            }
            var callback = new AudioSessionEventsCallback(eventClient);
            var ptr = QueryEventsInterface(callback);
            try
            {
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.RegisterAudioSessionNotification(ptr));
            }
            finally
            {
                Marshal.Release(ptr);
            }
            audioSessionEventCallbacks.Add(eventClient, callback);
        }
    }

    /// <summary>
    /// Unregisters an event client from receiving callbacks. Unregistering a client that
    /// is not registered has no effect.
    /// </summary>
    /// <param name="eventClient">The handler to unregister.</param>
    public void UnRegisterEventClient(IAudioSessionEventsHandler eventClient)
    {
        lock (eventCallbackLock)
        {
            if (!audioSessionEventCallbacks.TryGetValue(eventClient, out var callback))
            {
                return;
            }
            var ptr = QueryEventsInterface(callback);
            try
            {
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.UnregisterAudioSessionNotification(ptr));
            }
            finally
            {
                Marshal.Release(ptr);
            }
            audioSessionEventCallbacks.Remove(eventClient);
        }
    }
}
