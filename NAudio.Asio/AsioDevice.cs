using System;
using System.Linq;
using System.Threading;
using NAudio.Wave.Asio;

namespace NAudio.Wave
{
    /// <summary>
    /// High-level wrapper for an ASIO driver. Configurable into one of three mutually-exclusive modes:
    /// playback (<see cref="InitPlayback"/>), recording (<see cref="InitRecording"/>), or duplex processing
    /// (<see cref="InitDuplex"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="Open(string)"/> or <see cref="Open(int)"/> to obtain an instance, then call exactly one
    /// <c>Init*</c> method before <see cref="Start"/>. Once configured, the device cannot be reconfigured into
    /// a different mode — dispose and create a new instance instead.
    /// </para>
    /// <para>
    /// For simple playback with the legacy <see cref="IWavePlayer"/> interface, <see cref="AsioOut"/> remains
    /// available and is implemented as a facade over this class.
    /// </para>
    /// </remarks>
    public sealed class AsioDevice : IDisposable
    {
        private readonly SynchronizationContext syncContext;
        private readonly object stateLock = new();
        private AsioDriverExt driver;
        private AsioDeviceState state;

        // Playback-mode state (set by InitPlayback, consumed by the buffer callback)
        private IWaveProvider playbackSource;
        private AsioSampleConvertor.SampleConvertor playbackConvertor;
        private byte[] playbackStagingBuffer;
        private int playbackOutputChannelCount;
        private int framesPerBuffer;
        private bool autoStopOnEndOfStream;

        // Recording-mode state (set by InitRecording, consumed by the buffer callback)
        private AsioCallbackContext recordingContext;
        private AsioNativeToFloatConverter.ConverterFn inputConverter;

        // Duplex-mode state (set by InitDuplex, consumed by the buffer callback)
        private AsioCallbackContext duplexContext;
        private AsioNativeToFloatConverter.ConverterFn duplexInputConverter;
        private AsioFloatToNativeConverter.ConverterFn duplexOutputConverter;
        private AsioProcessCallback duplexProcessor;

        // Lifecycle
        private int callbackThreadId;
        private volatile bool autoStopTriggered;

        // Drain coordination — Dispose waits on this if a callback is in flight (Phase 0 finding F2).
        private readonly ManualResetEventSlim callbackIdle = new(initialState: true);
        private volatile bool disposing;

        // Last-applied configuration, cached for Reinitialize() (Phase 0 finding F6).
        // One of AsioPlaybackOptions / AsioRecordingOptions / AsioDuplexOptions, or null before any Init*.
        private object lastInitOptions;

        /// <summary>
        /// Gets the names of all installed ASIO drivers on this system.
        /// </summary>
        public static string[] GetDriverNames() => AsioDriver.GetAsioDriverNames();

        /// <summary>
        /// Opens the ASIO driver with the given name.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if no driver with that name is installed.</exception>
        public static AsioDevice Open(string driverName)
        {
            if (driverName is null) throw new ArgumentNullException(nameof(driverName));
            return new AsioDevice(driverName);
        }

        /// <summary>
        /// Opens the installed ASIO driver at the given zero-based index in <see cref="GetDriverNames"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if no drivers are installed or the index is out of range.</exception>
        public static AsioDevice Open(int driverIndex)
        {
            var names = GetDriverNames();
            if (names.Length == 0)
                throw new ArgumentException("There is no ASIO Driver installed on your system");
            if (driverIndex < 0 || driverIndex >= names.Length)
                throw new ArgumentOutOfRangeException(nameof(driverIndex), $"Invalid device number. Must be in the range [0,{names.Length - 1}]");
            return new AsioDevice(names[driverIndex]);
        }

        private AsioDevice(string driverName)
        {
            syncContext = SynchronizationContext.Current;
            DriverName = driverName;

            var basicDriver = AsioDriver.GetAsioDriverByName(driverName);
            try
            {
                driver = new AsioDriverExt(basicDriver);
            }
            catch
            {
                // Phase 0 finding F4 — release the underlying COM driver on init failure.
                try
                {
                    basicDriver.DisposeBuffers();
                    basicDriver.ReleaseComAsioDriver();
                }
                catch { /* best effort */ }
                throw;
            }
            driver.ResetRequestCallback = OnDriverResetRequest;
            state = AsioDeviceState.Unconfigured;
        }

        /// <summary>
        /// Name of the driver this device was opened with.
        /// </summary>
        public string DriverName { get; }

