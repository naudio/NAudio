namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines messages for a Media Foundation transform (MFT).
    /// </summary>
    public enum MftMessageType
    {
        /// <summary>
        /// Requests the MFT to flush all stored data.
        /// </summary>
        Flush = 0x00000000,
        /// <summary>
        /// Requests the MFT to drain any stored data.
        /// </summary>
        Drain = 0x00000001,
        /// <summary>
        /// Sets or clears the Direct3D Device Manager for DirectX Video Acceleration (DXVA).
        /// </summary>
        SetD3DManager = 0x00000002,
        /// <summary>
        /// Drop samples.
        /// </summary>
        DropSamples = 0x00000003,
        /// <summary>
        /// Command tick.
        /// </summary>
        CommandTick = 0x00000004,
        /// <summary>
        /// Notifies the MFT that streaming is about to begin.
        /// </summary>
        NotifyBeginStreaming = 0x10000000,
        /// <summary>
        /// Notifies the MFT that streaming is about to end.
        /// </summary>
        NotifyEndStreaming = 0x10000001,
        /// <summary>
        /// Notifies the MFT that an input stream has ended.
        /// </summary>
        NotifyEndOfStream = 0x10000002,
        /// <summary>
        /// Notifies the MFT that the first sample is about to be processed.
        /// </summary>
        NotifyStartOfStream = 0x10000003,
        /// <summary>
        /// Marks a point in the stream. This message applies only to asynchronous MFTs.
        /// </summary>
        CommandMarker = 0x20000000
    }
}
