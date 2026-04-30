using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Wave.DirectSoundInterop
{
    /// <summary>
    /// IDirectSound — root DirectSound device object. Created via <c>DirectSoundCreate</c>.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IDirectSound (dsound.h).
    /// Only <see cref="CreateSoundBuffer"/> and <see cref="SetCooperativeLevel"/> are invoked
    /// from <see cref="DirectSoundOut"/>; the remaining slots are declared with
    /// <see cref="IntPtr"/> placeholders to preserve vtable order without forcing
    /// marshalling decisions for unused methods.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("279AFA83-4981-11CE-A521-0020AF0BE560")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDirectSound
    {
        [PreserveSig]
        int CreateSoundBuffer(in BufferDescription pcDSBufferDesc, out IntPtr ppDSBuffer, IntPtr pUnkOuter);

        [PreserveSig]
        int GetCaps(IntPtr pDSCaps);

        [PreserveSig]
        int DuplicateSoundBuffer(IntPtr pDSBufferOriginal, IntPtr ppDSBufferDuplicate);

        [PreserveSig]
        int SetCooperativeLevel(IntPtr hwnd, DirectSoundCooperativeLevel dwLevel);

        [PreserveSig]
        int Compact();

        [PreserveSig]
        int GetSpeakerConfig(IntPtr pdwSpeakerConfig);

        [PreserveSig]
        int SetSpeakerConfig(uint dwSpeakerConfig);

        [PreserveSig]
        int Initialize(IntPtr pcGuidDevice);
    }
}
