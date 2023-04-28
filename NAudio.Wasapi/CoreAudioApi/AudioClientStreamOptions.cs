using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Defines values that describe the characteristics of an audio stream.
    /// AUDCLNT_STREAMOPTIONS 
    /// https://docs.microsoft.com/en-us/windows/win32/api/audioclient/ne-audioclient-audclnt_streamoptions
    /// </summary>
    [Flags]
    public enum AudioClientStreamOptions
    {
        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_NONE
        /// No stream options.
        /// </summary>
        None = 0,
        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_RAW
        /// The audio stream is a 'raw' stream that bypasses all signal processing except for endpoint specific, always-on processing in the APO, driver, and hardware.
        /// </summary>
        Raw = 0x1,
        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_MATCH_FORMAT
        /// The audio client is requesting that the audio engine match the format proposed by the client. The audio engine
        /// will match this format only if the format is supported by the audio driver and associated APOs.
        /// </summary>
        MatchFormat = 0x2,
        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_AMBISONICS
        /// </summary>
        Ambisonics = 0x4,
    }
}