        /// <summary>
        /// Capabilities of the underlying ASIO driver (channel counts, buffer sizes, sample rate, channel info).
        /// </summary>
        public AsioDriverCapability Capabilities
        {
            get { ThrowIfDisposed(); return driver.Capabilities; }
        }

        /// <summary>
        /// Direct access to the underlying <see cref="AsioDriverExt"/>. Reserved for the legacy
        /// <see cref="AsioOut"/> facade so it can keep its raw <c>IntPtr</c>-based buffer-switch contract
        /// while delegating lifecycle and capability queries to this class.
        /// </summary>
        internal AsioDriverExt UnderlyingDriver => driver;

        /// <summary>
        /// Current lifecycle state of the device.
        /// </summary>
        public AsioDeviceState State => state;

        /// <summary>
        /// Sample rate the driver is currently running at, in Hz, as reported by the driver.
        /// </summary>
        public int CurrentSampleRate
        {
            get { ThrowIfDisposed(); return (int)driver.Capabilities.SampleRate; }
        }

        /// <summary>
        /// Returns <c>true</c> if the driver supports the given sample rate.
        /// </summary>
        public bool IsSampleRateSupported(int sampleRate)
        {
            ThrowIfDisposed();
            return driver.IsSampleRateSupported(sampleRate);
        }

        /// <summary>
        /// Shows the driver's native control panel.
        /// </summary>
        public void ShowControlPanel()
        {
            ThrowIfDisposed();
            driver.ShowControlPanel();
        }

        /// <summary>
        /// Enumerates the clock sources reported by the driver. Pro interfaces typically expose Internal
        /// alongside Word Clock, S/PDIF, AES/EBU, and ADAT sync inputs; consumer interfaces usually report
        /// a single Internal source. The entry whose <see cref="AsioClockSource.IsCurrentSource"/> is non-zero
        /// is the one the driver is currently locked to.
        /// </summary>
        public AsioClockSource[] GetClockSources()
        {
            ThrowIfDisposed();
            return driver.GetClockSources();
        }

        /// <summary>
        /// Selects the clock source the driver should lock to. Pass an <see cref="AsioClockSource.Index"/>
        /// reported by <see cref="GetClockSources"/>. The driver may respond by raising a reset request,
        /// which surfaces via <see cref="DriverResetRequest"/> — handle it with the standard
        /// <c>Stop</c> → <c>Reinitialize</c> → <c>Start</c> recovery pattern.
        /// </summary>
        public void SetClockSource(int reference)
        {
            ThrowIfDisposed();
            driver.SetClockSource(reference);
        }

        /// <summary>
        /// Number of audio frames per ASIO buffer for the current configuration. Valid after any successful <c>Init*</c> call.
        /// </summary>
        public int FramesPerBuffer
        {
            get
            {
                if (state == AsioDeviceState.Unconfigured || state == AsioDeviceState.Disposed)
                    throw new InvalidOperationException("FramesPerBuffer is only available after Init*.");
                return framesPerBuffer;
            }
        }

        /// <summary>
        /// Input latency in frames, reported by the driver.
        /// </summary>
        public int InputLatencySamples
        {
            get { ThrowIfDisposed(); driver.Driver.GetLatencies(out int input, out _); return input; }
        }

        /// <summary>
        /// Output latency in frames, reported by the driver.
        /// </summary>
        public int OutputLatencySamples
        {
            get { ThrowIfDisposed(); driver.Driver.GetLatencies(out _, out int output); return output; }
        }

        /// <summary>
        /// Raised once, on the captured <see cref="SynchronizationContext"/>, when the device stops — whether because of
        /// a user <see cref="Stop"/>, end-of-stream with auto-stop, or an unrecoverable driver fault.
        /// Always dispatched off the ASIO callback thread, so handlers may safely <see cref="Dispose"/> the device.
        /// </summary>
        public event EventHandler<StoppedEventArgs> Stopped;

        /// <summary>
        /// Raised when the driver reports that its settings have changed (e.g. the user opened the control panel and
        /// altered the sample rate). The recommended response is <see cref="Stop"/> → <see cref="Reinitialize"/> → <see cref="Start"/>.
        /// Dispatched on the captured <see cref="SynchronizationContext"/>.
        /// </summary>
        public event EventHandler DriverResetRequest;

