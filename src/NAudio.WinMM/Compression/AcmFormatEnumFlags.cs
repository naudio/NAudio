using System;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Format Enumeration Flags
    /// </summary>
    [Flags]
    public enum AcmFormatEnumFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// ACM_FORMATENUMF_CONVERT
        /// The WAVEFORMATEX structure pointed to by the pwfx member of the ACMFORMATDETAILS structure is valid. The enumerator will only enumerate destination formats that can be converted from the given pwfx format. 
        /// </summary>
        Convert = 0x00100000,
        /// <summary>
        /// ACM_FORMATENUMF_HARDWARE
        /// The enumerator should only enumerate formats that are supported as native input or output formats on one or more of the installed waveform-audio devices. This flag provides a way for an application to choose only formats native to an installed waveform-audio device. This flag must be used with one or both of the ACM_FORMATENUMF_INPUT and ACM_FORMATENUMF_OUTPUT flags. Specifying both ACM_FORMATENUMF_INPUT and ACM_FORMATENUMF_OUTPUT will enumerate only formats that can be opened for input or output. This is true regardless of whether this flag is specified. 
        /// </summary>
        Hardware = 0x00400000, 
        /// <summary>
        /// ACM_FORMATENUMF_INPUT
        /// Enumerator should enumerate only formats that are supported for input (recording). 
        /// </summary>
        Input = 0x00800000, 
        /// <summary>
        /// ACM_FORMATENUMF_NCHANNELS 
        /// The nChannels member of the WAVEFORMATEX structure pointed to by the pwfx member of the ACMFORMATDETAILS structure is valid. The enumerator will enumerate only a format that conforms to this attribute. 
        /// </summary>
        Channels = 0x00020000, 
        /// <summary>
        /// ACM_FORMATENUMF_NSAMPLESPERSEC
        /// The nSamplesPerSec member of the WAVEFORMATEX structure pointed to by the pwfx member of the ACMFORMATDETAILS structure is valid. The enumerator will enumerate only a format that conforms to this attribute. 
        /// </summary>
        SamplesPerSecond = 0x00040000, 
        /// <summary>
        /// ACM_FORMATENUMF_OUTPUT 
        /// Enumerator should enumerate only formats that are supported for output (playback). 
        /// </summary>
        Output = 0x01000000, 
        /// <summary>
        /// ACM_FORMATENUMF_SUGGEST
        /// The WAVEFORMATEX structure pointed to by the pwfx member of the ACMFORMATDETAILS structure is valid. The enumerator will enumerate all suggested destination formats for the given pwfx format. This mechanism can be used instead of the acmFormatSuggest function to allow an application to choose the best suggested format for conversion. The dwFormatIndex member will always be set to zero on return. 
        /// </summary>
        Suggest = 0x00200000,
        /// <summary>
        /// ACM_FORMATENUMF_WBITSPERSAMPLE
        /// The wBitsPerSample member of the WAVEFORMATEX structure pointed to by the pwfx member of the ACMFORMATDETAILS structure is valid. The enumerator will enumerate only a format that conforms to this attribute. 
        /// </summary>
        BitsPerSample = 0x00080000,
        /// <summary>
        /// ACM_FORMATENUMF_WFORMATTAG
        /// The wFormatTag member of the WAVEFORMATEX structure pointed to by the pwfx member of the ACMFORMATDETAILS structure is valid. The enumerator will enumerate only a format that conforms to this attribute. The dwFormatTag member of the ACMFORMATDETAILS structure must be equal to the wFormatTag member. 
        /// </summary>
        FormatTag = 0x00010000,
    }
}
