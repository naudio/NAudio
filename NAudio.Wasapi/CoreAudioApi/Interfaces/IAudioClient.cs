using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IAudioClient interface
    /// Defined in AudioClient.h
    /// </summary>
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), 
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    public interface IAudioClient
    {
        /// <summary>
        /// initializes the audio stream.
        /// </summary>
        /// <param name="shareMode">The sharing mode for the connection. Through this parameter, the client tells the audio engine whether it wants to share the audio endpoint device with other clients.</param>
        /// <param name="streamFlags">Flags to control creation of the stream. The client should set this parameter to 0 or to the bitwise OR of one or more of the AUDCLNT_STREAMFLAGS_XXX Constants or the AUDCLNT_SESSIONFLAGS_XXX Constants.</param>
        /// <param name="hnsBufferDuration">The buffer capacity as a time value. This parameter is of type REFERENCE_TIME and is expressed in 100-nanosecond units. This parameter contains the buffer size that the caller requests for the buffer that the audio application will share with the audio engine (in shared mode) or with the endpoint device (in exclusive mode). If the call succeeds, the method allocates a buffer that is a least this large.</param>
        /// <param name="hnsPeriodicity">The device period. This parameter can be nonzero only in exclusive mode. In shared mode, always set this parameter to 0. In exclusive mode, this parameter specifies the requested scheduling period for successive buffer accesses by the audio endpoint device. If the requested device period lies outside the range that is set by the device's minimum period and the system's maximum period, then the method clamps the period to that range. If this parameter is 0, the method sets the device period to its default value. To obtain the default device period, call the IAudioClient::GetDevicePeriod method. If the AUDCLNT_STREAMFLAGS_EVENTCALLBACK stream flag is set and AUDCLNT_SHAREMODE_EXCLUSIVE is set as the ShareMode, then hnsPeriodicity must be nonzero and equal to hnsBufferDuration.</param>
        /// <param name="pFormat">Pointer to a format descriptor. This parameter must point to a valid format descriptor of type WAVEFORMATEX (or WAVEFORMATEXTENSIBLE).</param>
        /// <param name="audioSessionGuid">Pointer to a session GUID. This parameter points to a GUID value that identifies the audio session that the stream belongs to. If the GUID identifies a session that has been previously opened, the method adds the stream to that session. If the GUID does not identify an existing session, the method opens a new session and adds the stream to that session. The stream remains a member of the same session for its lifetime. Setting this parameter to NULL is equivalent to passing a pointer to a GUID_NULL value.</param>
        /// <returns></returns>
        [PreserveSig]
        int Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long hnsBufferDuration, // REFERENCE_TIME
            long hnsPeriodicity, // REFERENCE_TIME
            [In] WaveFormat pFormat,
            [In] ref Guid audioSessionGuid);

        /// <summary>
        /// The GetBufferSize method retrieves the size (maximum capacity) of the endpoint buffer.
        /// </summary>
        int GetBufferSize(out uint bufferSize);

        /// <summary>
        /// retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
        /// </summary>
        [return: MarshalAs(UnmanagedType.I8)]
        long GetStreamLatency();

        /// <summary>
        /// retrieves the number of frames of padding in the endpoint buffer.
        /// </summary>
        int GetCurrentPadding(out int currentPadding);

        /// <summary>
        /// Indicates whether the audio endpoint device supports a particular stream format.
        /// </summary>
        [PreserveSig]
        int IsFormatSupported(
            AudioClientShareMode shareMode,
            [In] WaveFormat pFormat,
            IntPtr closestMatchFormat); // or outIntPtr??

        /// <summary>
        /// retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
        /// </summary>
        int GetMixFormat(out IntPtr deviceFormatPointer);

        // REFERENCE_TIME is 64 bit int
        /// <summary>
        /// retrieves the length of the periodic interval separating successive processing passes by the audio engine on the data in the endpoint buffer.
        /// </summary>
        int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

        /// <summary>
        /// starts the audio stream.
        /// </summary>
        int Start();

        /// <summary>
        /// stops the audio stream.
        /// </summary>
        int Stop();

        /// <summary>
        /// resets the audio stream.
        /// </summary>
        int Reset();

        /// <summary>
        /// sets the event handle that the system signals when an audio buffer is ready to be processed by the client.
        /// </summary>
        int SetEventHandle(IntPtr eventHandle);

        /// <summary>
        /// The GetService method accesses additional services from the audio client object.
        /// </summary>
        /// <param name="interfaceId">The interface ID for the requested service.</param>
        /// <param name="interfacePointer">Pointer to a pointer variable into which the method writes the address of an instance of the requested interface. </param>
        [PreserveSig]
        int GetService([In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId, [Out, MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
    }
}