        /// <summary>
        /// Raised per ASIO buffer-switch while recording. Dispatched synchronously on the ASIO driver thread for latency reasons;
        /// user code inside the handler must not call <see cref="Stop"/>, <see cref="Dispose"/>, or <see cref="Reinitialize"/>.
        /// </summary>
        /// <remarks>
        /// Only fires when the device was configured via <see cref="InitRecording"/>. In duplex mode, use the
        /// <see cref="AsioProcessCallback"/> supplied in <see cref="AsioDuplexOptions"/> instead.
        /// </remarks>
        public event EventHandler<AsioAudioCapturedEventArgs> AudioCaptured;

        /// <summary>
        /// Configures the device for playback-only operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the device has already been configured.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or its <c>Source</c> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the channel count or indices are invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if the source format cannot be converted to the driver's native output format.</exception>
        public void InitPlayback(AsioPlaybackOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (options.Source is null) throw new ArgumentException($"{nameof(AsioPlaybackOptions)}.{nameof(options.Source)} is required.", nameof(options));
            ThrowIfDisposed();
            ThrowIfNotUnconfigured();

            var source = options.Source;
            int sourceChannels = source.WaveFormat.Channels;
            int[] outputChannels = options.OutputChannels ?? Enumerable.Range(0, sourceChannels).ToArray();
            if (outputChannels.Length != sourceChannels)
                throw new ArgumentException(
                    $"Source has {sourceChannels} channel(s) but OutputChannels has {outputChannels.Length} entries — they must match.",
                    nameof(options));
            ValidateChannelIndices(outputChannels, driver.Capabilities.NbOutputChannels, nameof(options) + "." + nameof(options.OutputChannels), isInput: false);

            int sampleRate = source.WaveFormat.SampleRate;
            if (!driver.IsSampleRateSupported(sampleRate))
                throw new ArgumentException($"Driver does not support sample rate {sampleRate} Hz.");

            // All outputs on an ASIO device usually share the same native type, so the first channel is representative.
            var nativeFormat = driver.Capabilities.OutputChannelInfos[0].type;
            AsioSampleConvertor.SampleConvertor convertor;
            try
            {
                convertor = AsioSampleConvertor.SelectSampleConvertor(source.WaveFormat, nativeFormat);
            }
            catch (ArgumentException ex)
            {
                // Phase 0 finding F3 — translate the internal rejection into a loud, config-time NotSupportedException.
                throw new NotSupportedException(
                    $"Cannot convert source format ({source.WaveFormat}) to native ASIO format {nativeFormat}.", ex);
            }
            if (convertor is null)
                throw new NotSupportedException(
                    $"Cannot convert source format ({source.WaveFormat}) to native ASIO format {nativeFormat}.");

            if ((int)driver.Capabilities.SampleRate != sampleRate)
                driver.SetSampleRate(sampleRate);

            int resultingBufferSize = options.BufferSize.HasValue
                ? driver.CreateBuffers(outputChannels.Length, 0, options.BufferSize.Value)
                : driver.CreateBuffers(outputChannels.Length, 0, false);
            driver.SetChannelMapping(outputChannels, Array.Empty<int>());

            framesPerBuffer = resultingBufferSize;
            playbackOutputChannelCount = outputChannels.Length;
            playbackSource = source;
            playbackConvertor = convertor;
            playbackStagingBuffer = new byte[resultingBufferSize * source.WaveFormat.BlockAlign];
            autoStopOnEndOfStream = options.AutoStopOnEndOfStream;

            driver.FillBufferCallback = OnBufferUpdatePlayback;
            state = AsioDeviceState.Configured;
            // Snapshot so a caller mutating their array post-Init can't desync the cached config from the live channel mapping.
            lastInitOptions = Snapshot(options);
        }

