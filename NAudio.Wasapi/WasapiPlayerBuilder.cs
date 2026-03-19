using NAudio.CoreAudioApi;

namespace NAudio.Wave
{
    /// <summary>
    /// Fluent builder for creating a <see cref="WasapiPlayer"/>.
    /// </summary>
    public class WasapiPlayerBuilder
    {
        private MMDevice device;
        private AudioClientShareMode shareMode = AudioClientShareMode.Shared;
        private int latencyMilliseconds = 200;
        private bool useEventSync = true;
        private AudioStreamCategory? audioCategory;
        private string mmcssTaskName;
        private bool preferLowLatency;

        /// <summary>
        /// Use the specified audio device for playback.
        /// </summary>
        public WasapiPlayerBuilder WithDevice(MMDevice device)
        {
            this.device = device;
            return this;
        }

        /// <summary>
        /// Use shared mode (default). Audio is mixed with other applications.
        /// </summary>
        public WasapiPlayerBuilder WithSharedMode()
        {
            shareMode = AudioClientShareMode.Shared;
            return this;
        }

        /// <summary>
        /// Use exclusive mode. The application has sole access to the audio device.
        /// Lower latency is possible but other applications cannot play audio.
        /// </summary>
        public WasapiPlayerBuilder WithExclusiveMode()
        {
            shareMode = AudioClientShareMode.Exclusive;
            return this;
        }

        /// <summary>
        /// Set the desired latency in milliseconds. Default is 200ms.
        /// In shared mode with IAudioClient3, the engine may use a lower period
        /// if <see cref="WithLowLatency"/> is also specified.
        /// </summary>
        public WasapiPlayerBuilder WithLatency(int milliseconds)
        {
            latencyMilliseconds = milliseconds;
            return this;
        }

        /// <summary>
        /// Use event-based synchronization (default). More efficient than polling.
        /// </summary>
        public WasapiPlayerBuilder WithEventSync()
        {
            useEventSync = true;
            return this;
        }

        /// <summary>
        /// Use polling-based synchronization instead of events.
        /// </summary>
        public WasapiPlayerBuilder WithPollingSync()
        {
            useEventSync = false;
            return this;
        }

        /// <summary>
        /// Set the audio stream category, used by Windows for audio policy decisions
        /// (ducking, routing, priority). Requires IAudioClient2 (Windows 8+).
        /// </summary>
        public WasapiPlayerBuilder WithCategory(AudioStreamCategory category)
        {
            audioCategory = category;
            return this;
        }

        /// <summary>
        /// Elevate the audio thread priority via MMCSS (Multimedia Class Scheduler Service).
        /// Common task names: "Pro Audio", "Audio", "Playback".
        /// </summary>
        public WasapiPlayerBuilder WithMmcssThreadPriority(string taskName = "Pro Audio")
        {
            mmcssTaskName = taskName;
            return this;
        }

        /// <summary>
        /// Request low-latency shared mode via IAudioClient3 if available.
        /// Falls back to standard initialization if IAudioClient3 is not supported.
        /// </summary>
        public WasapiPlayerBuilder WithLowLatency()
        {
            preferLowLatency = true;
            return this;
        }

        /// <summary>
        /// Builds the <see cref="WasapiPlayer"/> with the configured settings.
        /// </summary>
        public WasapiPlayer Build()
        {
            var actualDevice = device ?? GetDefaultRenderDevice();
            return new WasapiPlayer(actualDevice, shareMode, useEventSync, latencyMilliseconds,
                audioCategory, mmcssTaskName, preferLowLatency);
        }

        private static MMDevice GetDefaultRenderDevice()
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }
    }
}
