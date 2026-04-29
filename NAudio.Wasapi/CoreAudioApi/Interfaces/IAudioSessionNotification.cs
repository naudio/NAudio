using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IAudioSessionNotification interface
    /// Defined in AudioPolicy.h
    /// </summary>
    [GeneratedComInterface,
    Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IAudioSessionNotification
    {

        /// <summary>
        ///
        /// </summary>
        /// <param name="newSession">session being added</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int OnSessionCreated(IAudioSessionControl newSession);
    }
}