        /// <summary>
        /// Configures the device for recording-only operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the device has already been configured.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the channel indices are invalid or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown if the selected channels use a native format outside Int16LSB/Int24LSB/Int32LSB/Float32LSB, or mix formats.</exception>
        public void InitRecording(AsioRecordingOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (options.InputChannels is null || options.InputChannels.Length == 0)
                throw new ArgumentException($"{nameof(AsioRecordingOptions)}.{nameof(options.InputChannels)} is required and must contain at least one channel.", nameof(options));
            ThrowIfDisposed();
            ThrowIfNotUnconfigured();

            int[] inputChannels = options.InputChannels;
            ValidateChannelIndices(inputChannels, driver.Capabilities.NbInputChannels, nameof(options) + "." + nameof(options.InputChannels), isInput: true);

            // Phase 0 finding F3 — validate at configuration time that all selected inputs share a single supported native format.
            var nativeFormat = ValidateUniformInputFormat(inputChannels);
            inputConverter = AsioNativeToFloatConverter.Select(nativeFormat);

            int sampleRate = options.SampleRate ?? (int)driver.Capabilities.SampleRate;
            if (!driver.IsSampleRateSupported(sampleRate))
                throw new ArgumentException($"Driver does not support sample rate {sampleRate} Hz.");
            if ((int)driver.Capabilities.SampleRate != sampleRate)
                driver.SetSampleRate(sampleRate);

            int resultingBufferSize = options.BufferSize.HasValue
                ? driver.CreateBuffers(0, inputChannels.Length, options.BufferSize.Value)
                : driver.CreateBuffers(0, inputChannels.Length, false);
            driver.SetChannelMapping(Array.Empty<int>(), inputChannels);

            framesPerBuffer = resultingBufferSize;

            // Pre-allocate per-channel float buffers so the callback never allocates on the hot path.
            var floatBuffers = new float[inputChannels.Length][];
            for (int i = 0; i < inputChannels.Length; i++)
                floatBuffers[i] = new float[framesPerBuffer];

            recordingContext = new AsioCallbackContext
            {
                Frames = framesPerBuffer,
                SampleRate = sampleRate,
                InputChannelCount = inputChannels.Length,
                OutputChannelCount = 0,
                InputFormat = nativeFormat,
                InputFloatBuffers = floatBuffers,
                InputNativeBuffers = new IntPtr[inputChannels.Length],
                InputNativeBytesPerChannel = framesPerBuffer * AsioNativeToFloatConverter.BytesPerSample(nativeFormat),
                Valid = false
            };

            driver.FillBufferCallback = OnBufferUpdateRecording;
            state = AsioDeviceState.Configured;
            lastInitOptions = Snapshot(options);
        }

        /// <summary>
        /// Configures the device for duplex operation: a single user-supplied callback receives input and writes output
        /// in the same buffer-switch. Used for low-latency real-time DSP (passthrough monitoring, effects, level metering with playback).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the device has already been configured.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the input or output channel selection is invalid, or the processor is missing.</exception>
        /// <exception cref="NotSupportedException">Thrown if the selected channels use a native format outside Int16LSB/Int24LSB/Int32LSB/Float32LSB, or mix formats.</exception>
        public void InitDuplex(AsioDuplexOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (options.InputChannels is null || options.InputChannels.Length == 0)
                throw new ArgumentException($"{nameof(AsioDuplexOptions)}.{nameof(options.InputChannels)} is required and must contain at least one channel.", nameof(options));
            if (options.OutputChannels is null || options.OutputChannels.Length == 0)
                throw new ArgumentException($"{nameof(AsioDuplexOptions)}.{nameof(options.OutputChannels)} is required and must contain at least one channel.", nameof(options));
            if (options.Processor is null)
                throw new ArgumentException($"{nameof(AsioDuplexOptions)}.{nameof(options.Processor)} is required.", nameof(options));
            ThrowIfDisposed();
            ThrowIfNotUnconfigured();

            int[] inputChannels = options.InputChannels;
            int[] outputChannels = options.OutputChannels;
            ValidateChannelIndices(inputChannels, driver.Capabilities.NbInputChannels, nameof(options) + "." + nameof(options.InputChannels), isInput: true);
            ValidateChannelIndices(outputChannels, driver.Capabilities.NbOutputChannels, nameof(options) + "." + nameof(options.OutputChannels), isInput: false);

            // Phase 0 finding F3 — every selected channel on each side must share a single supported native format.
            var inputFormat = ValidateUniformInputFormat(inputChannels);
            var outputFormat = ValidateUniformOutputFormat(outputChannels);
            duplexInputConverter = AsioNativeToFloatConverter.Select(inputFormat);
            duplexOutputConverter = AsioFloatToNativeConverter.Select(outputFormat);

            int sampleRate = options.SampleRate ?? (int)driver.Capabilities.SampleRate;
            if (!driver.IsSampleRateSupported(sampleRate))
                throw new ArgumentException($"Driver does not support sample rate {sampleRate} Hz.");
            if ((int)driver.Capabilities.SampleRate != sampleRate)
                driver.SetSampleRate(sampleRate);

            int resultingBufferSize = options.BufferSize.HasValue
                ? driver.CreateBuffers(outputChannels.Length, inputChannels.Length, options.BufferSize.Value)
                : driver.CreateBuffers(outputChannels.Length, inputChannels.Length, false);
            driver.SetChannelMapping(outputChannels, inputChannels);

            framesPerBuffer = resultingBufferSize;

            // Pre-allocate per-channel float buffers so the callback is allocation-free on the hot path.
            var inputFloatBuffers = new float[inputChannels.Length][];
            for (int i = 0; i < inputChannels.Length; i++)
                inputFloatBuffers[i] = new float[framesPerBuffer];
            var outputFloatBuffers = new float[outputChannels.Length][];
            for (int i = 0; i < outputChannels.Length; i++)
                outputFloatBuffers[i] = new float[framesPerBuffer];

            duplexContext = new AsioCallbackContext
            {
                Frames = framesPerBuffer,
                SampleRate = sampleRate,
                InputChannelCount = inputChannels.Length,
                OutputChannelCount = outputChannels.Length,
                InputFormat = inputFormat,
                OutputFormat = outputFormat,
                InputFloatBuffers = inputFloatBuffers,
                OutputFloatBuffers = outputFloatBuffers,
                InputNativeBuffers = new IntPtr[inputChannels.Length],
                OutputNativeBuffers = new IntPtr[outputChannels.Length],
                InputNativeBytesPerChannel = framesPerBuffer * AsioNativeToFloatConverter.BytesPerSample(inputFormat),
                OutputNativeBytesPerChannel = framesPerBuffer * AsioNativeToFloatConverter.BytesPerSample(outputFormat),
                OutputRawAccessed = new bool[outputChannels.Length],
                Valid = false
            };

            duplexProcessor = options.Processor;
            driver.FillBufferCallback = OnBufferUpdateDuplex;
            state = AsioDeviceState.Configured;
            lastInitOptions = Snapshot(options);
        }

