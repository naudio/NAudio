using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Media Foundation attribute guids
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms696989%28v=vs.85%29.aspx
    /// </summary>
    public static class MediaFoundationAttributes
    {
        /// <summary>
        /// Specifies whether an MFT performs asynchronous processing.
        /// </summary>
        public static readonly Guid MF_TRANSFORM_ASYNC = new Guid(0xf81a699a, 0x649a, 0x497d, 0x8c, 0x73, 0x29, 0xf8, 0xfe, 0xd6, 0xad, 0x7a);
        /// <summary>
        /// Enables the use of an asynchronous MFT.
        /// </summary>
        public static readonly Guid MF_TRANSFORM_ASYNC_UNLOCK = new Guid(0xe5666d6b, 0x3422, 0x4eb6, 0xa4, 0x21, 0xda, 0x7d, 0xb1, 0xf8, 0xe2, 0x7);
        /// <summary>
        /// Contains flags for an MFT activation object.
        /// </summary>
        [FieldDescription("Transform Flags")]
        public static readonly Guid MF_TRANSFORM_FLAGS_Attribute = new Guid(0x9359bb7e, 0x6275, 0x46c4, 0xa0, 0x25, 0x1c, 0x1, 0xe4, 0x5f, 0x1a, 0x86);
        /// <summary>
        /// Specifies the category for an MFT.
        /// </summary>
        [FieldDescription("Transform Category")]
        public static readonly Guid MF_TRANSFORM_CATEGORY_Attribute = new Guid(0xceabba49, 0x506d, 0x4757, 0xa6, 0xff, 0x66, 0xc1, 0x84, 0x98, 0x7e, 0x4e);
        /// <summary>
        /// Contains the class identifier (CLSID) of an MFT.
        /// </summary>
        [FieldDescription("Class identifier")]
        public static readonly Guid MFT_TRANSFORM_CLSID_Attribute = new Guid(0x6821c42b, 0x65a4, 0x4e82, 0x99, 0xbc, 0x9a, 0x88, 0x20, 0x5e, 0xcd, 0xc);
        /// <summary>
        /// Contains the registered input types for a Media Foundation transform (MFT).
        /// </summary>
        [FieldDescription("Input Types")]
        public static readonly Guid MFT_INPUT_TYPES_Attributes = new Guid(0x4276c9b1, 0x759d, 0x4bf3, 0x9c, 0xd0, 0xd, 0x72, 0x3d, 0x13, 0x8f, 0x96);
        /// <summary>
        /// Contains the registered output types for a Media Foundation transform (MFT).
        /// </summary>
        [FieldDescription("Output Types")]
        public static readonly Guid MFT_OUTPUT_TYPES_Attributes = new Guid(0x8eae8cf3, 0xa44f, 0x4306, 0xba, 0x5c, 0xbf, 0x5d, 0xda, 0x24, 0x28, 0x18);
        /// <summary>
        /// Contains the symbolic link for a hardware-based MFT.
        /// </summary>
        public static readonly Guid MFT_ENUM_HARDWARE_URL_Attribute = new Guid(0x2fb866ac, 0xb078, 0x4942, 0xab, 0x6c, 0x0, 0x3d, 0x5, 0xcd, 0xa6, 0x74);
        /// <summary>
        /// Contains the display name for a hardware-based MFT.
        /// </summary>
        [FieldDescription("Name")]
        public static readonly Guid MFT_FRIENDLY_NAME_Attribute = new Guid(0x314ffbae, 0x5b41, 0x4c95, 0x9c, 0x19, 0x4e, 0x7d, 0x58, 0x6f, 0xac, 0xe3);
        /// <summary>
        ///  	Contains a pointer to the stream attributes of the connected stream on a hardware-based MFT.
        /// </summary>
        public static readonly Guid MFT_CONNECTED_STREAM_ATTRIBUTE = new Guid(0x71eeb820, 0xa59f, 0x4de2, 0xbc, 0xec, 0x38, 0xdb, 0x1d, 0xd6, 0x11, 0xa4);
        /// <summary>
        /// Specifies whether a hardware-based MFT is connected to another hardware-based MFT.
        /// </summary>
        public static readonly Guid MFT_CONNECTED_TO_HW_STREAM = new Guid(0x34e6e728, 0x6d6, 0x4491, 0xa5, 0x53, 0x47, 0x95, 0x65, 0xd, 0xb9, 0x12);
        /// <summary>
        /// Specifies the preferred output format for an encoder.
        /// </summary>
        [FieldDescription("Preferred Output Format")]
        public static readonly Guid MFT_PREFERRED_OUTPUTTYPE_Attribute = new Guid(0x7e700499, 0x396a, 0x49ee, 0xb1, 0xb4, 0xf6, 0x28, 0x2, 0x1e, 0x8c, 0x9d);
        /// <summary>
        /// Specifies whether an MFT is registered only in the application's process.
        /// </summary>
        public static readonly Guid MFT_PROCESS_LOCAL_Attribute = new Guid(0x543186e4, 0x4649, 0x4e65, 0xb5, 0x88, 0x4a, 0xa3, 0x52, 0xaf, 0xf3, 0x79);
        /// <summary>
        /// Contains configuration properties for an encoder.
        /// </summary>
        public static readonly Guid MFT_PREFERRED_ENCODER_PROFILE = new Guid(0x53004909, 0x1ef5, 0x46d7, 0xa1, 0x8e, 0x5a, 0x75, 0xf8, 0xb5, 0x90, 0x5f);
        /// <summary>
        /// Specifies whether a hardware device source uses the system time for time stamps.
        /// </summary>
        public static readonly Guid MFT_HW_TIMESTAMP_WITH_QPC_Attribute = new Guid(0x8d030fb8, 0xcc43, 0x4258, 0xa2, 0x2e, 0x92, 0x10, 0xbe, 0xf8, 0x9b, 0xe4);
        /// <summary>
        /// Contains an IMFFieldOfUseMFTUnlock pointer, which can be used to unlock the MFT.
        /// </summary>
        public static readonly Guid MFT_FIELDOFUSE_UNLOCK_Attribute = new Guid(0x8ec2e9fd, 0x9148, 0x410d, 0x83, 0x1e, 0x70, 0x24, 0x39, 0x46, 0x1a, 0x8e);
        /// <summary>
        /// Contains the merit value of a hardware codec.
        /// </summary>
        public static readonly Guid MFT_CODEC_MERIT_Attribute = new Guid(0x88a7cb15, 0x7b07, 0x4a34, 0x91, 0x28, 0xe6, 0x4c, 0x67, 0x3, 0xc4, 0xd3);
        /// <summary>
        /// Specifies whether a decoder is optimized for transcoding rather than for playback.
        /// </summary>
        public static readonly Guid MFT_ENUM_TRANSCODE_ONLY_ATTRIBUTE = new Guid(0x111ea8cd, 0xb62a, 0x4bdb, 0x89, 0xf6, 0x67, 0xff, 0xcd, 0xc2, 0x45, 0x8b);
    }
}
