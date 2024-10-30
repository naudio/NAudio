using System;
// ReSharper disable InconsistentNaming

namespace NAudio.Dmo
{
    /// <summary>
    /// Audio Media Subtypes
    /// </summary>
    public class AudioMediaSubtypes
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd317599(v=vs.85).aspx
        
        /// <summary>
        /// PCM
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71"); // PCM audio. 
        /// <summary>
        /// PCM Audio obsolete
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_PCMAudioObsolete = new Guid("e436eb8a-524f-11ce-9f53-0020af0ba770"); // Obsolete. Do not use. 
        /// <summary>
        /// MPEG1 Packet
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_MPEG1Packet = new Guid("e436eb80-524f-11ce-9f53-0020af0ba770"); // MPEG1 Audio packet. 
        /// <summary>
        /// MPEG1 Payload
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_MPEG1Payload = new Guid("e436eb81-524f-11ce-9f53-0020af0ba770"); // MPEG1 Audio Payload. 
        /// <summary>
        /// MPEG2 Audio
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_MPEG2_AUDIO = new Guid("e06d802b-db46-11cf-b4d1-00805f6cbbea"); // MPEG-2 audio data  
        /// <summary>
        /// DVD audio data
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_DVD_LPCM_AUDIO = new Guid("e06d8032-db46-11cf-b4d1-00805f6cbbea"); // DVD audio data  
        /// <summary>
        /// DRM Audio
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_DRM_Audio = new Guid("00000009-0000-0010-8000-00aa00389b71"); // Corresponds to WAVE_FORMAT_DRM. 
        /// <summary>
        /// IEEE Float
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71"); // Corresponds to WAVE_FORMAT_IEEE_FLOAT 
        /// <summary>
        /// Dolby AC3
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_DOLBY_AC3 = new Guid("e06d802c-db46-11cf-b4d1-00805f6cbbea"); // Dolby data  
        /// <summary>
        /// Dolby AC3 SPDIF
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_DOLBY_AC3_SPDIF = new Guid("00000092-0000-0010-8000-00aa00389b71"); // Dolby AC3 over SPDIF.  
        /// <summary>
        /// RAW Sport
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RAW_SPORT = new Guid("00000240-0000-0010-8000-00aa00389b71"); // Equivalent to MEDIASUBTYPE_DOLBY_AC3_SPDIF. 
        /// <summary>
        /// SPDIF TAG 241h
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_SPDIF_TAG_241h = new Guid("00000241-0000-0010-8000-00aa00389b71"); // Equivalent to MEDIASUBTYPE_DOLBY_AC3_SPDIF. 


