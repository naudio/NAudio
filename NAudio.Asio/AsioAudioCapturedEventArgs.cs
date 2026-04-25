using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Event args for <see cref="AsioDevice.AudioCaptured"/>. Exposes one converted float span per selected input channel,
    /// plus a zero-copy raw escape hatch. Valid only for the duration of the event handler invocation.
    /// </summary>
    public sealed class AsioAudioCapturedEventArgs : EventArgs
    {
        private readonly AsioCallbackContext context;

        internal AsioAudioCapturedEventArgs(AsioCallbackContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Number of audio frames per channel in this callback.
        /// </summary>
        public int Frames { get { ThrowIfInvalid(); return context.Frames; } }

        /// <summary>
        /// Sample rate the driver is currently running at, in Hz.
        /// </summary>
        public int SampleRate { get { ThrowIfInvalid(); return context.SampleRate; } }

        /// <summary>
        /// Number of input channels selected at init time.
        /// </summary>
        public int ChannelCount { get { ThrowIfInvalid(); return context.InputChannelCount; } }

        /// <summary>
        /// Position of the first frame in this buffer, in frames since the driver started, as reported by the driver.
        /// Zero if the driver did not report a position for this callback.
        /// </summary>
        public long SamplePosition { get { ThrowIfInvalid(); return context.SamplePosition; } }

        /// <summary>
        /// Host system time, in nanoseconds, corresponding to <see cref="SamplePosition"/>. This pins the audio clock
        /// to the host clock at one precise instant per buffer — use it for A/V sync, drift correction against
        /// <see cref="System.Diagnostics.Stopwatch"/>, or aligning multiple ASIO devices. Zero if the driver did not
        /// report a timestamp for this callback.
        /// </summary>
        public long SystemTimeNanoseconds { get { ThrowIfInvalid(); return context.SystemTimeNanoseconds; } }

        /// <summary>
        /// Varispeed factor reported by the driver (1.0 = nominal). Most drivers report 1.0; non-1.0 values appear
        /// when the driver is doing pull-up/pull-down (29.97 vs 30 fps film transfer) or external rate adaptation.
        /// </summary>
        public double Speed { get { ThrowIfInvalid(); return context.Speed; } }

        /// <summary>
        /// SMPTE/MTC time code from an external source (LTC input, MTC over MIDI, etc.) corresponding to this buffer.
        /// <c>null</c> when no external time-code source is connected — the common case.
        /// </summary>
        public AsioTimeCodeInfo? TimeCode { get { ThrowIfInvalid(); return context.TimeCode; } }

        /// <summary>
        /// Gets a read-only span of converted float samples for the specified channel.
        /// </summary>
        /// <param name="channelIndex">Zero-based index into the selected-inputs array, not the physical channel index. Valid range: <c>0</c> to <see cref="ChannelCount"/> - 1.</param>
        /// <returns>A span of length <see cref="Frames"/>. Valid only for the duration of the event handler.</returns>
        public ReadOnlySpan<float> GetChannel(int channelIndex)
        {
            ThrowIfInvalid();
            if ((uint)channelIndex >= (uint)context.InputChannelCount)
                throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Expected index in [0, {context.InputChannelCount - 1}].");
            return context.InputFloatBuffers[channelIndex].AsSpan(0, context.Frames);
        }

        /// <summary>
        /// Zero-copy access to the specified channel in the driver's native sample format.
        /// </summary>
        /// <param name="channelIndex">Zero-based index into the selected-inputs array.</param>
        public unsafe AsioRawInputBuffer RawInput(int channelIndex)
        {
            ThrowIfInvalid();
            if ((uint)channelIndex >= (uint)context.InputChannelCount)
                throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Expected index in [0, {context.InputChannelCount - 1}].");
            var bytes = new ReadOnlySpan<byte>((void*)context.InputNativeBuffers[channelIndex], context.InputNativeBytesPerChannel);
            return new AsioRawInputBuffer(bytes, context.InputFormat, context.Frames);
        }

        private void ThrowIfInvalid()
        {
            if (!context.Valid)
                throw new InvalidOperationException(
                    "AsioAudioCapturedEventArgs is only valid for the duration of the handler. " +
                    "Do not access it after the event handler returns.");
        }
    }
}