        /// <summary>
        /// Reserved for the legacy <see cref="AsioOut"/> facade. Registers a raw <c>IntPtr</c>-based fill callback
        /// that runs through this device's drain / dispose-disposing / F1-guard infrastructure, and transitions
        /// the state machine to <see cref="AsioDeviceState.Configured"/>. Buffer creation, channel-offset selection,
        /// and sample-rate negotiation remain the caller's responsibility (via <see cref="UnderlyingDriver"/>).
        /// </summary>
        internal void ConfigureLegacyRawCallback(AsioFillBufferCallback rawCallback)
        {
            if (rawCallback is null) throw new ArgumentNullException(nameof(rawCallback));
            ThrowIfDisposed();
            ThrowIfNotUnconfigured();
            legacyRawCallback = rawCallback;
            driver.FillBufferCallback = OnBufferUpdateLegacy;
            state = AsioDeviceState.Configured;
        }

        private AsioFillBufferCallback legacyRawCallback;

        private void OnBufferUpdateLegacy(IntPtr[] inputBuffers, IntPtr[] outputBuffers)
        {
            if (disposing) return;
            callbackIdle.Reset();
            try
            {
                callbackThreadId = Environment.CurrentManagedThreadId;
                legacyRawCallback?.Invoke(inputBuffers, outputBuffers);
            }
            catch (Exception ex)
            {
                if (!autoStopTriggered)
                {
                    autoStopTriggered = true;
                    var capturedEx = ex;
                    ThreadPool.QueueUserWorkItem(_ => StopInternal(capturedEx));
                }
            }
            finally
            {
                callbackIdle.Set();
            }
        }

        /// <summary>
        /// Re-applies the most recent <c>Init*</c> configuration. The canonical use is recovering from a
        /// <see cref="DriverResetRequest"/>: <c>Stop()</c> → <c>Reinitialize()</c> → <c>Start()</c>.
        /// </summary>
        /// <remarks>
        /// The driver buffers are released and re-created against the (possibly changed) driver state,
        /// but the underlying COM driver instance is reused — there is no need to re-open the device.
        /// For playback mode the source <see cref="IWaveProvider"/> resumes from its current position.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no prior <c>Init*</c> succeeded, or if called while the device is <see cref="AsioDeviceState.Running"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">Thrown if the device has been disposed.</exception>
        public void Reinitialize()
        {
            ThrowIfDisposed();
            if (lastInitOptions is null)
                throw new InvalidOperationException("Reinitialize requires a prior successful Init* call.");
            if (state == AsioDeviceState.Running)
                throw new InvalidOperationException("Reinitialize requires the device to be stopped first.");

            // Detach the current callback before tearing down buffers — driver may invoke the callback
            // one last time during DisposeBuffers on some implementations.
            driver.FillBufferCallback = null;
            driver.DisposeBuffers();

            ResetModeState();
            state = AsioDeviceState.Unconfigured;

            // Keep lastInitOptions intact across the replay: if the inner InitX throws (e.g., the driver briefly
            // reports an unsupported sample rate), the user can recover by calling Reinitialize() again. Each
            // InitX rewrites lastInitOptions with the same reference on success, so this is a no-op on the happy path.
            var opts = lastInitOptions;
            switch (opts)
            {
                case AsioPlaybackOptions p: InitPlayback(p); break;
                case AsioRecordingOptions r: InitRecording(r); break;
                case AsioDuplexOptions d: InitDuplex(d); break;
                default:
                    throw new InvalidOperationException($"Unrecognized cached options type {opts.GetType().Name}.");
            }
        }

