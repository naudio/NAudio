namespace NAudio.Wave
{
    /// <summary>
    /// Options for configuring an <see cref="AsioDevice"/> in recording-only mode via <see cref="AsioDevice.InitRecording"/>.
    /// </summary>
    public sealed class AsioRecordingOptions
    {
        /// <summary>
        /// Physical input channel indices to record from. Required. Entries may be non-contiguous (e.g., <c>[0, 3, 5]</c>).
        /// The callback receives them in the order listed: <see cref="AsioAudioCapturedEventArgs.GetChannel"/> with
        /// index 0 returns samples from <c>InputChannels[0]</c>, etc.
        /// </summary>
        public int[] InputChannels { get; init; }

        /// <summary>
        /// Desired sample rate in Hz. If <c>null</c>, uses the driver's current sample rate
        /// (<see cref="AsioDevice.CurrentSampleRate"/>).
        /// </summary>
        public int? SampleRate { get; init; }

        /// <summary>
        /// Requested ASIO buffer size in frames. If <c>null</c>, uses the driver's preferred buffer size.
        /// </summary>
        public int? BufferSize { get; init; }
    }
}
