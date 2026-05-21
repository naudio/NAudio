using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Wave.DirectSoundInterop
{
    /// <summary>
    /// IDirectSoundBuffer — a sound buffer attached to an <see cref="IDirectSound"/> device.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IDirectSoundBuffer (dsound.h).
    /// Invoked from <see cref="DirectSoundOut"/>: <see cref="GetCaps"/>,
    /// <see cref="GetCurrentPosition"/>, <see cref="GetStatus"/>, <see cref="Lock"/>,
    /// <see cref="Play"/>, <see cref="SetCurrentPosition"/>, <see cref="Stop"/>,
    /// <see cref="Unlock"/>, <see cref="Restore"/>. The remaining slots are declared with
    /// <see cref="IntPtr"/> placeholders to preserve vtable order — in particular,
    /// <c>SetFormat</c>'s <c>WAVEFORMATEX</c> parameter (a managed reference type) is
    /// dead code: the wave format reaches the secondary buffer via
    /// <see cref="BufferDescription.lpwfxFormat"/> (a pinned <see cref="GCHandle"/>),
    /// not via <c>SetFormat</c>.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("279AFA85-4981-11CE-A521-0020AF0BE560")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDirectSoundBuffer
    {
        [PreserveSig]
        int GetCaps(ref BufferCaps pDSBufferCaps);

        [PreserveSig]
        int GetCurrentPosition(out uint pdwCurrentPlayCursor, out uint pdwCurrentWriteCursor);

        [PreserveSig]
        int GetFormat(IntPtr pwfxFormat, int dwSizeAllocated, IntPtr pdwSizeWritten);

        [PreserveSig]
        int GetVolume(out int plVolume);

        [PreserveSig]
        int GetPan(out int plPan);

        [PreserveSig]
        int GetFrequency(out uint pdwFrequency);

        [PreserveSig]
        int GetStatus(out DirectSoundBufferStatus pdwStatus);

        [PreserveSig]
        int Initialize(IntPtr pDirectSound, IntPtr pcDSBufferDesc);

        [PreserveSig]
        int Lock(int dwOffset, uint dwBytes,
                 out IntPtr ppvAudioPtr1, out int pdwAudioBytes1,
                 out IntPtr ppvAudioPtr2, out int pdwAudioBytes2,
                 DirectSoundBufferLockFlag dwFlags);

        [PreserveSig]
        int Play(uint dwReserved1, uint dwPriority, DirectSoundPlayFlags dwFlags);

        [PreserveSig]
        int SetCurrentPosition(uint dwNewPosition);

        [PreserveSig]
        int SetFormat(IntPtr pcfxFormat);

        [PreserveSig]
        int SetVolume(int lVolume);

        [PreserveSig]
        int SetPan(int lPan);

        [PreserveSig]
        int SetFrequency(uint dwFrequency);

        [PreserveSig]
        int Stop();

        [PreserveSig]
        int Unlock(IntPtr pvAudioPtr1, int dwAudioBytes1, IntPtr pvAudioPtr2, int dwAudioBytes2);

        [PreserveSig]
        int Restore();
    }
}
