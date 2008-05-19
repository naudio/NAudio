using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WASAPI.Interfaces
{
    /// <summary>
    /// n.b. WORK IN PROGRESS - this code will do nothing but crash at the moment
    /// </summary>
    [Guid("11112222-3333-4444-5555-0305E82C3301"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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
        /*HRESULT Initialize(
  AUDCLNT_SHAREMODE  ShareMode,
  DWORD  StreamFlags,
  REFERENCE_TIME  hnsBufferDuration,
  REFERENCE_TIME  hnsPeriodicity,
  const WAVEFORMATEX  *pFormat,
  LPCGUID  AudioSessionGuid
);*/

    /*    HRESULT IsFormatSupported(
  AUDCLNT_SHAREMODE  ShareMode,
  const WAVEFORMATEX  *pFormat,
  WAVEFORMATEX  **ppClosestMatch
);*/
        int Reset();
        int SetEventHandle(IntPtr eventHandle);
        int Start();
        int Stop();


    }
}
