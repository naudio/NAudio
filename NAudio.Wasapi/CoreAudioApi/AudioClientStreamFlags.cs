using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AUDCLNT_STREAMFLAGS
    /// https://docs.microsoft.com/en-us/windows/win32/coreaudio/audclnt-streamflags-xxx-constants
    /// </summary>
    [Flags]
    public enum AudioClientStreamFlags : uint
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_CROSSPROCESS
        /// The audio stream will be a member of a cross-process audio session.
        /// </summary>
        CrossProcess = 0x00010000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_LOOPBACK
        /// The audio stream will operate in loopback mode
        /// </summary>
        Loopback = 0x00020000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_EVENTCALLBACK 
        /// Processing of the audio buffer by the client will be event driven
        /// </summary>
        EventCallback = 0x00040000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_NOPERSIST   
        /// The volume and mute settings for an audio session will not persist across application restarts
        /// </summary>
        NoPersist = 0x00080000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_RATEADJUST
        /// The sample rate of the stream is adjusted to a rate specified by an application.
        /// </summary>
        RateAdjust = 0x00100000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY
        /// When used with AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM, a sample rate converter with better quality 
        /// than the default conversion but with a higher performance cost is used. This should be used if 
        /// the audio is ultimately intended to be heard by humans as opposed to other scenarios such as 
        /// pumping silence or populating a meter.
        /// </summary>
        SrcDefaultQuality = 0x08000000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM
        /// A channel matrixer and a sample rate converter are inserted as necessary to convert between the uncompressed format supplied to IAudioClient::Initialize and the audio engine mix format.
        /// </summary>
        AutoConvertPcm = 0x80000000,
           
    }
}
