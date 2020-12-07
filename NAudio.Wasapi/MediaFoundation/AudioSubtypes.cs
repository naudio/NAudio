using System;
using NAudio.Utils;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Audio Subtype GUIDs
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa372553%28v=vs.85%29.aspx
    /// </summary>
    public static class AudioSubtypes
    {
        /// <summary>
        /// Advanced Audio Coding (AAC).
        /// </summary>
        [FieldDescription("AAC")]
        public static readonly Guid MFAudioFormat_AAC = new Guid("00001610-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Not used
        /// </summary>
        [FieldDescription("ADTS")]
        public static readonly Guid MFAudioFormat_ADTS = new Guid("00001600-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Dolby AC-3 audio over Sony/Philips Digital Interface (S/PDIF).
        /// </summary>
        [FieldDescription("Dolby AC3 SPDIF")]
        public static readonly Guid MFAudioFormat_Dolby_AC3_SPDIF = new Guid("00000092-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Encrypted audio data used with secure audio path.
        /// </summary>
        [FieldDescription("DRM")]
        public static readonly Guid MFAudioFormat_DRM = new Guid("00000009-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Digital Theater Systems (DTS) audio.
        /// </summary>
        [FieldDescription("DTS")]
        public static readonly Guid MFAudioFormat_DTS = new Guid("00000008-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Uncompressed IEEE floating-point audio.
        /// </summary>
        [FieldDescription("IEEE floating-point")]
        public static readonly Guid MFAudioFormat_Float = new Guid("00000003-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// MPEG Audio Layer-3 (MP3).
        /// </summary>
        [FieldDescription("MP3")]
        public static readonly Guid MFAudioFormat_MP3 = new Guid("00000055-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// MPEG-1 audio payload.
        /// </summary>
        [FieldDescription("MPEG")]
        public static readonly Guid MFAudioFormat_MPEG = new Guid("00000050-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Windows Media Audio 9 Voice codec.
        /// </summary>
        [FieldDescription("WMA 9 Voice codec")]
        public static readonly Guid MFAudioFormat_MSP1 = new Guid("0000000a-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Uncompressed PCM audio.
        /// </summary>
        [FieldDescription("PCM")]
        public static readonly Guid MFAudioFormat_PCM = new Guid("00000001-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Windows Media Audio 9 Professional codec over S/PDIF.
        /// </summary>
        [FieldDescription("WMA SPDIF")]
        public static readonly Guid MFAudioFormat_WMASPDIF = new Guid("00000164-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Windows Media Audio 9 Lossless codec or Windows Media Audio 9.1 codec.
        /// </summary>
        [FieldDescription("WMAudio Lossless")]
        public static readonly Guid MFAudioFormat_WMAudio_Lossless = new Guid("00000163-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Windows Media Audio 8 codec, Windows Media Audio 9 codec, or Windows Media Audio 9.1 codec.
        /// </summary>
        [FieldDescription("Windows Media Audio")]
        public static readonly Guid MFAudioFormat_WMAudioV8 = new Guid("00000161-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Windows Media Audio 9 Professional codec or Windows Media Audio 9.1 Professional codec.
        /// </summary>
        [FieldDescription("Windows Media Audio Professional")]
        public static readonly Guid MFAudioFormat_WMAudioV9 = new Guid("00000162-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Dolby Digital (AC-3).
        /// </summary>
        [FieldDescription("Dolby AC3")]
        public static readonly Guid MFAudioFormat_Dolby_AC3 = new Guid("e06d802c-db46-11cf-b4d1-00805f6cbbea");

        /// <summary>
        /// MPEG-4 and AAC Audio Types
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd317599(v=vs.85).aspx
        /// Reference : wmcodecdsp.h
        /// </summary>
        [FieldDescription("MPEG-4 and AAC Audio Types")]
        public static readonly Guid MEDIASUBTYPE_RAW_AAC1 = new Guid("000000ff-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// Dolby Audio Types
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd317599(v=vs.85).aspx
        /// Reference : wmcodecdsp.h
        /// </summary>
        [FieldDescription("Dolby Audio Types")]
        public static readonly Guid MEDIASUBTYPE_DVM = new Guid("00002000-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// Dolby Audio Types
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd317599(v=vs.85).aspx
        /// Reference : wmcodecdsp.h
        /// </summary>
        [FieldDescription("Dolby Audio Types")]
        public static readonly Guid MEDIASUBTYPE_DOLBY_DDPLUS = new Guid("a7fb87af-2d02-42fb-a4d4-05cd93843bdd");

        /// <summary>
        /// μ-law coding
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd390971(v=vs.85).aspx
        /// Reference : Ksmedia.h
        /// </summary>
        [FieldDescription("μ-law")]
        public static readonly Guid KSDATAFORMAT_SUBTYPE_MULAW = new Guid("00000007-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// Adaptive delta pulse code modulation (ADPCM)
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd390971(v=vs.85).aspx
        /// Reference : Ksmedia.h
        /// </summary>
        [FieldDescription("ADPCM")]
        public static readonly Guid KSDATAFORMAT_SUBTYPE_ADPCM = new Guid("00000002-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// Dolby Digital Plus formatted for HDMI output.
        /// http://msdn.microsoft.com/en-us/library/windows/hardware/ff538392(v=vs.85).aspx
        /// Reference : internet
        /// </summary>
        [FieldDescription("Dolby Digital Plus for HDMI")]
        public static readonly Guid KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS = new Guid("0000000a-0cea-0010-8000-00aa00389b71");

        /// <summary>
        /// MSAudio1 - unknown meaning
        /// Reference : wmcodecdsp.h
        /// </summary>
        [FieldDescription("MSAudio1")]
        public static readonly Guid MEDIASUBTYPE_MSAUDIO1 = new Guid("00000160-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// IMA ADPCM ACM Wrapper
        /// </summary>
        [FieldDescription("IMA ADPCM")]
        public static readonly Guid ImaAdpcm = new Guid("00000011-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// WMSP2 - unknown meaning
        /// Reference: wmsdkidl.h
        /// </summary>
        [FieldDescription("WMSP2")]
        public static readonly Guid WMMEDIASUBTYPE_WMSP2 = new Guid("0000000b-0000-0010-8000-00aa00389b71");


        // TODO: find out what these are, and add them:
        // {00000031-0000-0010-8000-00aa00389b71} // probably GSM610 ACM wrapper

    }
}
