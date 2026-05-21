using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Wave.DirectSoundInterop
{
    /// <summary>
    /// IDirectSoundNotify — sets event-based playback-cursor notifications on a
    /// secondary buffer. Acquired via QI from <see cref="IDirectSoundBuffer"/>.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IDirectSoundNotify (dsound.h).
    /// </remarks>
    [GeneratedComInterface]
    [Guid("b0210783-89cd-11d0-af08-00a0c925cd16")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDirectSoundNotify
    {
        /// <summary>
        /// Sets the notification positions. <paramref name="pcPositionNotifies"/> must be a
        /// pinned pointer to an array of <see cref="DirectSoundBufferPositionNotify"/>
        /// (length <paramref name="dwPositionNotifies"/>).
        /// </summary>
        [PreserveSig]
        int SetNotificationPositions(uint dwPositionNotifies, IntPtr pcPositionNotifies);
    }
}
