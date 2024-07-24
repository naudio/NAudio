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
        private int outputChannelOffset;
        private int inputChannelOffset;
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
        /// Creates the buffers for playing.
        /// </summary>
        /// <param name="numberOfOutputChannels"></param>
        /// <param name="numberOfInputChannels"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public int CreateBuffers(int numberOfOutputChannels, int numberOfInputChannels, int bufferSize = -1)
        {
            if (numberOfOutputChannels < 0 || numberOfOutputChannels > capability.NbOutputChannels)
            {
                throw new ArgumentException(
                    $"Invalid number of channels {numberOfOutputChannels}, must be in the range [0,{capability.NbOutputChannels}]");
            }
            if (numberOfInputChannels < 0 || numberOfInputChannels > capability.NbInputChannels)
            {
                throw new ArgumentException("numberOfInputChannels",
                    $"Invalid number of input channels {numberOfInputChannels}, must be in the range [0,{capability.NbInputChannels}]");
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

            bool bufferSizeWentWrong = false;
            try
            {
                bufferSize = EnsureBufferSize(bufferSize);
                CreateAsioBuffer(nbTotalChannels, bufferSize);
            }
            catch
            {
                bufferSizeWentWrong = true;
            }

            if (bufferSizeWentWrong)
            {
                bufferSize = capability.BufferPreferredSize;
                CreateAsioBuffer(nbTotalChannels, bufferSize);
            }

            // Check if outputReady is supported
            isOutputReadySupported = (driver.OutputReady() == AsioError.ASE_OK);
            return bufferSize;
        }

        private void CreateAsioBuffer(int nbTotalChannels, int bufferSize)
        {
            unsafe
            {
                fixed (AsioBufferInfo* infos = &bufferInfos[0])
                {
                    IntPtr pOutputBufferInfos = new IntPtr(infos);

                    // Create the ASIO Buffers with the callbacks
                    driver.CreateBuffers(pOutputBufferInfos, nbTotalChannels, bufferSize, ref callbacks);
                }
            }
        }

        private int EnsureBufferSize(int bufferSize)
        {
            // If a preference was not submitted than go for the preferred size
            if (bufferSize == -1)
                bufferSize = capability.BufferPreferredSize;
            else if (bufferSize < capability.BufferMinSize)
                bufferSize = capability.BufferMinSize;
            else if (bufferSize > capability.BufferMaxSize)
                bufferSize = capability.BufferMaxSize;
            else
            {
                /* BUFFER GRANULARITY
                 * Calculating a supported buffer size can be made by starting from the minimum supported
                 * size (capability.BufferMinSize) and adding each time the value of granularity (capability.BufferGranularity).
                 * With a minimum of 8 and a granularity of 16 we got this possible values:
                 *      8, 24, 40, 56, 72, 88, 104, etc.. until max size is reached (capability.BufferMaxSize)
                 */

                // If the requested buffer size is not supported by granularity we choose the closest value supported
                if ((bufferSize - capability.BufferMinSize) % capability.BufferGranularity != 0)
                {
                    int prevBS = -1,
                        nextBs = -1;
                    for (int bs = capability.BufferMinSize; bs < capability.BufferMaxSize; bs += capability.BufferGranularity)
                        if (bs > bufferSize)
                        {
                            nextBs = bs;
                            prevBS = bs - capability.BufferGranularity;
                            break;
                        }
                    if (prevBS == -1 && nextBs == -1)
                        bufferSize = capability.BufferPreferredSize;
                    else if (prevBS == -1 && nextBs > 0)
                        bufferSize = nextBs;
                    else if (prevBS > 0 && nextBs == -1)
                        bufferSize = prevBS;
                    else
                    {
                        int diffPrev = bufferSize - prevBS,
                            diffNext = nextBs - bufferSize;
                        if (diffPrev == diffNext)
                            bufferSize = nextBs;
                        else if (diffPrev < diffNext)
                            bufferSize = diffPrev;
                        else if (diffPrev > diffNext)
                            bufferSize = diffNext;
                    }
                }
            }
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
        /// Gets the size of the buffer.
        /// </summary>
        /// <param name="minSize">Size of the min.</param>
        /// <param name="maxSize">Size of the max.</param>
        /// <param name="preferredSize">Size of the preferred.</param>
        /// <param name="granularity">The granularity.</param>
        public void GetBufferSize(out int minSize, out int maxSize, out int preferredSize, out int granularity)
        {
            driver.GetBufferSize(out minSize, out maxSize, out preferredSize, out granularity);
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
                currentInputBuffers[i] = bufferInfos[i + inputChannelOffset].Buffer(doubleBufferIndex);
            }

            for (int i = 0; i < numberOfOutputChannels; i++)
            {
                currentOutputBuffers[i] = bufferInfos[i + outputChannelOffset + capability.NbInputChannels].Buffer(doubleBufferIndex);
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
