using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains flags for registering and enumerating Media Foundation transforms (MFTs).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_ENUM_FLAG</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_enum_flag">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftEnumFlags
    {
        /// <summary>
        /// None
        /// </summary>
        /// <remarks>None</remarks>
        None = 0,
        /// <summary>
        /// The MFT performs synchronous data processing in software.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_SYNCMFT</remarks>
        SyncMft = 0x00000001,
        /// <summary>
        /// The MFT performs asynchronous data processing in software.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_ASYNCMFT</remarks>
        AsyncMft = 0x00000002,
        /// <summary>
        /// The MFT performs hardware-based data processing, using either the AVStream driver or a GPU-based proxy MFT.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_HARDWARE</remarks>
        Hardware = 0x00000004,
        /// <summary>
        /// The MFT must be unlocked by the application before use.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_FIELDOFUSE</remarks>
        FieldOfUse = 0x00000008,
        /// <summary>
        /// For enumeration, include MFTs that were registered in the caller's process.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_LOCALMFT</remarks>
        LocalMft = 0x00000010,
        /// <summary>
        /// The MFT is optimized for transcoding rather than playback.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_TRANSCODE_ONLY</remarks>
        TranscodeOnly = 0x00000020,
        /// <summary>
        /// For enumeration, sort and filter the results.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_SORTANDFILTER</remarks>
        SortAndFilter = 0x00000040,
        /// <summary>
        /// Bitwise OR of all the flags, excluding SortAndFilter.
        /// </summary>
        /// <remarks>MFT_ENUM_FLAG_ALL</remarks>
        All = 0x0000003F
    }
}
