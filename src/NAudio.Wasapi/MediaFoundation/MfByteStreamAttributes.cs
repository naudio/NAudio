using System;

namespace NAudio.MediaFoundation;

/// <summary>
/// Attributes that can be applied to <see cref="Interfaces.IMFByteStream"/> objects.
/// </summary>
internal static class MfByteStreamAttributes
{
    /// <summary>
    /// MF_BYTESTREAM_CONTENT_TYPE <br />
    /// Data type: LPWSTR <br />
    /// Specifies the MIME type of a byte stream.
    /// </summary>
    public static Guid ContentType => new(0xfc358289, 0x3cb6, 0x460c, 0xa4, 0x24, 0xb6, 0x68, 0x12, 0x60, 0x37, 0x5a);

    /// <summary>
    /// MF_BYTESTREAM_ORIGIN_NAME <br />
    /// Data type: LPWSTR <br />
    /// Specifies the original URL for a byte stream. <br />
    /// File-based byte streams can support this attribute. <br />
    /// The attribute value is set when the byte stream is created.
    /// </summary>
    public static Guid OriginName => new(0xfc358288, 0x3cb6, 0x460c, 0xa4, 0x24, 0xb6, 0x68, 0x12, 0x60, 0x37, 0x5a);

    /// <summary>
    /// MF_BYTESTREAM_LAST_MODIFIED_TIME <br />
    /// Type: BLOB <br />
    /// The format of this blob is given by the FILETIME structure. <br />
    /// It specifies the time the presentation was last modified. <br />
    /// This attribute is optional.
    /// </summary>
    public static Guid LastModifiedTime => new(0xfc35828b, 0x3cb6, 0x460c, 0xa4, 0x24, 0xb6, 0x68, 0x12, 0x60, 0x37, 0x5a);

    /// <summary>
    /// MF_BYTESTREAM_DURATION <br />
    /// Data type: int64 <br />
    /// Duration in 100ns units of the presentation. <br />
    /// This attribute is optional.
    /// </summary>
    public static Guid Duration => new(0xfc35828a, 0x3cb6, 0x460c, 0xa4, 0x24, 0xb6, 0x68, 0x12, 0x60, 0x37, 0x5a);
}
