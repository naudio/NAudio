namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines messages for a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>MFT_MESSAGE_TYPE</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-mft_message_type">MS Learn</see>.
    /// </remarks>
    public enum MftMessageType
    {
        /// <summary>
        /// Requests the MFT to flush all stored data.
        /// </summary>
        /// <remarks>MFT_MESSAGE_COMMAND_FLUSH</remarks>
        Flush = 0x00000000,
        /// <summary>
        /// Requests the MFT to drain any stored data.
        /// </summary>
        /// <remarks>MFT_MESSAGE_COMMAND_DRAIN</remarks>
        Drain = 0x00000001,
        /// <summary>
        /// Sets or clears the Direct3D Device Manager for DirectX Video Acceleration (DXVA).
        /// </summary>
        /// <remarks>MFT_MESSAGE_SET_D3D_MANAGER</remarks>
        SetD3DManager = 0x00000002,
        /// <summary>
        /// Drop samples.
        /// </summary>
        /// <remarks>MFT_MESSAGE_DROP_SAMPLES</remarks>
        DropSamples = 0x00000003,
        /// <summary>
        /// Command tick.
        /// </summary>
        /// <remarks>MFT_MESSAGE_COMMAND_TICK</remarks>
        CommandTick = 0x00000004,
        /// <summary>
        /// Notifies the MFT that streaming is about to begin.
        /// </summary>
        /// <remarks>MFT_MESSAGE_NOTIFY_BEGIN_STREAMING</remarks>
        NotifyBeginStreaming = 0x10000000,
        /// <summary>
        /// Notifies the MFT that streaming is about to end.
        /// </summary>
        /// <remarks>MFT_MESSAGE_NOTIFY_END_STREAMING</remarks>
        NotifyEndStreaming = 0x10000001,
        /// <summary>
        /// Notifies the MFT that an input stream has ended.
        /// </summary>
        /// <remarks>MFT_MESSAGE_NOTIFY_END_OF_STREAM</remarks>
        NotifyEndOfStream = 0x10000002,
        /// <summary>
        /// Notifies the MFT that the first sample is about to be processed.
        /// </summary>
        /// <remarks>MFT_MESSAGE_NOTIFY_START_OF_STREAM</remarks>
        NotifyStartOfStream = 0x10000003,
        /// <summary>
        /// Marks a point in the stream. This message applies only to asynchronous MFTs.
        /// </summary>
        /// <remarks>MFT_MESSAGE_COMMAND_MARKER</remarks>
        CommandMarker = 0x20000000
    }
}
