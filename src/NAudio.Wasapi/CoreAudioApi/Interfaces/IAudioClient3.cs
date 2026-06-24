using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IAudioClient3 interface
    /// Provides low-latency shared mode audio streaming.
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclient/nn-audioclient-iaudioclient3
    /// </summary>
    [GeneratedComInterface]
    [Guid("7ED4EE07-8E67-4CD4-8C1A-2B7A5987AD42")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IAudioClient3
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

        // ---- IAudioClient2 methods ----

        [PreserveSig]
        int IsOffloadCapable(AudioStreamCategory category, out int pbOffloadCapable);

        [PreserveSig]
        int SetClientProperties(IntPtr pProperties);

        [PreserveSig]
        int GetBufferSizeLimits(IntPtr pFormat, [MarshalAs(UnmanagedType.Bool)] bool bEventDriven,
            out long phnsMinBufferDuration, out long phnsMaxBufferDuration);

        // ---- IAudioClient3-specific methods ----

        /// <summary>
        /// Returns the range of periodicities supported by the engine for the specified stream format.
        /// </summary>
        [PreserveSig]
        int GetSharedModeEnginePeriod(
            IntPtr pFormat,
            out uint pDefaultPeriodInFrames,
            out uint pFundamentalPeriodInFrames,
            out uint pMinPeriodInFrames,
            out uint pMaxPeriodInFrames);

        /// <summary>
        /// Initializes a shared audio stream with the specified periodicity.
        /// </summary>
        [PreserveSig]
        int InitializeSharedAudioStream(
            AudioClientStreamFlags streamFlags,
            uint periodInFrames,
            IntPtr pFormat,
            in Guid audioSessionGuid);

        /// <summary>
        /// Returns the current period of the audio engine.
        /// </summary>
        [PreserveSig]
        int GetCurrentSharedModeEnginePeriod(
            out IntPtr ppFormat,
            out uint pCurrentPeriodInFrames);
    }
}
