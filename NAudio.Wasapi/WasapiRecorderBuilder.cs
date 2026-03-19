using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudio.Wasapi
{
    /// <summary>
    /// Fluent builder for creating a <see cref="WasapiRecorder"/>.
    /// </summary>
    public class WasapiRecorderBuilder
    {
        private MMDevice device;
        private AudioClientShareMode shareMode = AudioClientShareMode.Shared;
        private bool useEventSync = true;
        private int bufferMilliseconds = 100;
        private WaveFormat requestedFormat;
        private string mmcssTaskName;
        private uint? processLoopbackId;
        private ProcessLoopbackMode processLoopbackMode = ProcessLoopbackMode.IncludeTargetProcessTree;

        /// <summary>
        /// Use the specified audio device for capture.
        /// </summary>
        public WasapiRecorderBuilder WithDevice(MMDevice device)
        {
            this.device = device;
            return this;
        }

        /// <summary>
        /// Use shared mode (default).
        /// </summary>
        public WasapiRecorderBuilder WithSharedMode()
        {
            shareMode = AudioClientShareMode.Shared;
            return this;
        }

        /// <summary>
        /// Use exclusive mode for lower latency capture.
        /// </summary>
        public WasapiRecorderBuilder WithExclusiveMode()
        {
            shareMode = AudioClientShareMode.Exclusive;
            return this;
        }

        /// <summary>
        /// Use event-based synchronization (default).
        /// </summary>
        public WasapiRecorderBuilder WithEventSync()
        {
            useEventSync = true;
            return this;
        }

        /// <summary>
        /// Use polling-based synchronization.
        /// </summary>
        public WasapiRecorderBuilder WithPollingSync()
        {
            useEventSync = false;
            return this;
        }

        /// <summary>
        /// Set the internal buffer length in milliseconds. Default is 100ms.
        /// Lower values reduce latency but increase CPU usage.
        /// </summary>
        public WasapiRecorderBuilder WithBufferLength(int milliseconds)
        {
            bufferMilliseconds = milliseconds;
            return this;
        }

        /// <summary>
        /// Request a specific capture format. If not set, uses the device's mix format.
        /// In shared mode with AutoConvertPcm, the engine will convert to this format.
        /// </summary>
        public WasapiRecorderBuilder WithFormat(WaveFormat format)
        {
            requestedFormat = format;
            return this;
        }

        /// <summary>
        /// Elevate the capture thread priority via MMCSS.
        /// Common task names: "Pro Audio", "Audio", "Capture".
        /// </summary>
        public WasapiRecorderBuilder WithMmcssThreadPriority(string taskName = "Pro Audio")
        {
            mmcssTaskName = taskName;
            return this;
        }

        /// <summary>
        /// Capture audio from a specific process (and optionally its child processes).
        /// Requires Windows 10 2004 (build 19041) or later.
        /// This uses ActivateAudioInterfaceAsync with AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS.
        /// </summary>
        /// <param name="processId">The process ID to capture audio from.</param>
        /// <param name="mode">Whether to include or exclude the target process tree.</param>
        public WasapiRecorderBuilder WithProcessLoopback(uint processId,
            ProcessLoopbackMode mode = ProcessLoopbackMode.IncludeTargetProcessTree)
        {
            processLoopbackId = processId;
            processLoopbackMode = mode;
            return this;
        }

        /// <summary>
        /// Builds the <see cref="WasapiRecorder"/> with the configured settings.
        /// </summary>
        public WasapiRecorder Build()
        {
            if (processLoopbackId.HasValue)
            {
                return WasapiRecorder.CreateProcessLoopback(
                    processLoopbackId.Value, processLoopbackMode,
                    useEventSync, bufferMilliseconds, requestedFormat, mmcssTaskName);
            }

            var actualDevice = device ?? GetDefaultCaptureDevice();
            return new WasapiRecorder(actualDevice, shareMode, useEventSync,
                bufferMilliseconds, requestedFormat, mmcssTaskName);
        }

        private static MMDevice GetDefaultCaptureDevice()
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }
    }
}
