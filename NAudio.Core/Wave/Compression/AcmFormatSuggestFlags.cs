using System;

namespace NAudio.Wave.Compression
{
    [Flags]
    enum AcmFormatSuggestFlags
    {
        /// <summary>
        /// ACM_FORMATSUGGESTF_WFORMATTAG
        /// </summary>
        FormatTag = 0x00010000,
        /// <summary>
        /// ACM_FORMATSUGGESTF_NCHANNELS
        /// </summary>
        Channels = 0x00020000,
        /// <summary>
        /// ACM_FORMATSUGGESTF_NSAMPLESPERSEC
        /// </summary>
        SamplesPerSecond = 0x00040000,
        /// <summary>
        /// ACM_FORMATSUGGESTF_WBITSPERSAMPLE
        /// </summary>
        BitsPerSample = 0x00080000,
        /// <summary>
        /// ACM_FORMATSUGGESTF_TYPEMASK
        /// </summary>
        TypeMask = 0x00FF0000,
    }
}
