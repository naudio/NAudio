using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Callback used by the AsioDriverExt to get wave data
    /// </summary>
    public delegate void AsioFillBufferCallback(IntPtr[] inputChannels, IntPtr[] outputChannels);

    /// <summary>
    /// AsioDriverExt is a simplified version of the AsioDriver. It provides an easier
    /// way to access the capabilities of the Driver and implement the callbacks necessary 
    /// for feeding the driver.
    /// Implementation inspired from Rob Philpot's with a managed C++ ASIO wrapper BlueWave.Interop.Asio
    /// http://www.codeproject.com/KB/mcpp/Asio.Net.aspx
    /// 
    /// Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioDriverExt
    {
        private readonly AsioDriver driver;
        private AsioCallbacks callbacks;
        private AsioDriverCapability capability;
        private AsioBufferInfo[] bufferInfos;
        private bool isOutputReadySupported;
        private IntPtr[] currentOutputBuffers;
        private IntPtr[] currentInputBuffers;
        private int numberOfOutputChannels;
        private int numberOfInputChannels;
        private AsioFillBufferCallback fillBufferCallback;
        private int bufferSize;
        private int outputChannelOffset;
        private int inputChannelOffset;
        private int[] outputChannelIndices;
        private int[] inputChannelIndices;
        /// <summary>
        /// Reset Request Callback
        /// </summary>
        public Action ResetRequestCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsioDriverExt"/> class based on an already
        /// instantiated AsioDriver instance.
        /// </summary>
        /// <param name="driver">A AsioDriver already instantiated.</param>
        public AsioDriverExt(AsioDriver driver)
        {
            this.driver = driver;

            if (!driver.Init(IntPtr.Zero))
            {
                throw new InvalidOperationException(driver.GetErrorMessage());
            }

            callbacks = new AsioCallbacks();
            callbacks.pasioMessage = AsioMessageCallBack;
            callbacks.pbufferSwitch = BufferSwitchCallBack;
            callbacks.pbufferSwitchTimeInfo = BufferSwitchTimeInfoCallBack;
            callbacks.psampleRateDidChange = SampleRateDidChangeCallBack;

            BuildCapabilities();
        }

        /// <summary>
        /// Allows adjustment of which is the first output channel we write to
        /// </summary>
        /// <param name="outputChannelOffset">Output Channel offset</param>
        /// <param name="inputChannelOffset">Input Channel offset</param>
        public void SetChannelOffset(int outputChannelOffset, int inputChannelOffset)
        {
            if (outputChannelOffset + numberOfOutputChannels <= Capabilities.NbOutputChannels)
            {
                this.outputChannelOffset = outputChannelOffset;
            }
            else
            {
                throw new ArgumentException("Invalid channel offset");
            }
            if (inputChannelOffset + numberOfInputChannels <= Capabilities.NbInputChannels)
            {
                this.inputChannelOffset = inputChannelOffset;
            }
            else
            {
                throw new ArgumentException("Invalid channel offset");
            }

            // Propagate the contiguous range to the index arrays that the buffer-switch callback reads from.
            outputChannelIndices = new int[numberOfOutputChannels];
            inputChannelIndices = new int[numberOfInputChannels];
            for (int i = 0; i < numberOfOutputChannels; i++) outputChannelIndices[i] = outputChannelOffset + i;
            for (int i = 0; i < numberOfInputChannels; i++) inputChannelIndices[i] = inputChannelOffset + i;
        }

        /// <summary>
        /// Selects arbitrary (non-contiguous) physical channel indices for this session.
        /// Must be called after <see cref="CreateBuffers(int, int, bool)"/>. The number of entries in each array must match
        /// the channel counts passed to <c>CreateBuffers</c>.
        /// </summary>
        /// <param name="outputChannels">Physical output channel indices. Each must be in <c>[0, NbOutputChannels)</c>.</param>
        /// <param name="inputChannels">Physical input channel indices. Each must be in <c>[0, NbInputChannels)</c>.</param>
        public void SetChannelMapping(int[] outputChannels, int[] inputChannels)
        {
            outputChannels ??= Array.Empty<int>();
            inputChannels ??= Array.Empty<int>();
            if (outputChannels.Length != numberOfOutputChannels)
                throw new ArgumentException($"Expected {numberOfOutputChannels} output channel indices, got {outputChannels.Length}", nameof(outputChannels));
            if (inputChannels.Length != numberOfInputChannels)
                throw new ArgumentException($"Expected {numberOfInputChannels} input channel indices, got {inputChannels.Length}", nameof(inputChannels));
            for (int i = 0; i < outputChannels.Length; i++)
                if (outputChannels[i] < 0 || outputChannels[i] >= capability.NbOutputChannels)
                    throw new ArgumentOutOfRangeException(nameof(outputChannels), $"Output channel index {outputChannels[i]} out of range [0, {capability.NbOutputChannels - 1}]");
            for (int i = 0; i < inputChannels.Length; i++)
                if (inputChannels[i] < 0 || inputChannels[i] >= capability.NbInputChannels)
                    throw new ArgumentOutOfRangeException(nameof(inputChannels), $"Input channel index {inputChannels[i]} out of range [0, {capability.NbInputChannels - 1}]");

            outputChannelIndices = (int[])outputChannels.Clone();
            inputChannelIndices = (int[])inputChannels.Clone();
        }

        /// <summary>
        /// Gets the driver used.
        /// </summary>
        /// <value>The ASIOdriver.</value>
        public AsioDriver Driver => driver;

        /// <summary>
        /// Starts playing the buffers.
        /// </summary>
        public void Start()
        {
            driver.Start();
        }

        /// <summary>
        /// Stops playing the buffers.
        /// </summary>
        public void Stop()
        {
            driver.Stop();
        }

        /// <summary>
        /// Shows the control panel.
        /// </summary>
        public void ShowControlPanel()
        {
            driver.ControlPanel();
        }

        /// <summary>
        /// Disposes the driver-allocated buffers without releasing the underlying COM driver.
        /// Used by the <see cref="AsioDevice.Reinitialize"/> path so the same driver instance can be
        /// re-configured after a sample-rate or buffer-size change without a full <see cref="ReleaseDriver"/> cycle.
        /// </summary>
        public void DisposeBuffers()
        {
            driver.DisposeBuffers();
            // Re-read capabilities — buffer-size constraints are unchanged but sample rate may have changed under us.
            BuildCapabilities();
        }

        /// <summary>
        /// Releases this instance.
        /// </summary>
        public void ReleaseDriver()
        {
            try
            {
                driver.DisposeBuffers();
            } catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
            }
            driver.ReleaseComAsioDriver();
        }

        /// <summary>
        /// Determines whether the specified sample rate is supported.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        /// <returns>
        /// 	<c>true</c> if [is sample rate supported]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSampleRateSupported(double sampleRate)
        {
            return driver.CanSampleRate(sampleRate);
        }

        /// <summary>
        /// Sets the sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        public void SetSampleRate(double sampleRate)
        {
            driver.SetSampleRate(sampleRate);
            // Update Capabilities
            BuildCapabilities();
        }

        /// <summary>
        /// Gets or sets the fill buffer callback.
        /// </summary>
        /// <value>The fill buffer callback.</value>
        public AsioFillBufferCallback FillBufferCallback
        {
            get { return fillBufferCallback; }
            set { fillBufferCallback = value; }
        }

        /// <summary>
        /// Gets the capabilities of the AsioDriver.
        /// </summary>
        /// <value>The capabilities.</value>
        public AsioDriverCapability Capabilities => capability;

        /// <summary>
        /// Creates the buffers for playing, requesting a specific buffer size.
        /// </summary>
        /// <param name="numberOfOutputChannels">The number of outputs channels.</param>
        /// <param name="numberOfInputChannels">The number of input channel.</param>
        /// <param name="requestedBufferSize">Desired ASIO buffer size in frames. Must be within <see cref="AsioDriverCapability.BufferMinSize"/>..<see cref="AsioDriverCapability.BufferMaxSize"/> and respect <see cref="AsioDriverCapability.BufferGranularity"/>.</param>
        public int CreateBuffers(int numberOfOutputChannels, int numberOfInputChannels, int requestedBufferSize)
        {
            if (requestedBufferSize < capability.BufferMinSize || requestedBufferSize > capability.BufferMaxSize)
                throw new ArgumentOutOfRangeException(nameof(requestedBufferSize),
                    $"Requested buffer size {requestedBufferSize} is outside the driver's supported range [{capability.BufferMinSize}, {capability.BufferMaxSize}]");
            return CreateBuffersCore(numberOfOutputChannels, numberOfInputChannels, requestedBufferSize);
        }

        /// <summary>
        /// Creates the buffers for playing.
        /// </summary>
        /// <param name="numberOfOutputChannels">The number of outputs channels.</param>
        /// <param name="numberOfInputChannels">The number of input channel.</param>
        /// <param name="useMaxBufferSize">if set to <c>true</c> [use max buffer size] else use Prefered size</param>
        public int CreateBuffers(int numberOfOutputChannels, int numberOfInputChannels, bool useMaxBufferSize)
        {
            return CreateBuffersCore(numberOfOutputChannels, numberOfInputChannels,
                useMaxBufferSize ? capability.BufferMaxSize : capability.BufferPreferredSize);
        }

        private int CreateBuffersCore(int numberOfOutputChannels, int numberOfInputChannels, int requestedBufferSize)
        {
            if (numberOfOutputChannels < 0 || numberOfOutputChannels > capability.NbOutputChannels)
            {
                throw new ArgumentException(
                    $"Invalid number of channels {numberOfOutputChannels}, must be in the range [0,{capability.NbOutputChannels}]");
            }
            if (numberOfInputChannels < 0 || numberOfInputChannels > capability.NbInputChannels)
            {
                throw new ArgumentException(
                    $"Invalid number of input channels {numberOfInputChannels}, must be in the range [0,{capability.NbInputChannels}]",
                    nameof(numberOfInputChannels));
            }

            // each channel needs a buffer info
            this.numberOfOutputChannels = numberOfOutputChannels;
            this.numberOfInputChannels = numberOfInputChannels;
            // Ask for maximum of output channels even if we use only the nbOutputChannelsArg
            int nbTotalChannels = capability.NbInputChannels + capability.NbOutputChannels;
            bufferInfos = new AsioBufferInfo[nbTotalChannels];
            currentOutputBuffers = new IntPtr[numberOfOutputChannels];
            currentInputBuffers = new IntPtr[numberOfInputChannels];

            // and do the same for output channels
            // ONLY work on output channels (just put isInput = true for InputChannel)
            int totalIndex = 0;
            for (int index = 0; index < capability.NbInputChannels; index++, totalIndex++)
            {
                bufferInfos[totalIndex].isInput = true;
                bufferInfos[totalIndex].channelNum = index;
                bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            for (int index = 0; index < capability.NbOutputChannels; index++, totalIndex++)
            {
                bufferInfos[totalIndex].isInput = false;
                bufferInfos[totalIndex].channelNum = index;
                bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            bufferSize = requestedBufferSize;

            unsafe
            {
                fixed (AsioBufferInfo* infos = &bufferInfos[0])
                {
                    IntPtr pOutputBufferInfos = new IntPtr(infos);

                    // Create the ASIO Buffers with the callbacks
                    driver.CreateBuffers(pOutputBufferInfos, nbTotalChannels, bufferSize, ref callbacks);
                }
            }

            // Default to a contiguous mapping starting at channel 0 — callers replace this via
            // SetChannelOffset or SetChannelMapping to select a different physical range.
            outputChannelIndices = new int[numberOfOutputChannels];
            inputChannelIndices = new int[numberOfInputChannels];
            for (int i = 0; i < numberOfOutputChannels; i++) outputChannelIndices[i] = i;
            for (int i = 0; i < numberOfInputChannels; i++) inputChannelIndices[i] = i;

            // Check if outputReady is supported
            isOutputReadySupported = (driver.OutputReady() == AsioError.ASE_OK);
            return bufferSize;
        }

        /// <summary>
        /// Builds the capabilities internally.
        /// </summary>
        private void BuildCapabilities()
        {
            capability = new AsioDriverCapability();

            capability.DriverName = driver.GetDriverName();

            // Get nb Input/Output channels
            driver.GetChannels(out capability.NbInputChannels, out capability.NbOutputChannels);

            capability.InputChannelInfos = new AsioChannelInfo[capability.NbInputChannels];
            capability.OutputChannelInfos = new AsioChannelInfo[capability.NbOutputChannels];

            // Get ChannelInfo for Inputs
            for (int i = 0; i < capability.NbInputChannels; i++)
            {
                capability.InputChannelInfos[i] = driver.GetChannelInfo(i, true);
            }

            // Get ChannelInfo for Output
            for (int i = 0; i < capability.NbOutputChannels; i++)
            {
                capability.OutputChannelInfos[i] = driver.GetChannelInfo(i, false);
            }

            // Get the current SampleRate
            capability.SampleRate = driver.GetSampleRate();

            var error = driver.GetLatencies(out capability.InputLatency, out capability.OutputLatency);
            // focusrite scarlett 2i4 returns ASE_NotPresent here

            if (error != AsioError.ASE_OK && error != AsioError.ASE_NotPresent)
            {
                var ex = new AsioException("ASIOgetLatencies");
                ex.Error = error;
                throw ex;
            }

            // Get BufferSize
            driver.GetBufferSize(out capability.BufferMinSize, out capability.BufferMaxSize, out capability.BufferPreferredSize, out capability.BufferGranularity);
        }

        /// <summary>
        /// Callback called by the AsioDriver on fill buffer demand. Redirect call to external callback.
        /// </summary>
        /// <param name="doubleBufferIndex">Index of the double buffer.</param>
        /// <param name="directProcess">if set to <c>true</c> [direct process].</param>
        private void BufferSwitchCallBack(int doubleBufferIndex, bool directProcess)
        {
            for (int i = 0; i < numberOfInputChannels; i++)
            {
                currentInputBuffers[i] = bufferInfos[inputChannelIndices[i]].Buffer(doubleBufferIndex);
            }

            for (int i = 0; i < numberOfOutputChannels; i++)
            {
                currentOutputBuffers[i] = bufferInfos[outputChannelIndices[i] + capability.NbInputChannels].Buffer(doubleBufferIndex);
            }

            fillBufferCallback?.Invoke(currentInputBuffers, currentOutputBuffers);

            if (isOutputReadySupported)
            {
                driver.OutputReady();
            }
        }

        /// <summary>
        /// Callback called by the AsioDriver on event "Samples rate changed".
        /// </summary>
        /// <param name="sRate">The sample rate.</param>
        private void SampleRateDidChangeCallBack(double sRate)
        {
            // Check when this is called?
            capability.SampleRate = sRate;
        }

        /// <summary>
        /// Asio message call back.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="value">The value.</param>
        /// <param name="message">The message.</param>
        /// <param name="opt">The opt.</param>
        /// <returns></returns>
        private int AsioMessageCallBack(AsioMessageSelector selector, int value, IntPtr message, IntPtr opt)
        {
            // Check when this is called?
            switch (selector)
            {
                case AsioMessageSelector.kAsioSelectorSupported:
                    AsioMessageSelector subValue = (AsioMessageSelector)Enum.ToObject(typeof(AsioMessageSelector), value);
                    switch (subValue)
                    {
                        case AsioMessageSelector.kAsioEngineVersion:
                            return 1;
                        case AsioMessageSelector.kAsioResetRequest:
                            ResetRequestCallback?.Invoke();
                            return 0;
                        case AsioMessageSelector.kAsioBufferSizeChange:
                            return 0;
                        case AsioMessageSelector.kAsioResyncRequest:
                            return 0;
                        case AsioMessageSelector.kAsioLatenciesChanged:
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeInfo:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeCode:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                    }
                    break;
                case AsioMessageSelector.kAsioEngineVersion:
                    return 2;
                case AsioMessageSelector.kAsioResetRequest:
                    ResetRequestCallback?.Invoke();
                    return 1;
                case AsioMessageSelector.kAsioBufferSizeChange:
                    return 0;
                case AsioMessageSelector.kAsioResyncRequest:
                    return 0;
                case AsioMessageSelector.kAsioLatenciesChanged:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeInfo:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeCode:
                    return 0;
            }
            return 0;
        }

        /// <summary>
        /// Buffers switch time info call back.
        /// </summary>
        /// <param name="asioTimeParam">The asio time param.</param>
        /// <param name="doubleBufferIndex">Index of the double buffer.</param>
        /// <param name="directProcess">if set to <c>true</c> [direct process].</param>
        /// <returns></returns>
        private IntPtr BufferSwitchTimeInfoCallBack(IntPtr asioTimeParam, int doubleBufferIndex, bool directProcess)
        {
            // Check when this is called?
            return IntPtr.Zero;
        }
    }
}