        private void ResetModeState()
        {
            playbackSource = null;
            playbackConvertor = null;
            playbackStagingBuffer = null;
            playbackOutputChannelCount = 0;
            autoStopOnEndOfStream = false;

            recordingContext = null;
            inputConverter = null;

            duplexContext = null;
            duplexInputConverter = null;
            duplexOutputConverter = null;
            duplexProcessor = null;

            autoStopTriggered = false;
            framesPerBuffer = 0;
        }

        /// <summary>
        /// Starts the driver. The device must have been configured via one of the <c>Init*</c> methods.
        /// </summary>
        public void Start()
        {
            ThrowIfDisposed();
            lock (stateLock)
            {
                if (state != AsioDeviceState.Configured && state != AsioDeviceState.Stopped)
                    throw new InvalidOperationException($"Cannot Start from state {state}. Expected Configured or Stopped.");
                state = AsioDeviceState.Running;
            }
            autoStopTriggered = false;
            driver.Start();
        }

        /// <summary>
        /// Stops the driver. Safe to call from any thread other than the ASIO buffer-switch callback.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if called from inside an ASIO buffer-switch callback.</exception>
        public void Stop()
        {
            // Phase 0 finding F1 — calling driver.Stop() from inside the callback deadlocks waiting on ourselves.
            if (state == AsioDeviceState.Running && Environment.CurrentManagedThreadId == callbackThreadId)
                throw new InvalidOperationException(
                    "Cannot call Stop from inside an ASIO buffer-switch callback. Use AutoStopOnEndOfStream, or call Stop from another thread.");
            StopInternal(null);
        }

        private void StopInternal(Exception exception)
        {
            lock (stateLock)
            {
                if (state != AsioDeviceState.Running) return;
                state = AsioDeviceState.Stopped;
            }
            try { driver?.Stop(); }
            catch (Exception ex) { exception ??= ex; }
            RaiseStopped(exception);
        }

        /// <summary>
        /// Releases the underlying COM ASIO driver. After disposal the device cannot be reused.
        /// </summary>
        /// <remarks>
        /// Dispose synchronises with any in-flight buffer-switch callback (Phase 0 finding F2): the
        /// disposing flag forces new callbacks to short-circuit, <see cref="AsioDriverExt.Stop"/> waits
        /// for the in-flight callback to return per the ASIO contract, and a final wait on the idle
        /// signal acts as a safety net for drivers that don't fully honour that contract.
        /// </remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (driver is null) return;
            try
            {
                disposing = true;
                if (state == AsioDeviceState.Running)
                {
                    try { StopInternal(null); } catch { /* best effort */ }
                }
                // Belt-and-braces: wait up to 1 second for any callback that snuck through to finish.
                // After this, the driver promises no more callbacks; releasing the driver is safe.
                callbackIdle.Wait(TimeSpan.FromSeconds(1));
                driver.ResetRequestCallback = null;
                driver.ReleaseDriver();
            }
            finally
            {
                callbackIdle.Dispose();
                driver = null;
                state = AsioDeviceState.Disposed;
            }
        }

        /// <summary>
        /// Safety net for callers that forget to <see cref="Dispose"/>. Releases only the unmanaged COM
        /// driver — managed objects (callbackIdle, syncContext, etc.) may already have been finalized,
        /// so they are not touched. Always prefer explicit <c>Dispose</c> / <c>using</c>.
        /// </summary>
        ~AsioDevice()
        {
            try { driver?.ReleaseDriver(); } catch { /* finalizer must not throw */ }
        }

