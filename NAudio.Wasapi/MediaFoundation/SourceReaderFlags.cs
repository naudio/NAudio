using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains flags that indicate the status of the IMFSourceReader ReadSample method.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>MF_SOURCE_READER_FLAG</c>.
    /// Defined in <c>mfreadwrite.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mfreadwrite/ne-mfreadwrite-mf_source_reader_flag">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum SourceReaderFlags
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0,
        /// <summary>
        /// An error occurred. If you receive this flag, do not make any further calls to IMFSourceReader methods.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_ERROR</remarks>
        Error = 0x00000001,
        /// <summary>
        /// The source reader reached the end of the stream.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_ENDOFSTREAM</remarks>
        EndOfStream = 0x00000002,
        /// <summary>
        /// One or more new streams were created.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_NEWSTREAM</remarks>
        NewStream = 0x00000004,
        /// <summary>
        /// The native format has changed for one or more streams.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED</remarks>
        NativeMediaTypeChanged = 0x00000010,
        /// <summary>
        /// The current media type has changed for one or more streams.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED</remarks>
        CurrentMediaTypeChanged = 0x00000020,
        /// <summary>
        /// There is a gap in the stream. This flag corresponds to an MEStreamTick event from the media source.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_STREAMTICK</remarks>
        StreamTick = 0x00000100,
        /// <summary>
        /// All transforms inserted by the application have been removed for a particular stream.
        /// </summary>
        /// <remarks>MF_SOURCE_READERF_ALLEFFECTSREMOVED</remarks>
        AllEffectsRemoved = 0x00000200
    }
}
