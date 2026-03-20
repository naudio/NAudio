using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains flags for registering and enumerating Media Foundation transforms (MFTs).
    /// </summary>
    [Flags]
    public enum MftEnumFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The MFT performs synchronous data processing in software.
        /// </summary>
        SyncMft = 0x00000001,
        /// <summary>
        /// The MFT performs asynchronous data processing in software.
        /// </summary>
        AsyncMft = 0x00000002,
        /// <summary>
        /// The MFT performs hardware-based data processing, using either the AVStream driver or a GPU-based proxy MFT.
        /// </summary>
        Hardware = 0x00000004,
        /// <summary>
        /// The MFT must be unlocked by the application before use.
        /// </summary>
        FieldOfUse = 0x00000008,
        /// <summary>
        /// For enumeration, include MFTs that were registered in the caller's process.
        /// </summary>
        LocalMft = 0x00000010,
        /// <summary>
        /// The MFT is optimized for transcoding rather than playback.
        /// </summary>
        TranscodeOnly = 0x00000020,
        /// <summary>
        /// For enumeration, sort and filter the results.
        /// </summary>
        SortAndFilter = 0x00000040,
        /// <summary>
        /// Bitwise OR of all the flags, excluding SortAndFilter.
        /// </summary>
        All = 0x0000003F
    }
}
