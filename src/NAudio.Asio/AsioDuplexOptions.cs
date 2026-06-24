namespace NAudio.Wave
{
    /// <summary>
    /// Options for configuring an <see cref="AsioDevice"/> in duplex mode via <see cref="AsioDevice.InitDuplex"/>.
    /// Duplex mode runs a user-supplied <see cref="AsioProcessCallback"/> that reads input and writes output in a single
    /// buffer-switch callback.
    /// </summary>
    public sealed class AsioDuplexOptions
    {
        /// <summary>
        /// Physical input channel indices. May be non-contiguous. The processor callback addresses them via
        /// <see cref="AsioProcessBuffers.GetInput"/> using zero-based indices into this array.
        /// </summary>
        public int[] InputChannels { get; init; }

        /// <summary>
        /// Physical output channel indices. May be non-contiguous. The processor callback writes to them via
        /// <see cref="AsioProcessBuffers.GetOutput"/> using zero-based indices into this array.
        /// Outputs not written by the callback are zeroed by the library before native-format conversion.
        /// </summary>
        public int[] OutputChannels { get; init; }

        /// <summary>
        /// Desired sample rate in Hz. If <c>null</c>, uses the driver's current sample rate
        /// (<see cref="AsioDevice.CurrentSampleRate"/>).
        /// </summary>
        public int? SampleRate { get; init; }

        /// <summary>
        /// Requested ASIO buffer size in frames. If <c>null</c>, uses the driver's preferred buffer size.
        /// </summary>
        public int? BufferSize { get; init; }

        /// <summary>
        /// User-supplied processing callback. Required. Runs on the ASIO driver's real-time thread — see the
        /// remarks on <see cref="AsioProcessCallback"/> for constraints.
        /// </summary>
        public AsioProcessCallback Processor { get; init; }
    }
}
