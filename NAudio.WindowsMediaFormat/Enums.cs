using System;
using System.Runtime.InteropServices;

namespace NAudio.WindowsMediaFormat
{


    [Guid("96406BD9-2B2B-11d3-B36B-00C04F6108FF"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMMetadataEditor
    {
        uint Open([In, MarshalAs(UnmanagedType.LPWStr)] string pwszFilename);
        uint Close();
        uint Flush();

    }

    [Guid("15CC68E3-27CC-4ecd-B222-3F5D02D80BD5"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMHeaderInfo3
    {
        uint GetAttributeCount(
            [In]									ushort wStreamNum,
            [Out]									out ushort pcAttributes);

        uint GetAttributeByIndex(
            [In]									ushort wIndex,
            [Out, In]								ref ushort pwStreamNum,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszName,
            [Out, In]								ref ushort pcchNameLen,
            [Out]									out WMT_ATTR_DATATYPE pType,
            [Out, MarshalAs(UnmanagedType.LPArray)]	byte[] pValue,
            [Out, In]								ref ushort pcbLength);

        uint GetAttributeByName(
            [Out, In]								ref ushort pwStreamNum,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pszName,
            [Out]									out WMT_ATTR_DATATYPE pType,
            [Out, MarshalAs(UnmanagedType.LPArray)]	byte[] pValue,
            [Out, In]								ref ushort pcbLength);

        uint SetAttribute(
            [In]									ushort wStreamNum,
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pszName,
            [In]									WMT_ATTR_DATATYPE Type,
            [In, MarshalAs(UnmanagedType.LPArray)]	byte[] pValue,
            [In]									ushort cbLength);

        uint GetMarkerCount(
            [Out]									out ushort pcMarkers);

        uint GetMarker(
            [In]									ushort wIndex,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszMarkerName,
            [Out, In]								ref ushort pcchMarkerNameLen,
            [Out]									out ulong pcnsMarkerTime);

        uint AddMarker(
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pwszMarkerName,
            [In]									ulong cnsMarkerTime);

        uint RemoveMarker(
            [In]									ushort wIndex);

        uint GetScriptCount(
            [Out]									out ushort pcScripts);

        uint GetScript(
            [In]									ushort wIndex,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszType,
            [Out, In]								ref ushort pcchTypeLen,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszCommand,
            [Out, In]								ref ushort pcchCommandLen,
            [Out]									out ulong pcnsScriptTime);

        uint AddScript(
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pwszType,
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pwszCommand,
            [In]									ulong cnsScriptTime);

        uint RemoveScript(
            [In]									ushort wIndex);

        uint GetCodecInfoCount(
            [Out]									out uint pcCodecInfos);

        uint GetCodecInfo(
            [In]									uint wIndex,
            [Out, In]								ref ushort pcchName,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszName,
            [Out, In]								ref ushort pcchDescription,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszDescription,
            [Out]									out WMT_CODEC_INFO_TYPE pCodecType,
            [Out, In]								ref ushort pcbCodecInfo,
            [Out, MarshalAs(UnmanagedType.LPArray)]	byte[] pbCodecInfo);

        uint GetAttributeCountEx(
            [In]									ushort wStreamNum,
            [Out]									out ushort pcAttributes);

        uint GetAttributeIndices(
            [In]									ushort wStreamNum,
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pwszName,
            [In]									ref ushort pwLangIndex,
            [Out, MarshalAs(UnmanagedType.LPArray)] ushort[] pwIndices,
            [Out, In]								ref ushort pwCount);

        uint GetAttributeByIndexEx(
            [In]									ushort wStreamNum,
            [In]									ushort wIndex,
            [Out, MarshalAs(UnmanagedType.LPWStr)]	string pwszName,
            [Out, In]								ref ushort pwNameLen,
            [Out]									out WMT_ATTR_DATATYPE pType,
            [Out]									out ushort pwLangIndex,
            [Out, MarshalAs(UnmanagedType.LPArray)]	byte[] pValue,
            [Out, In]								ref uint pdwDataLength);

        uint ModifyAttribute(
            [In]									ushort wStreamNum,
            [In]									ushort wIndex,
            [In]									WMT_ATTR_DATATYPE Type,
            [In]									ushort wLangIndex,
            [In, MarshalAs(UnmanagedType.LPArray)]	byte[] pValue,
            [In]									uint dwLength);

        uint AddAttribute(
            [In]									ushort wStreamNum,
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pszName,
            [Out]									out ushort pwIndex,
            [In]									WMT_ATTR_DATATYPE Type,
            [In]									ushort wLangIndex,
            [In, MarshalAs(UnmanagedType.LPArray)]	byte[] pValue,
            [In]									uint dwLength);

        uint DeleteAttribute(
            [In]									ushort wStreamNum,
            [In]									ushort wIndex);

        uint AddCodecInfo(
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pszName,
            [In, MarshalAs(UnmanagedType.LPWStr)]	string pwszDescription,
            [In]									WMT_CODEC_INFO_TYPE codecType,
            [In]									ushort cbCodecInfo,
            [In, MarshalAs(UnmanagedType.LPArray)]	byte[] pbCodecInfo);
    }

    public enum WMT_ATTR_DATATYPE
    {
        WMT_TYPE_DWORD = 0,
        WMT_TYPE_STRING = 1,
        WMT_TYPE_BINARY = 2,
        WMT_TYPE_BOOL = 3,
        WMT_TYPE_QWORD = 4,
        WMT_TYPE_WORD = 5,
        WMT_TYPE_GUID = 6,
    }

    public enum WMT_CODEC_INFO_TYPE
    {
        WMT_CODECINFO_AUDIO = 0,
        WMT_CODECINFO_VIDEO = 1,
        WMT_CODECINFO_UNKNOWN = 0xffffff
    }

    public enum DRM_HTTP_STATUS
    {
        HTTP_NOTINITIATED = 0,
        HTTP_CONNECTING   = 1,
        HTTP_REQUESTING   = 2,
        HTTP_RECEIVING    = 3,
        HTTP_COMPLETED    = 4
    }

    public enum DRM_INDIVIDUALIZATION_STATUS
    {
        INDI_UNDEFINED  = 0x0000,
        INDI_BEGIN      = 0x0001,
        INDI_SUCCEED    = 0x0002,
        INDI_FAIL       = 0x0004,
        INDI_CANCEL     = 0x0008,
        INDI_DOWNLOAD   = 0x0010,
        INDI_INSTALL    = 0x0020
    }

    public enum DRM_LICENSE_STATE_CATEGORY{
        WM_DRM_LICENSE_STATE_NORIGHT  =0,
        WM_DRM_LICENSE_STATE_UNLIM  ,
        WM_DRM_LICENSE_STATE_COUNT  ,
        WM_DRM_LICENSE_STATE_FROM  ,
        WM_DRM_LICENSE_STATE_UNTIL  ,
        WM_DRM_LICENSE_STATE_FROM_UNTIL  ,
        WM_DRM_LICENSE_STATE_COUNT_FROM  ,
        WM_DRM_LICENSE_STATE_COUNT_UNTIL  ,
        WM_DRM_LICENSE_STATE_COUNT_FROM_UNTIL  ,
        WM_DRM_LICENSE_STATE_EXPIRATION_AFTER_FIRSTUSE // DRM_LICENSE_STATE_CATEGORY
    }

    public enum NETSOURCE_URLCREDPOLICY_SETTINGS
    {
        NETSOURCE_URLCREDPOLICY_SETTING_SILENTLOGONOK   =0,
        NETSOURCE_URLCREDPOLICY_SETTING_MUSTPROMPTUSER  =1,
        NETSOURCE_URLCREDPOLICY_SETTING_ANONYMOUSONLY   =2
    }

    public enum WM_AETYPE 
    {
        WM_AETYPE_INCLUDE  = 'i',
        WM_AETYPE_EXCLUDE  = 'e'
    }

    public enum WMT_ATTR_IMAGETYPE
    {
        WMT_IMAGETYPE_BITMAP  = 1,
        WMT_IMAGETYPE_JPEG    = 2,
        WMT_IMAGETYPE_GIF     = 3
    }

    [Flags]
    enum WMT_CREDENTIAL_FLAGS
    {
        WMT_CREDENTIAL_SAVE = 0x00000001,
        WMT_CREDENTIAL_DONT_CACHE = 0x00000002,
        WMT_CREDENTIAL_CLEAR_TEXT = 0x00000004,
        WMT_CREDENTIAL_PROXY = 0x00000008,
        WMT_CREDENTIAL_ENCRYPT = 0x00000010
    }

    enum WMT_DRMLA_TRUST
    {
        WMT_DRMLA_UNTRUSTED = 0,
        WMT_DRMLA_TRUSTED,
        WMT_DRMLA_TAMPERED
    }

    enum tagWMT_FILESINK_MODE
    {
        WMT_FM_SINGLE_BUFFERS = 1,
        WMT_FM_FILESINK_DATA_UNITS = 2,
        WMT_FM_FILESINK_UNBUFFERED = 4
    }

    enum WMT_IMAGE_TYPE
    {
        WMT_IT_NONE = 0,
        WMT_IT_BITMAP = 1,
        WMT_IT_JPEG = 2,
        WMT_IT_GIF = 3
    }

    enum WMT_INDEX_TYPE
    {
        WMT_IT_NEAREST_DATA_UNIT = 1,
        WMT_IT_NEAREST_OBJECT = 2,
        WMT_IT_NEAREST_CLEAN_POINT = 3
    }

    enum WMT_INDEXER_TYPE
    {
        WMT_IT_PRESENTATION_TIME = 0,
        WMT_IT_FRAME_NUMBERS = 1,
        WMT_IT_TIMECODE = 2
    }

    enum WMT_NET_PROTOCOL
    {
        WMT_PROTOCOL_HTTP = 0,
    }

    enum WMT_MUSICSPEECH_CLASS_MODE
    {
        WMT_MS_CLASS_MUSIC = 0,
        WMT_MS_CLASS_SPEECH = 1,
        WMT_MS_CLASS_MIXED = 2
    }

    enum WMT_OFFSET_FORMAT
    {
        WMT_OFFSET_FORMAT_100NS = 0,
        WMT_OFFSET_FORMAT_FRAME_NUMBERS = 1,
        WMT_OFFSET_FORMAT_PLAYLIST_OFFSET = 2,
        WMT_OFFSET_FORMAT_TIMECODE = 3,
        WMT_OFFSET_FORMAT_100NS_APPROXIMATE = 4
    }

    enum WMT_PLAY_MODE
    {
        WMT_PLAY_MODE_AUTOSELECT = 0,
        WMT_PLAY_MODE_LOCAL = 1,
        WMT_PLAY_MODE_DOWNLOAD = 2,
        WMT_PLAY_MODE_STREAMING = 3
    }

    enum WMT_PROXY_SETTINGS
    {
        WMT_PROXY_SETTING_NONE = 0,
        WMT_PROXY_SETTING_MANUAL = 1,
        WMT_PROXY_SETTING_AUTO = 2,
        WMT_PROXY_SETTING_BROWSER = 3,
        WMT_PROXY_SETTING_MAX = 4
    }

    [Flags]
    public enum WMT_RIGHTS
    {
        None = 0,
        WMT_RIGHT_PLAYBACK = 0x00000001,
        WMT_RIGHT_COPY_TO_NON_SDMI_DEVICE = 0x00000002,
        WMT_RIGHT_COPY_TO_CD = 0x00000008,
        WMT_RIGHT_COPY_TO_SDMI_DEVICE = 0x00000010,
        WMT_RIGHT_ONE_TIME = 0x00000020,
        WMT_RIGHT_SAVE_STREAM_PROTECTED = 0x00000040,
        WMT_RIGHT_COPY = 0x00000080,
        WMT_RIGHT_COLLABORATIVE_PLAY = 0x00000100,
        WMT_RIGHT_SDMI_TRIGGER = 0x00010000,
        WMT_RIGHT_SDMI_NOMORECOPIES = 0x00020000
    }

    public enum WMT_STATUS
    {
        WMT_ERROR = 0,
        WMT_OPENED = 1,
        WMT_BUFFERING_START = 2,
        WMT_BUFFERING_STOP = 3,
        WMT_EOF = 4,
        WMT_END_OF_FILE = 4,
        WMT_END_OF_SEGMENT = 5,
        WMT_END_OF_STREAMING = 6,
        WMT_LOCATING = 7,
        WMT_CONNECTING = 8,
        WMT_NO_RIGHTS = 9,
        WMT_MISSING_CODEC = 10,
        WMT_STARTED = 11,
        WMT_STOPPED = 12,
        WMT_CLOSED = 13,
        WMT_STRIDING = 14,
        WMT_TIMER = 15,
        WMT_INDEX_PROGRESS = 16,
        WMT_SAVEAS_START = 17,
        WMT_SAVEAS_STOP = 18,
        WMT_NEW_SOURCEFLAGS = 19,
        WMT_NEW_METADATA = 20,
        WMT_BACKUPRESTORE_BEGIN = 21,
        WMT_SOURCE_SWITCH = 22,
        WMT_ACQUIRE_LICENSE = 23,
        WMT_INDIVIDUALIZE = 24,
        WMT_NEEDS_INDIVIDUALIZATION = 25,
        WMT_NO_RIGHTS_EX = 26,
        WMT_BACKUPRESTORE_END = 27,
        WMT_BACKUPRESTORE_CONNECTING = 28,
        WMT_BACKUPRESTORE_DISCONNECTING = 29,
        WMT_ERROR_WITHURL = 30,
        WMT_RESTRICTED_LICENSE = 31,
        WMT_CLIENT_CONNECT = 32,
        WMT_CLIENT_DISCONNECT = 33,
        WMT_NATIVE_OUTPUT_PROPS_CHANGED = 34,
        WMT_RECONNECT_START = 35,
        WMT_RECONNECT_END = 36,
        WMT_CLIENT_CONNECT_EX = 37,
        WMT_CLIENT_DISCONNECT_EX = 38,
        WMT_SET_FEC_SPAN = 39,
        WMT_PREROLL_READY = 40,
        WMT_PREROLL_COMPLETE = 41,
        WMT_CLIENT_PROPERTIES = 42,
        WMT_LICENSEURL_SIGNATURE_STATE = 43,
        WMT_INIT_PLAYLIST_BURN = 44,
        WMT_TRANSCRYPTOR_INIT = 45,
        WMT_TRANSCRYPTOR_SEEKED = 46,
        WMT_TRANSCRYPTOR_READ = 47,
        WMT_TRANSCRYPTOR_CLOSED = 48,
        WMT_PROXIMITY_RESULT = 49,
        WMT_PROXIMITY_COMPLETED = 50,
        WMT_CONTENT_ENABLER = 51
    }

    enum WMT_STORAGE_FORMAT
    {
        WMT_Storage_Format_MP3 = 0,
        WMT_Storage_Format_V1 = 1,
    }

    public enum WMT_STREAM_SELECTION
    {
        WMT_OFF = 0,
        WMT_CLEANPOINT_ONLY = 1,
        WMT_ON = 2
    }

    enum WMT_TRANSPORT_TYPE
    {
        WMT_Transport_Type_Unreliable = 0,
        WMT_Transport_Type_Reliable = 1
    }

    public enum WMT_VERSION
    {
        WMT_VER_4_0 = 0x00040000,
        WMT_VER_7_0 = 0x00070000,
        WMT_VER_8_0 = 0x00080000,
        WMT_VER_9_0 = 0x00090000
    }

    enum WMT_WATERMARK_ENTRY_TYPE
    {
        WMT_WMETYPE_AUDIO = 1,
        WMT_WMETYPE_VIDEO = 2
    }

}