        private void OnBufferUpdateRecording(IntPtr[] inputBuffers, IntPtr[] outputBuffers)
        {
            if (disposing) return;
            callbackIdle.Reset();
            try
            {
                callbackThreadId = Environment.CurrentManagedThreadId;
                var ctx = recordingContext;
                var timing = driver.LatestTimingInfo;
                ctx.SamplePosition = timing.SamplePosition;
                ctx.SystemTimeNanoseconds = timing.SystemTimeNanoseconds;
                ctx.Speed = timing.Speed;
                ctx.TimeCode = timing.TimeCode;
                for (int i = 0; i < ctx.InputChannelCount; i++)
                {
                    ctx.InputNativeBuffers[i] = inputBuffers[i];
                    inputConverter(inputBuffers[i], ctx.InputFloatBuffers[i].AsSpan(0, ctx.Frames), ctx.Frames);
                }
                ctx.Valid = true;
                try
                {
                    RaiseAudioCaptured(ctx);
                }
                finally
                {
                    ctx.Valid = false;
                }
            }
            catch (Exception ex)
            {
                if (!autoStopTriggered)
                {
                    autoStopTriggered = true;
                    var capturedEx = ex;
                    ThreadPool.QueueUserWorkItem(_ => StopInternal(capturedEx));
                }
            }
            finally
            {
                callbackIdle.Set();
            }
        }

        private void OnBufferUpdateDuplex(IntPtr[] inputBuffers, IntPtr[] outputBuffers)
        {
            if (disposing) return;
            callbackIdle.Reset();
            try
            {
                callbackThreadId = Environment.CurrentManagedThreadId;
                var ctx = duplexContext;
                var timing = driver.LatestTimingInfo;
                ctx.SamplePosition = timing.SamplePosition;
                ctx.SystemTimeNanoseconds = timing.SystemTimeNanoseconds;
                ctx.Speed = timing.Speed;
                ctx.TimeCode = timing.TimeCode;

                // Snapshot driver pointers and convert native input → library float buffer for each selected input channel.
                for (int i = 0; i < ctx.InputChannelCount; i++)
                {
                    ctx.InputNativeBuffers[i] = inputBuffers[i];
                    duplexInputConverter(inputBuffers[i], ctx.InputFloatBuffers[i].AsSpan(0, ctx.Frames), ctx.Frames);
                }

                // Reset the per-callback output state: zero the float staging buffers so unwritten outputs are silent,
                // capture native pointers, and clear the raw-access flags.
                for (int i = 0; i < ctx.OutputChannelCount; i++)
                {
                    ctx.OutputNativeBuffers[i] = outputBuffers[i];
                    Array.Clear(ctx.OutputFloatBuffers[i], 0, ctx.Frames);
                    ctx.OutputRawAccessed[i] = false;
                }

                ctx.Valid = true;
                try
                {
                    var buffers = new AsioProcessBuffers(ctx);
                    duplexProcessor(in buffers);
                }
                finally
                {
                    ctx.Valid = false;
                }

                // Convert the library float buffers → native for every channel the processor did not handle via RawOutput.
                for (int i = 0; i < ctx.OutputChannelCount; i++)
                {
                    if (ctx.OutputRawAccessed[i]) continue;
                    duplexOutputConverter(ctx.OutputFloatBuffers[i].AsSpan(0, ctx.Frames), ctx.OutputNativeBuffers[i], ctx.Frames);
                }
            }
            catch (Exception ex)
            {
                if (!autoStopTriggered)
                {
                    autoStopTriggered = true;
                    var capturedEx = ex;
                    ThreadPool.QueueUserWorkItem(_ => StopInternal(capturedEx));
                }
            }
            finally
            {
                callbackIdle.Set();
            }
        }

        private void OnBufferUpdatePlayback(IntPtr[] inputBuffers, IntPtr[] outputBuffers)
        {
            if (disposing) return;
            callbackIdle.Reset();
            try
            {
                callbackThreadId = Environment.CurrentManagedThreadId;
                int read = playbackSource.Read(playbackStagingBuffer.AsSpan());
                if (read < playbackStagingBuffer.Length)
                    Array.Clear(playbackStagingBuffer, read, playbackStagingBuffer.Length - read);

                unsafe
                {
                    fixed (byte* pBuffer = playbackStagingBuffer)
                    {
                        playbackConvertor(new IntPtr(pBuffer), outputBuffers, playbackOutputChannelCount, framesPerBuffer);
                    }
                }

                if (read == 0 && autoStopOnEndOfStream && !autoStopTriggered)
                {
                    autoStopTriggered = true;
                    // Phase 0 finding F1 — never call Stop() from the callback thread; defer.
                    ThreadPool.QueueUserWorkItem(_ => StopInternal(null));
                }
            }
            catch (Exception ex)
            {
                if (!autoStopTriggered)
                {
                    autoStopTriggered = true;
                    var capturedEx = ex;
                    ThreadPool.QueueUserWorkItem(_ => StopInternal(capturedEx));
                }
            }
            finally
            {
                callbackIdle.Set();
            }
        }

