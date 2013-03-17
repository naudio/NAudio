using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Callback used by the ASIODriverExt to get wave data
    /// </summary>
    internal delegate void ASIOFillBufferCallback(IntPtr[] inputChannels, IntPtr[] outputChannels);

    /// <summary>
    /// ASIODriverExt is a simplified version of the ASIODriver. It provides an easier
    /// way to access the capabilities of the Driver and implement the callbacks necessary 
    /// for feeding the driver.
    /// Implementation inspired from Rob Philpot's with a managed C++ ASIO wrapper BlueWave.Interop.Asio
    /// http://www.codeproject.com/KB/mcpp/Asio.Net.aspx
    /// 
    /// Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    internal class ASIODriverExt
    {
        private ASIODriver driver;
        private ASIOCallbacks callbacks;
        private AsioDriverCapability capability;
        private ASIOBufferInfo[] bufferInfos;
        private bool isOutputReadySupported;
        private IntPtr[] currentOutputBuffers;
        private IntPtr[] currentInputBuffers;
        private int numberOfOutputChannels;
        private int numberOfInputChannels;
        private ASIOFillBufferCallback fillBufferCallback;
        private int bufferSize;
        private int outputChannelOffset;
        private int inputChannelOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="ASIODriverExt"/> class based on an already
        /// instantiated ASIODriver instance.
        /// </summary>
        /// <param name="driver">A ASIODriver already instantiated.</param>
        public ASIODriverExt(ASIODriver driver)
        {
            this.driver = driver;

            if (!driver.init(IntPtr.Zero))
            {
                throw new InvalidOperationException(driver.getErrorMessage());
            }

            callbacks = new ASIOCallbacks();
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
        public ASIODriver Driver
        {
            get { return driver; }
        }

        /// <summary>
        /// Starts playing the buffers.
        /// </summary>
        public void Start()
        {
            driver.start();
        }

        /// <summary>
        /// Stops playing the buffers.
        /// </summary>
        public void Stop()
        {
            driver.stop();
        }

        /// <summary>
        /// Shows the control panel.
        /// </summary>
        public void ShowControlPanel()
        {
            driver.controlPanel();
        }

        /// <summary>
        /// Releases this instance.
        /// </summary>
        public void ReleaseDriver()
        {
            try
            {
                driver.disposeBuffers();
            } catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
            }
            driver.ReleaseComASIODriver();
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
            return driver.canSampleRate(sampleRate);
        }

        /// <summary>
        /// Sets the sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        public void SetSampleRate(double sampleRate)
        {
            driver.setSampleRate(sampleRate);
            // Update Capabilities
            BuildCapabilities();
        }

        /// <summary>
        /// Gets or sets the fill buffer callback.
        /// </summary>
        /// <value>The fill buffer callback.</value>
        public ASIOFillBufferCallback FillBufferCallback
        {
            get { return fillBufferCallback; }
            set { fillBufferCallback = value; }
        }

        /// <summary>
        /// Gets the capabilities of the ASIODriver.
        /// </summary>
        /// <value>The capabilities.</value>
        public AsioDriverCapability Capabilities
        {
            get { return capability; }
        }

        /// <summary>
        /// Creates the buffers for playing.
        /// </summary>
        /// <param name="numberOfOutputChannels">The number of outputs channels.</param>
        /// <param name="numberOfInputChannels">The number of input channel.</param>
        /// <param name="useMaxBufferSize">if set to <c>true</c> [use max buffer size] else use Prefered size</param>
        public int CreateBuffers(int numberOfOutputChannels, int numberOfInputChannels, bool useMaxBufferSize)
        {
            if (numberOfOutputChannels < 0 || numberOfOutputChannels > capability.NbOutputChannels)
            {
                throw new ArgumentException(String.Format(
                                                "Invalid number of channels {0}, must be in the range [0,{1}]",
                                                numberOfOutputChannels, capability.NbOutputChannels));
            }
            if (numberOfInputChannels < 0 || numberOfInputChannels > capability.NbInputChannels)
            {
                throw new ArgumentException("numberOfInputChannels", 
                    String.Format("Invalid number of input channels {0}, must be in the range [0,{1}]",
                        numberOfInputChannels, capability.NbInputChannels));
            }

            // each channel needs a buffer info
            this.numberOfOutputChannels = numberOfOutputChannels;
            this.numberOfInputChannels = numberOfInputChannels;
            // Ask for maximum of output channels even if we use only the nbOutputChannelsArg
            int nbTotalChannels = capability.NbInputChannels + capability.NbOutputChannels;
            bufferInfos = new ASIOBufferInfo[nbTotalChannels];
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

            if (useMaxBufferSize)
            {
                // use the drivers maximum buffer size
                bufferSize = capability.BufferMaxSize;
            }
            else
            {
                // use the drivers preferred buffer size
                bufferSize = capability.BufferPreferredSize;
            }

            unsafe
            {
                fixed (ASIOBufferInfo* infos = &bufferInfos[0])
                {
                    IntPtr pOutputBufferInfos = new IntPtr(infos);

                    // Create the ASIO Buffers with the callbacks
                    driver.createBuffers(pOutputBufferInfos, nbTotalChannels, bufferSize, ref callbacks);
                }
            }

            // Check if outputReady is supported
            isOutputReadySupported = (driver.outputReady() == ASIOError.ASE_OK);
            return bufferSize;
        }

        /// <summary>
        /// Builds the capabilities internally.
        /// </summary>
        private void BuildCapabilities()
        {
            capability = new AsioDriverCapability();

            capability.DriverName = driver.getDriverName();

            // Get nb Input/Output channels
            driver.getChannels(out capability.NbInputChannels, out capability.NbOutputChannels);

            capability.InputChannelInfos = new ASIOChannelInfo[capability.NbInputChannels];
            capability.OutputChannelInfos = new ASIOChannelInfo[capability.NbOutputChannels];

            // Get ChannelInfo for Inputs
            for (int i = 0; i < capability.NbInputChannels; i++)
            {
                capability.InputChannelInfos[i] = driver.getChannelInfo(i, true);
            }

            // Get ChannelInfo for Output
            for (int i = 0; i < capability.NbOutputChannels; i++)
            {
                capability.OutputChannelInfos[i] = driver.getChannelInfo(i, false);
            }

            // Get the current SampleRate
            capability.SampleRate = driver.getSampleRate();

            var error = driver.GetLatencies(out capability.InputLatency, out capability.OutputLatency);
            // focusrite scarlett 2i4 returns ASE_NotPresent here

            if (error != ASIOError.ASE_OK && error != ASIOError.ASE_NotPresent)
            {
                var ex = new ASIOException("ASIOgetLatencies");
                ex.Error = error;
                throw ex;
            }

            // Get BufferSize
            driver.getBufferSize(out capability.BufferMinSize, out capability.BufferMaxSize, out capability.BufferPreferredSize, out capability.BufferGranularity);
        }

        /// <summary>
        /// Callback called by the ASIODriver on fill buffer demand. Redirect call to external callback.
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

            if (fillBufferCallback != null)
            {
                fillBufferCallback(currentInputBuffers, currentOutputBuffers);
            }

            if (isOutputReadySupported)
            {
                driver.outputReady();
            }
        }

        /// <summary>
        /// Callback called by the ASIODriver on event "Samples rate changed".
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
        private int AsioMessageCallBack(ASIOMessageSelector selector, int value, IntPtr message, IntPtr opt)
        {
            // Check when this is called?
            switch (selector)
            {
                case ASIOMessageSelector.kAsioSelectorSupported:
                    ASIOMessageSelector subValue = (ASIOMessageSelector)Enum.ToObject(typeof(ASIOMessageSelector), value);
                    switch (subValue)
                    {
                        case ASIOMessageSelector.kAsioEngineVersion:
                            return 1;
                        case ASIOMessageSelector.kAsioResetRequest:
                            return 0;
                        case ASIOMessageSelector.kAsioBufferSizeChange:
                            return 0;
                        case ASIOMessageSelector.kAsioResyncRequest:
                            return 0;
                        case ASIOMessageSelector.kAsioLatenciesChanged:
                            return 0;
                        case ASIOMessageSelector.kAsioSupportsTimeInfo:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                        case ASIOMessageSelector.kAsioSupportsTimeCode:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                    }
                    break;
                case ASIOMessageSelector.kAsioEngineVersion:
                    return 2;
                case ASIOMessageSelector.kAsioResetRequest:
                    return 1;
                case ASIOMessageSelector.kAsioBufferSizeChange:
                    return 0;
                case ASIOMessageSelector.kAsioResyncRequest:
                    return 0;
                case ASIOMessageSelector.kAsioLatenciesChanged:
                    return 0;
                case ASIOMessageSelector.kAsioSupportsTimeInfo:
                    return 0;
                case ASIOMessageSelector.kAsioSupportsTimeCode:
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
