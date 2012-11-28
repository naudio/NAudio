using System;
using System.Collections.Generic;
using System.Text;
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

        // TODO: find out what these are, and add them:
        // {00002000-0000-0010-8000-00aa00389b71} - MEDIASUBTYPE_AC3_AUDIO_OTHER
        // {a7fb87af-2d02-42fb-a4d4-05cd93843bdd} - MEDIASUBTYPE_DOLBY_DDPLUS
        // {0000000a-0cea-0010-8000-00aa00389b71} - KSDATAFORMAT_SUBTYPE_IEC61937_DOLBY_DIGITAL_PLUS
        // {00000160-0000-0010-8000-00aa00389b71} - MEDIASUBTYPE_MSAUDIO1
        // {000000ff-0000-0010-8000-00aa00389b71} - ?
        // {00000031-0000-0010-8000-00aa00389b71}
        // {0000000b-0000-0010-8000-00aa00389b71}
        // {00000007-0000-0010-8000-00aa00389b71}
        // {00000011-0000-0010-8000-00aa00389b71}
        // {00000002-0000-0010-8000-00aa00389b71}

    }
}
