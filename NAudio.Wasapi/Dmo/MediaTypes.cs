using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// uuids.h, ksuuids.h
    /// </summary>
    static class MediaTypes
    {
        public static readonly Guid MEDIATYPE_AnalogAudio = new Guid("0482DEE1-7817-11cf-8a03-00aa006ecb65"); // Analog audio. 
        public static readonly Guid MEDIATYPE_AnalogVideo = new Guid("0482DDE1-7817-11cf-8A03-00AA006ECB65"); // Analog video. 
        public static readonly Guid MEDIATYPE_Audio = new Guid("73647561-0000-0010-8000-00AA00389B71"); // Audio. 
        public static readonly Guid MEDIATYPE_AUXLine21Data = new Guid("670AEA80-3A82-11d0-B79B-00AA003767A7"); // Line 21 data. Used by closed captions. 
        public static readonly Guid MEDIATYPE_File = new Guid("656c6966-0000-0010-8000-00AA00389B71"); // File. (Obsolete) 
        public static readonly Guid MEDIATYPE_Interleaved = new Guid("73766169-0000-0010-8000-00AA00389B71"); // Interleaved audio and video. Used for Digital Video (DV). 
        public static readonly Guid MEDIATYPE_Midi = new Guid("7364696D-0000-0010-8000-00AA00389B71"); // MIDI format. 
        // public static readonly Guid MEDIATYPE_MPEG2_PES MPEG-2 PES packets. 
        // public static readonly Guid MEDIATYPE_MPEG2_SECTIONS MPEG-2 section data 
        public static readonly Guid MEDIATYPE_ScriptCommand = new Guid("73636d64-0000-0010-8000-00AA00389B71"); // Data is a script command, used by closed captions. 
        public static readonly Guid MEDIATYPE_Stream = new Guid("e436eb83-524f-11ce-9f53-0020af0ba770"); // Byte stream with no time stamps. 
        public static readonly Guid MEDIATYPE_Text = new Guid("73747874-0000-0010-8000-00AA00389B71"); // Text. 
        public static readonly Guid MEDIATYPE_Timecode = new Guid("0482DEE3-7817-11cf-8a03-00aa006ecb65"); // Timecode data. Note: DirectShow does not provide any filters that support this media type. 
        public static readonly Guid MEDIATYPE_Video = new Guid("73646976-0000-0010-8000-00AA00389B71"); // Video. 

        public static readonly Guid[] MajorTypes = new Guid[]
        {
            MEDIATYPE_AnalogAudio,
            MEDIATYPE_AnalogVideo,
            MEDIATYPE_Audio,
            MEDIATYPE_AUXLine21Data,
            MEDIATYPE_File,
            MEDIATYPE_Interleaved,
            MEDIATYPE_Midi,
            MEDIATYPE_ScriptCommand,
            MEDIATYPE_Stream,
            MEDIATYPE_Text,
            MEDIATYPE_Timecode,
            MEDIATYPE_Video,
        };

        public static readonly string[] MajorTypeNames = new string[]
        {
            "Analog Audio",
            "Analog Video",
            "Audio",
            "AUXLine21Data",
            "File",
            "Interleaved",
            "Midi",
            "ScriptCommand",
            "Stream",
            "Text",
            "Timecode",
            "Video",
        };


        public static string GetMediaTypeName(Guid majorType)
        {
            for (int index = 0; index < MajorTypes.Length; index++)
            {
                if (majorType == MajorTypes[index])
                {
                    return MajorTypeNames[index];
                }
            }
            throw new ArgumentException("Major Type not found");
        }
    }


}
