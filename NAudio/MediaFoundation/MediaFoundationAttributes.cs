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
        public static readonly Guid MF_TRANSFORM_ASYNC = new Guid("f81a699a-649a-497d-8c73-29f8fed6ad7a");
        /// <summary>
        /// Enables the use of an asynchronous MFT.
        /// </summary>
        public static readonly Guid MF_TRANSFORM_ASYNC_UNLOCK = new Guid("e5666d6b-3422-4eb6-a421-da7db1f8e207");
        /// <summary>
        /// Contains flags for an MFT activation object.
        /// </summary>
        [FieldDescription("Transform Flags")]
        public static readonly Guid MF_TRANSFORM_FLAGS_Attribute = new Guid("9359bb7e-6275-46c4-a025-1c01e45f1a86");
        /// <summary>
        /// Specifies the category for an MFT.
        /// </summary>
        [FieldDescription("Transform Category")]
        public static readonly Guid MF_TRANSFORM_CATEGORY_Attribute = new Guid("ceabba49-506d-4757-a6ff-66c184987e4e");
        /// <summary>
        /// Contains the class identifier (CLSID) of an MFT.
        /// </summary>
        [FieldDescription("Class identifier")]
        public static readonly Guid MFT_TRANSFORM_CLSID_Attribute = new Guid("6821c42b-65a4-4e82-99bc-9a88205ecd0c");
        /// <summary>
        /// Contains the registered input types for a Media Foundation transform (MFT).
        /// </summary>
        [FieldDescription("Input Types")]
        public static readonly Guid MFT_INPUT_TYPES_Attributes = new Guid("4276c9b1-759d-4bf3-9cd0-0d723d138f96");
        /// <summary>
        /// Contains the registered output types for a Media Foundation transform (MFT).
        /// </summary>
        [FieldDescription("Output Types")]
        public static readonly Guid MFT_OUTPUT_TYPES_Attributes = new Guid("8eae8cf3-a44f-4306-ba5c-bf5dda242818");
        /// <summary>
        /// Contains the symbolic link for a hardware-based MFT.
        /// </summary>
        public static readonly Guid MFT_ENUM_HARDWARE_URL_Attribute = new Guid("2fb866ac-b078-4942-ab6c-003d05cda674");
        /// <summary>
        /// Contains the display name for a hardware-based MFT.
        /// </summary>
        [FieldDescription("Name")]
        public static readonly Guid MFT_FRIENDLY_NAME_Attribute = new Guid(0x314ffbae, 0x5b41, 0x4c95, 0x9c, 0x19, 0x4e, 0x7d, 0x58, 0x6f, 0xac, 0xe3);
        /// <summary>
        ///  	Contains a pointer to the stream attributes of the connected stream on a hardware-based MFT.
        /// </summary>
        public static readonly Guid MFT_CONNECTED_STREAM_ATTRIBUTE = new Guid("71eeb820-a59f-4de2-bcec-38db1dd611a4");
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
        public static readonly Guid MFT_HW_TIMESTAMP_WITH_QPC_Attribute = new Guid("8d030fb8-cc43-4258-a22e-9210bef89be4");
        /// <summary>
        /// Contains an IMFFieldOfUseMFTUnlock pointer, which can be used to unlock the MFT.
        /// </summary>
        public static readonly Guid MFT_FIELDOFUSE_UNLOCK_Attribute = new Guid("8ec2e9fd-9148-410d-831e-702439461a8e");
        /// <summary>
        /// Contains the merit value of a hardware codec.
        /// </summary>
        public static readonly Guid MFT_CODEC_MERIT_Attribute = new Guid("88a7cb15-7b07-4a34-9128-e64c6703c4d3");
        /// <summary>
        /// Specifies whether a decoder is optimized for transcoding rather than for playback.
        /// </summary>
        public static readonly Guid MFT_ENUM_TRANSCODE_ONLY_ATTRIBUTE = new Guid("111ea8cd-b62a-4bdb-89f6-67ffcdc2458b");

        // Presentation descriptor attributes:
        // http://msdn.microsoft.com/en-gb/library/windows/desktop/aa367736%28v=vs.85%29.aspx

        // in mfid1.h
        /// <summary>
        /// Contains a pointer to the proxy object for the application's presentation descriptor.
        /// </summary>
        [FieldDescription("PMP Host Context")]
        public static readonly Guid MF_PD_PMPHOST_CONTEXT = new Guid("6c990d31-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Contains a pointer to the presentation descriptor from the protected media path (PMP).
        /// </summary>
        [FieldDescription("App Context")]
        public static readonly Guid MF_PD_APP_CONTEXT = new Guid("6c990d32-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies the duration of a presentation, in 100-nanosecond units.
        /// </summary>
        [FieldDescription("Duration")]
        public static readonly Guid MF_PD_DURATION = new Guid("6c990d33-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies the total size of the source file, in bytes. 
        /// </summary>
        [FieldDescription("Total File Size")]
        public static readonly Guid MF_PD_TOTAL_FILE_SIZE = new Guid("6c990d34-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies the audio encoding bit rate for the presentation, in bits per second.
        /// </summary>
        [FieldDescription("Audio encoding bitrate")]
        public static readonly Guid MF_PD_AUDIO_ENCODING_BITRATE = new Guid("6c990d35-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies the video encoding bit rate for the presentation, in bits per second.
        /// </summary>
        [FieldDescription("Video Encoding Bitrate")]
        public static readonly Guid MF_PD_VIDEO_ENCODING_BITRATE = new Guid("6c990d36-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies the MIME type of the content.
        /// </summary>
        [FieldDescription("MIME Type")]
        public static readonly Guid MF_PD_MIME_TYPE = new Guid("6c990d37-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies when a presentation was last modified.
        /// </summary>
        [FieldDescription("Last Modified Time")]
        public static readonly Guid MF_PD_LAST_MODIFIED_TIME = new Guid("6c990d38-bb8e-477a-8598-0d5d96fcd88a");
        // win 7 and above:
        /// <summary>
        /// The identifier of the playlist element in the presentation.
        /// </summary>
        [FieldDescription("Element ID")]
        public static readonly Guid MF_PD_PLAYBACK_ELEMENT_ID = new Guid("6c990d39-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Contains the preferred RFC 1766 language of the media source.
        /// </summary>
        [FieldDescription("Preferred Language")]
        public static readonly Guid MF_PD_PREFERRED_LANGUAGE = new Guid("6c990d3a-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// The time at which the presentation must begin, relative to the start of the media source.
        /// </summary>
        [FieldDescription("Playback boundary time")]
        public static readonly Guid MF_PD_PLAYBACK_BOUNDARY_TIME = new Guid("6c990d3b-bb8e-477a-8598-0d5d96fcd88a");
        /// <summary>
        /// Specifies whether the audio streams in the presentation have a variable bit rate.
        /// </summary>
        [FieldDescription("Audio is variable bitrate")]
        public static readonly Guid MF_PD_AUDIO_ISVARIABLEBITRATE = new Guid("33026ee0-e387-4582-ae0a-34a2ad3baa18");

    }
}
