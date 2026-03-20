using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains flags that indicate the status of the IMFSourceReader ReadSample method.
    /// </summary>
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
        Error = 0x00000001,
        /// <summary>
        /// The source reader reached the end of the stream.
        /// </summary>
        EndOfStream = 0x00000002,
        /// <summary>
        /// One or more new streams were created.
        /// </summary>
        NewStream = 0x00000004,
        /// <summary>
        /// The native format has changed for one or more streams.
        /// </summary>
        NativeMediaTypeChanged = 0x00000010,
        /// <summary>
        /// The current media type has changed for one or more streams.
        /// </summary>
        CurrentMediaTypeChanged = 0x00000020,
        /// <summary>
        /// There is a gap in the stream. This flag corresponds to an MEStreamTick event from the media source.
        /// </summary>
        StreamTick = 0x00000100,
        /// <summary>
        /// All transforms inserted by the application have been removed for a particular stream.
        /// </summary>
        AllEffectsRemoved = 0x00000200
    }
}
