using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains media type information for registering a Media Foundation transform (MFT).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class MftRegisterTypeInfo
    {
        /// <summary>
        /// The major media type.
        /// </summary>
        public Guid MajorType;
        /// <summary>
        /// The media subtype.
        /// </summary>
        public Guid SubType;
    }
}
