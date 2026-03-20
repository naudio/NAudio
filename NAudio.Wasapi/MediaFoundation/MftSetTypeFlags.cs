using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for setting or testing the media type on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_SET_TYPE_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_set_type_flags">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftSetTypeFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Test the proposed media type, but do not set it.
        /// </summary>
        /// <remarks>MFT_SET_TYPE_TEST_ONLY</remarks>
        TestOnly = 0x00000001
    }
}
