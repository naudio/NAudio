using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclient/nn-audioclient-iaudioclient2
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("726778CD-F60A-4eda-82DE-E47610CD78AA")]
    public interface IAudioClient2 : IAudioClient
    {
        //virtual HRESULT STDMETHODCALLTYPE IsOffloadCapable(/*[in]*/ _In_  
        //   AUDIO_STREAM_CATEGORY Category, /*[in]*/ _Out_  BOOL *pbOffloadCapable) = 0;
        /// <summary>
        /// The IsOffloadCapable method retrieves information about whether or not the endpoint on which a stream is created is capable of supporting an offloaded audio stream.
        /// </summary>
        /// <param name="category">An enumeration that specifies the category of an audio stream.</param>
        /// <param name="pbOffloadCapable">A pointer to a Boolean value. TRUE indicates that the endpoint is offload-capable. FALSE indicates that the endpoint is not offload-capable.</param>
        void IsOffloadCapable(AudioStreamCategory category, out bool pbOffloadCapable);
        //virtual HRESULT STDMETHODCALLTYPE SetClientProperties(/*[in]*/ _In_  
        //  const AudioClientProperties *pProperties) = 0;
        /// <summary>
        /// Pointer to an AudioClientProperties structure.
        /// </summary>
        /// <param name="pProperties"></param>
        void SetClientProperties([In] IntPtr pProperties);
        // TODO: try this: void SetClientProperties([In, MarshalAs(UnmanagedType.LPStruct)] AudioClientProperties pProperties);
        //virtual HRESULT STDMETHODCALLTYPE GetBufferSizeLimits(/*[in]*/ _In_  
        //   const WAVEFORMATEX *pFormat, /*[in]*/ _In_  BOOL bEventDriven, /*[in]*/ 
        //  _Out_  REFERENCE_TIME *phnsMinBufferDuration, /*[in]*/ _Out_  
        //  REFERENCE_TIME *phnsMaxBufferDuration) = 0;
        /// <summary>
        /// The GetBufferSizeLimits method returns the buffer size limits of the hardware audio engine in 100-nanosecond units.
        /// </summary>
        /// <param name="pFormat">A pointer to the target format that is being queried for the buffer size limit.</param>
        /// <param name="bEventDriven">Boolean value to indicate whether or not the stream can be event-driven.</param>
        /// <param name="phnsMinBufferDuration">Returns a pointer to the minimum buffer size (in 100-nanosecond units) that is required for the underlying hardware audio engine to operate at the format specified in the pFormat parameter, without frequent audio glitching.</param>
        /// <param name="phnsMaxBufferDuration">Returns a pointer to the maximum buffer size (in 100-nanosecond units) that the underlying hardware audio engine can support for the format specified in the pFormat parameter.</param>
        void GetBufferSizeLimits(IntPtr pFormat, bool bEventDriven,
                                 out long phnsMinBufferDuration, out long phnsMaxBufferDuration);
    }
}
