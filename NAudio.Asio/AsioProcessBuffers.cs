using System;
using NAudio.Wave.Asio;

namespace NAudio.Wave
{
    /// <summary>
    /// Per-callback view of input and output audio for a duplex <see cref="AsioDevice"/> session.
    /// Passed by reference to the user's <see cref="AsioProcessCallback"/>.
    /// </summary>
    /// <remarks>
    /// This is a <c>ref struct</c>: it lives on the stack and cannot outlive the buffer-switch callback.
    /// All spans returned from it point into library-owned or driver-owned memory and are invalidated
    /// the moment the callback returns. Do not capture any of these values across callbacks.
    /// </remarks>
    public readonly ref struct AsioProcessBuffers
    {
        private readonly AsioCallbackContext context;

        internal AsioProcessBuffers(AsioCallbackContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Number of audio frames per channel in this callback.
        /// </summary>
        public int Frames => context.Frames;

        /// <summary>
        /// Sample rate the driver is currently running at, in Hz.
        /// </summary>
        public int SampleRate => context.SampleRate;

        /// <summary>
        /// Number of input channels selected at init time.
        /// </summary>
        public int InputChannelCount => context.InputChannelCount;

        /// <summary>
        /// Number of output channels selected at init time.
        /// </summary>
        public int OutputChannelCount => context.OutputChannelCount;

        /// <summary>
        /// Position of the first frame in this buffer, in frames since the driver started, as reported by the driver.
        /// Zero if the driver did not report a position for this callback.
        /// </summary>
        public long SamplePosition => context.SamplePosition;

        /// <summary>
        /// Host system time, in nanoseconds, corresponding to <see cref="SamplePosition"/>. This pins the audio clock
        /// to the host clock at one precise instant per buffer — use it for A/V sync, drift correction against
        /// <see cref="System.Diagnostics.Stopwatch"/>, or aligning multiple ASIO devices. Zero if the driver did not
        /// report a timestamp for this callback.
        /// </summary>
        public long SystemTimeNanoseconds => context.SystemTimeNanoseconds;

        /// <summary>
        /// Varispeed factor reported by the driver (1.0 = nominal). Most drivers report 1.0; non-1.0 values appear
        /// when the driver is doing pull-up/pull-down (29.97 vs 30 fps film transfer) or external rate adaptation.
        /// </summary>
        public double Speed => context.Speed;

        /// <summary>
        /// SMPTE/MTC time code from an external source (LTC input, MTC over MIDI, etc.) corresponding to this buffer.
        /// <c>null</c> when no external time-code source is connected — the common case.
        /// </summary>
        public AsioTimeCodeInfo? TimeCode => context.TimeCode;

        /// <summary>
        /// Gets a read-only span of converted float samples for the specified input channel.
        /// </summary>
        /// <param name="channelIndex">Zero-based index into the selected-inputs array, not the physical channel index. Valid range: <c>0</c> to <see cref="InputChannelCount"/> - 1.</param>
        /// <returns>A span of length <see cref="Frames"/>. Valid only for the duration of the callback.</returns>
        public ReadOnlySpan<float> GetInput(int channelIndex)
        {
            if ((uint)channelIndex >= (uint)context.InputChannelCount)
                throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Expected index in [0, {context.InputChannelCount - 1}].");
            return context.InputFloatBuffers[channelIndex].AsSpan(0, context.Frames);
        }

        /// <summary>
        /// Gets a writable span for the specified output channel. Samples are converted to the driver's native format on callback return.
        /// </summary>
        /// <param name="channelIndex">Zero-based index into the selected-outputs array, not the physical channel index. Valid range: <c>0</c> to <see cref="OutputChannelCount"/> - 1.</param>
        /// <returns>A span of length <see cref="Frames"/>. Unwritten frames are zeroed by the library before conversion.</returns>
        public Span<float> GetOutput(int channelIndex)
        {
            if ((uint)channelIndex >= (uint)context.OutputChannelCount)
                throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Expected index in [0, {context.OutputChannelCount - 1}].");
            return context.OutputFloatBuffers[channelIndex].AsSpan(0, context.Frames);
        }

        /// <summary>
        /// Zero-copy access to the specified input channel in the driver's native sample format.
        /// </summary>
        /// <param name="channelIndex">Zero-based index into the selected-inputs array.</param>
        public unsafe AsioRawInputBuffer RawInput(int channelIndex)
        {
            if ((uint)channelIndex >= (uint)context.InputChannelCount)
                throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Expected index in [0, {context.InputChannelCount - 1}].");
            var bytes = new ReadOnlySpan<byte>((void*)context.InputNativeBuffers[channelIndex], context.InputNativeBytesPerChannel);
            return new AsioRawInputBuffer(bytes, context.InputFormat, context.Frames);
        }

        /// <summary>
        /// Zero-copy access to the specified output channel in the driver's native sample format.
        /// Writing through this buffer bypasses float-to-native conversion and the unwritten-output clearing —
        /// the caller becomes responsible for writing every frame in this channel for this callback.
        /// </summary>
        /// <param name="channelIndex">Zero-based index into the selected-outputs array.</param>
        public unsafe AsioRawOutputBuffer RawOutput(int channelIndex)
        {
            if ((uint)channelIndex >= (uint)context.OutputChannelCount)
                throw new ArgumentOutOfRangeException(nameof(channelIndex), $"Expected index in [0, {context.OutputChannelCount - 1}].");
            // Mark the channel as raw-accessed so the duplex post-callback path skips float→native conversion for it.
            context.OutputRawAccessed[channelIndex] = true;
            var bytes = new Span<byte>((void*)context.OutputNativeBuffers[channelIndex], context.OutputNativeBytesPerChannel);
            return new AsioRawOutputBuffer(bytes, context.OutputFormat, context.Frames);
        }
    }

    /// <summary>
    /// Internal per-callback state shared between an <see cref="AsioProcessBuffers"/> ref struct,
    /// an <see cref="AsioAudioCapturedEventArgs"/>, and their originating <see cref="AsioDevice"/>.
    /// Reused across callbacks; <see cref="Valid"/> is set false by the device once the handler returns.
    /// </summary>
    internal sealed class AsioCallbackContext
    {
        public int Frames;
        public int SampleRate;
        public int InputChannelCount;
        public int OutputChannelCount;
        public long SamplePosition;
        public long SystemTimeNanoseconds;
        public double Speed;
        public AsioTimeCodeInfo? TimeCode;

        public AsioSampleType InputFormat;
        public AsioSampleType OutputFormat;

        public IntPtr[] InputNativeBuffers;     // driver-owned pointers, one per selected input channel
        public IntPtr[] OutputNativeBuffers;    // driver-owned pointers, one per selected output channel
        public float[][] InputFloatBuffers;     // library-owned, pre-allocated to [Frames] per channel
        public float[][] OutputFloatBuffers;    // library-owned, pre-allocated to [Frames] per channel
        public int InputNativeBytesPerChannel;
        public int OutputNativeBytesPerChannel;
        public bool[] OutputRawAccessed;        // set true by AsioProcessBuffers.RawOutput for the corresponding channel

        public bool Valid;
    }
}
