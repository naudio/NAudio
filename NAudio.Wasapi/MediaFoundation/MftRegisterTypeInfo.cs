using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains media type information for registering a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: MFT_REGISTER_TYPE_INFO (mftransform.h).
    /// See https://learn.microsoft.com/windows/win32/api/mftransform/ns-mftransform-mft_register_type_info
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public class MftRegisterTypeInfo
    {
        /// <summary>
        /// The major media type.
        /// </summary>
        /// <remarks>guidMajorType</remarks>
        public Guid MajorType;
        /// <summary>
        /// The media subtype.
        /// </summary>
        /// <remarks>guidSubtype</remarks>
        public Guid SubType;
    }
}
