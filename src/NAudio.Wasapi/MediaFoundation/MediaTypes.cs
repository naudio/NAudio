using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Major Media Types
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa367377%28v=vs.85%29.aspx
    /// </summary>
    public static class MediaTypes
    {
        /// <summary>
        /// Default
        /// </summary>
        public static readonly Guid MFMediaType_Default = new Guid("81A412E6-8103-4B06-857F-1862781024AC");
        /// <summary>
        /// Audio
        /// </summary>
        [FieldDescription("Audio")]
        public static readonly Guid MFMediaType_Audio = new Guid("73647561-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Video
        /// </summary>
        [FieldDescription("Video")]
        public static readonly Guid MFMediaType_Video = new Guid("73646976-0000-0010-8000-00aa00389b71");
        /// <summary>
        /// Protected Media
        /// </summary>
        [FieldDescription("Protected Media")]
        public static readonly Guid MFMediaType_Protected = new Guid("7b4b6fe6-9d04-4494-be14-7e0bd076c8e4");
        /// <summary>
        /// Synchronized Accessible Media Interchange (SAMI) captions.
        /// </summary>
        [FieldDescription("SAMI captions")]
        public static readonly Guid MFMediaType_SAMI = new Guid("e69669a0-3dcd-40cb-9e2e-3708387c0616");
        /// <summary>
        /// Script stream
        /// </summary>
        [FieldDescription("Script stream")]
        public static readonly Guid MFMediaType_Script = new Guid("72178c22-e45b-11d5-bc2a-00b0d0f3f4ab");
        /// <summary>
        /// Still image stream.
        /// </summary>
        [FieldDescription("Still image stream")]
        public static readonly Guid MFMediaType_Image = new Guid("72178c23-e45b-11d5-bc2a-00b0d0f3f4ab");
        /// <summary>
        /// HTML stream.
        /// </summary>
        [FieldDescription("HTML stream")]
        public static readonly Guid MFMediaType_HTML = new Guid("72178c24-e45b-11d5-bc2a-00b0d0f3f4ab");
        /// <summary>
        /// Binary stream.
        /// </summary>
        [FieldDescription("Binary stream")]
        public static readonly Guid MFMediaType_Binary = new Guid("72178c25-e45b-11d5-bc2a-00b0d0f3f4ab");
        /// <summary>
        /// A stream that contains data files.
        /// </summary>
        [FieldDescription("File transfer")]
        public static readonly Guid MFMediaType_FileTransfer = new Guid("72178c26-e45b-11d5-bc2a-00b0d0f3f4ab");
    }
}