        private void ThrowIfNotUnconfigured()
        {
            if (state != AsioDeviceState.Unconfigured)
                throw new InvalidOperationException($"AsioDevice has already been configured (state = {state}). Dispose and create a new instance to reconfigure.");
        }

        private void ThrowIfDisposed()
        {
            if (state == AsioDeviceState.Disposed || driver is null)
                throw new ObjectDisposedException(nameof(AsioDevice));
        }

        private AsioSampleType ValidateUniformInputFormat(int[] inputChannels)
            => ValidateUniformChannelFormat(inputChannels, driver.Capabilities.InputChannelInfos, isInput: true);

        private AsioSampleType ValidateUniformOutputFormat(int[] outputChannels)
            => ValidateUniformChannelFormat(outputChannels, driver.Capabilities.OutputChannelInfos, isInput: false);

        // Pure helper extracted for unit-testability: takes the channel info array directly so tests don't
        // need a real or faked AsioDriverExt to exercise the mixed-format detection.
        internal static AsioSampleType ValidateUniformChannelFormat(int[] selected, AsioChannelInfo[] channelInfos, bool isInput)
        {
            var format = channelInfos[selected[0]].type;
            for (int i = 1; i < selected.Length; i++)
            {
                var other = channelInfos[selected[i]].type;
                if (other != format)
                    throw new NotSupportedException(
                        $"Selected {(isInput ? "input" : "output")} channels have mixed native formats (channel {selected[0]} is {format}, channel {selected[i]} is {other}). " +
                        "Mixed sample types across selected channels are not supported.");
            }
            return format;
        }

        // Snapshot helpers — clone the user-mutable channel arrays so a caller modifying their array
        // after Init can't desync the cached Reinitialize() config from the live channel mapping.
        private static AsioPlaybackOptions Snapshot(AsioPlaybackOptions o) => new()
        {
            Source = o.Source,
            OutputChannels = o.OutputChannels is null ? null : (int[])o.OutputChannels.Clone(),
            BufferSize = o.BufferSize,
            AutoStopOnEndOfStream = o.AutoStopOnEndOfStream,
        };

        private static AsioRecordingOptions Snapshot(AsioRecordingOptions o) => new()
        {
            InputChannels = (int[])o.InputChannels.Clone(),
            SampleRate = o.SampleRate,
            BufferSize = o.BufferSize,
        };

        private static AsioDuplexOptions Snapshot(AsioDuplexOptions o) => new()
        {
            InputChannels = (int[])o.InputChannels.Clone(),
            OutputChannels = (int[])o.OutputChannels.Clone(),
            SampleRate = o.SampleRate,
            BufferSize = o.BufferSize,
            Processor = o.Processor,
        };

        // Internal for unit-testability — pure data validation, no driver state.
        internal static void ValidateChannelIndices(int[] indices, int maxChannels, string paramName, bool isInput)
        {
            if (indices.Length == 0)
                throw new ArgumentException($"{paramName} must contain at least one channel index.", paramName);
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0 || indices[i] >= maxChannels)
                    throw new ArgumentOutOfRangeException(paramName,
                        $"{(isInput ? "Input" : "Output")} channel index {indices[i]} at position {i} is out of range [0, {maxChannels - 1}].");
                for (int j = 0; j < i; j++)
                {
                    if (indices[j] == indices[i])
                        throw new ArgumentException($"{paramName} contains duplicate channel index {indices[i]}.", paramName);
                }
            }
        }

        private void OnDriverResetRequest()
        {
            var handler = DriverResetRequest;
            if (handler is null) return;
            if (syncContext is null)
                ThreadPool.QueueUserWorkItem(_ => handler(this, EventArgs.Empty));
            else
                syncContext.Post(_ => handler(this, EventArgs.Empty), null);
        }

        internal void RaiseStopped(Exception exception)
        {
            var handler = Stopped;
            if (handler is null) return;
            var args = new StoppedEventArgs(exception);
            if (syncContext is null)
                ThreadPool.QueueUserWorkItem(_ => handler(this, args));
            else
                syncContext.Post(_ => handler(this, args), null);
        }

        internal void RaiseAudioCaptured(AsioCallbackContext context)
        {
            // Synchronous dispatch on the ASIO callback thread — see XML comments on AudioCaptured.
            AudioCaptured?.Invoke(this, new AsioAudioCapturedEventArgs(context));
        }
    }
}