        // http://msdn.microsoft.com/en-us/library/dd757532%28VS.85%29.aspx
        /// <summary>
        /// I420
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_I420 = new Guid("30323449-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// IYUV
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_IYUV = new Guid("56555949-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// RGB1
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB1 = new Guid("e436eb78-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// RGB24
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB24 = new Guid("e436eb7d-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// RGB32
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB32 = new Guid("e436eb7e-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// RGB4
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB4 = new Guid("e436eb79-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// RGB555
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB555 = new Guid("e436eb7c-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// RGB565
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB565 = new Guid("e436eb7b-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// RGB8
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_RGB8 = new Guid("e436eb7a-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// UYVY
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_UYVY = new Guid("59565955-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// Video Image
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_VIDEOIMAGE = new Guid("1d4a45f2-e5f6-4b44-8388-f0ae5c0e0c37");
        /// <summary>
        /// YUY2
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_YUY2 = new Guid("32595559-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// YV12
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_YV12 = new Guid("31313259-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// YVU9
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_YVU9 = new Guid("39555659-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// YVYU
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_YVYU = new Guid("55595659-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// MPEG2 Video
        /// </summary>
        public static readonly Guid WMFORMAT_MPEG2Video = new Guid("e06d80e3-db46-11cf-b4d1-00805f6cbbea");
        /// <summary>
        /// SCcript
        /// </summary>
        public static readonly Guid WMFORMAT_Script = new Guid("5C8510F2-DEBE-4ca7-BBA5-F07A104F8DFF");
        /// <summary>
        /// Video Info
        /// </summary>
        public static readonly Guid WMFORMAT_VideoInfo = new Guid("05589f80-c356-11ce-bf01-00aa0055595a");
        /// <summary>
        /// WAVEFORMATEX
        /// </summary>
        public static readonly Guid WMFORMAT_WaveFormatEx = new Guid("05589f81-c356-11ce-bf01-00aa0055595a");
        /// <summary>
        /// Webstream
        /// </summary>
        public static readonly Guid WMFORMAT_WebStream = new Guid("da1e6b13-8359-4050-b398-388e965bf00c");
        /// <summary>
        /// ACELP net
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_ACELPnet = new Guid("00000130-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// Base
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_Base = new Guid("00000000-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// DRM
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_DRM = new Guid("00000009-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// MP3
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_MP3 = new Guid("00000055-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// MP43
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_MP43 = new Guid("3334504D-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// MP4S
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_MP4S = new Guid("5334504D-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// M4S2
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_M4S2 = new Guid("3253344D-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// P422
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_P422 = new Guid("32323450-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// MPEG2 Video
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_MPEG2_VIDEO = new Guid("e06d8026-db46-11cf-b4d1-00805f6cbbea");
        /// <summary>
        /// MSS1
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_MSS1 = new Guid("3153534D-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// MSS2
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_MSS2 = new Guid("3253534D-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// PCM
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WebStream
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WebStream = new Guid("776257d4-c627-41cb-8f81-7ac7ff1c40cc");
        /// <summary>
        /// WM Audio Lossless
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMAudio_Lossless = new Guid("00000163-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WM Audio V2
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMAudioV2 = new Guid("00000161-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WM Audio V7
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMAudioV7 = new Guid("00000161-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WM Audio V8
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMAudioV8 = new Guid("00000161-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WM Audio V9
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMAudioV9 = new Guid("00000162-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMSP1
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMSP1 = new Guid("0000000A-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMV1
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMV1 = new Guid("31564D57-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMV2
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMV2 = new Guid("32564D57-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMV3
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMV3 = new Guid("33564D57-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMVA
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMVA = new Guid("41564D57-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMVP
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WMVP = new Guid("50564D57-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// WMVP2
        /// </summary>
        public static readonly Guid WMMEDIASUBTYPE_WVP2 = new Guid("32505657-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// Audio
        /// </summary>
        public static readonly Guid WMMEDIATYPE_Audio = new Guid("73647561-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// File Transfer
        /// </summary>
        public static readonly Guid WMMEDIATYPE_FileTransfer = new Guid("D9E47579-930E-4427-ADFC-AD80F290E470");
        /// <summary>
        /// Image
        /// </summary>
        public static readonly Guid WMMEDIATYPE_Image = new Guid("34A50FD8-8AA5-4386-81FE-A0EFE0488E31");
        /// <summary>
        /// Script
        /// </summary>
        public static readonly Guid WMMEDIATYPE_Script = new Guid("73636d64-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// Text
        /// </summary>
        public static readonly Guid WMMEDIATYPE_Text = new Guid("9BBA1EA7-5AB2-4829-BA57-0940209BCF3E");
        /// <summary>
        /// Video
        /// </summary>
        public static readonly Guid WMMEDIATYPE_Video = new Guid("73646976-0000-0010-8000-00AA00389B71");
        /// <summary>
        /// Two strings
        /// </summary>
        public static readonly Guid WMSCRIPTTYPE_TwoStrings = new Guid("82f38a70-c29f-11d1-97ad-00a0c95ea850");


        // others?
        /// <summary>
        /// Wave
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_WAVE = new Guid("e436eb8b-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// AU
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_AU = new Guid("e436eb8c-524f-11ce-9f53-0020af0ba770");
        /// <summary>
        /// AIFF
        /// </summary>
        public static readonly Guid MEDIASUBTYPE_AIFF = new Guid("e436eb8d-524f-11ce-9f53-0020af0ba770");

        /// <summary>
        /// Audio Subtypes
        /// </summary>
        public static readonly Guid[] AudioSubTypes = {
            MEDIASUBTYPE_PCM,
            MEDIASUBTYPE_PCMAudioObsolete,
            MEDIASUBTYPE_MPEG1Packet,
            MEDIASUBTYPE_MPEG1Payload,
            MEDIASUBTYPE_MPEG2_AUDIO,
            MEDIASUBTYPE_DVD_LPCM_AUDIO,
            MEDIASUBTYPE_DRM_Audio,
            MEDIASUBTYPE_IEEE_FLOAT,
            MEDIASUBTYPE_DOLBY_AC3,
            MEDIASUBTYPE_DOLBY_AC3_SPDIF,
            MEDIASUBTYPE_RAW_SPORT,
            MEDIASUBTYPE_SPDIF_TAG_241h,
            WMMEDIASUBTYPE_MP3,
        };

        /// <summary>
        /// Audio subtype names
        /// </summary>
        public static readonly string[] AudioSubTypeNames = {
            "PCM",
            "PCM Obsolete",
            "MPEG1Packet",
            "MPEG1Payload",
            "MPEG2_AUDIO",
            "DVD_LPCM_AUDIO",
            "DRM_Audio",
            "IEEE_FLOAT",
            "DOLBY_AC3",
            "DOLBY_AC3_SPDIF",
            "RAW_SPORT",
            "SPDIF_TAG_241h",
            "MP3"
        };

        /// <summary>
        /// Get Audio Subtype Name
        /// </summary>
        public static string GetAudioSubtypeName(Guid subType)
        {
            for (int index = 0; index < AudioSubTypes.Length; index++)
            {
                if (subType == AudioSubTypes[index])
                {
                    return AudioSubTypeNames[index];
                }
            }
            return subType.ToString();
        }
    }
}
