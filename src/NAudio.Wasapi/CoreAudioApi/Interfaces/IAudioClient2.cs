using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IAudioClient2 interface
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclient/nn-audioclient-iaudioclient2
    /// </summary>
    [GeneratedComInterface]
    [Guid("726778CD-F60A-4eda-82DE-E47610CD78AA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IAudioClient2
    {
        // ---- IAudioClient methods (must redeclare in vtable order) ----

        [PreserveSig]
        int Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long hnsBufferDuration,
            long hnsPeriodicity,
            IntPtr pFormat,
            in Guid audioSessionGuid);

        [PreserveSig]
        int GetBufferSize(out uint bufferSize);

        [PreserveSig]
        int GetStreamLatency(out long latency);

        [PreserveSig]
        int GetCurrentPadding(out int currentPadding);

        [PreserveSig]
        int IsFormatSupported(
            AudioClientShareMode shareMode,
            IntPtr pFormat,
            out IntPtr closestMatchFormat);

        [PreserveSig]
        int GetMixFormat(out IntPtr deviceFormatPointer);

        [PreserveSig]
        int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

        [PreserveSig]
        int Start();

        [PreserveSig]
        int Stop();

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int SetEventHandle(IntPtr eventHandle);

        [PreserveSig]
        int GetService(in Guid interfaceId, out IntPtr interfacePointer);

        // ---- IAudioClient2-specific methods ----

        [PreserveSig]
        int IsOffloadCapable(AudioStreamCategory category, out int pbOffloadCapable);

        [PreserveSig]
        int SetClientProperties(IntPtr pProperties);

        [PreserveSig]
        int GetBufferSizeLimits(IntPtr pFormat, [MarshalAs(UnmanagedType.Bool)] bool bEventDriven,
            out long phnsMinBufferDuration, out long phnsMaxBufferDuration);
    }
}
