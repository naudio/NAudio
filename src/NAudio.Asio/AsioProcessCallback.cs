namespace NAudio.Wave
{
    /// <summary>
    /// User-supplied processing callback for <see cref="AsioDevice.InitDuplex"/>.
    /// Called once per ASIO buffer-switch with a stack-only view of input and output channels.
    /// </summary>
    /// <param name="buffers">Per-channel input and output buffers for this buffer switch. Valid only for the duration of the call.</param>
    /// <remarks>
    /// The callback runs on the ASIO driver's real-time thread. It must not call
    /// <see cref="AsioDevice.Stop"/>, <see cref="AsioDevice.Dispose"/>, or <see cref="AsioDevice.Reinitialize"/>,
    /// must not allocate or perform blocking I/O, and must return promptly — the time budget is the
    /// ASIO buffer duration (often under 10ms).
    /// </remarks>
    public delegate void AsioProcessCallback(in AsioProcessBuffers buffers);
}
