using System;
using System.Runtime.InteropServices;

// additional enums, not sure if they are useful
namespace NAudio.WindowsMediaFormat
{

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

    enum WMT_MUSICSPEECH_CLASS_MODE
    {
        WMT_MS_CLASS_MUSIC = 0,
        WMT_MS_CLASS_SPEECH = 1,
        WMT_MS_CLASS_MIXED = 2
    }

    enum WMT_PROXY_SETTINGS
    {
        WMT_PROXY_SETTING_NONE = 0,
        WMT_PROXY_SETTING_MANUAL = 1,
        WMT_PROXY_SETTING_AUTO = 2,
        WMT_PROXY_SETTING_BROWSER = 3,
        WMT_PROXY_SETTING_MAX = 4
    }

    enum WMT_WATERMARK_ENTRY_TYPE
    {
        WMT_WMETYPE_AUDIO = 1,
        WMT_WMETYPE_VIDEO = 2
    }

}
