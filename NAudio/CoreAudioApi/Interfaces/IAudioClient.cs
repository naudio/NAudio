using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// n.b. WORK IN PROGRESS - this code will do nothing but crash at the moment
    /// </summary>
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), 
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioClient
    {
        int GetBufferSize(out int bufferSize);
        int GetCurrentPadding(out int currentPadding);
        // REFERENCE_TIME is 64 bit int
        int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);
        // the address of a pointer which will be set to point to a WAVEFORMATEX structure
        int GetMixFormat(IntPtr deviceFormatPointer);

        void GetService(Guid interfaceId, IntPtr interfacePointer);
        int GetStreamLatency(out long streamLatency);
        int Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags StreamFlags,
            long hnsBufferDuration, // REFERENCE_TIME
            long hnsPeriodicity, // REFERENCE_TIME
            [In] WaveFormat pFormat,
            [In] Guid AudioSessionGuid);

        int IsFormatSupported(
            AudioClientShareMode shareMode,
            [In] WaveFormat pFormat,
            out WaveFormat closestMatch);

        int Reset();
        int SetEventHandle(IntPtr eventHandle);
        int Start();
        int Stop();


    }

    /// <summary>
    /// AUDCLNT_SHAREMODE
    /// </summary>
    enum AudioClientShareMode
    {
        AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_SHAREMODE_EXCLUSIVE
    }

    [Flags]
    enum AudioClientStreamFlags
    {
        AUDCLNT_STREAMFLAGS_CROSSPROCESS  = 0x00010000,
        AUDCLNT_STREAMFLAGS_LOOPBACK      = 0x00020000,
        AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000,
        AUDCLNT_STREAMFLAGS_NOPERSIST     = 0x00080000,
    }
}
