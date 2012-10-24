using System;
using NAudio.Wave.Asio;
using System.Threading;
using NAudio.Wave.WaveOutputs;

namespace NAudio.Wave
{
    /// <summary>
    /// ASIO Out Player. New implementation using an internal C# binding.
    /// 
    /// This implementation is only supporting Short16Bit and Float32Bit formats and is optimized 
    /// for 2 outputs channels .
    /// SampleRate is supported only if ASIODriver is supporting it (TODO: Add a resampler otherwhise).
    ///     
    /// This implementation is probably the first ASIODriver binding fully implemented in C#!
    /// 
    /// Original Contributor: Mark Heath 
    /// New Contributor to C# binding : Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioOut : IWavePlayer
    {
        private ASIODriverExt driver;
        private IWaveProvider sourceStream;
        private PlaybackState playbackState;
        private int nbSamples;
        private byte[] waveBuffer;
        private ASIOSampleConvertor.SampleConvertor convertor;
        private string driverName;

        private SynchronizationContext syncContext;

        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// When recording, fires whenever recorded audio is available
        /// </summary>
        public event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsioOut"/> class with the first 
        /// available ASIO Driver.
        /// </summary>
        public AsioOut()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsioOut"/> class with the driver name.
        /// </summary>
        /// <param name="driverName">Name of the device.</param>
        public AsioOut(String driverName)
        {
            this.syncContext = SynchronizationContext.Current;
            InitFromName(driverName);
        }

        /// <summary>
        /// Opens an ASIO output device
        /// </summary>
        /// <param name="driverIndex">Device number (zero based)</param>
        public AsioOut(int driverIndex)
        {
            this.syncContext = SynchronizationContext.Current; 
            String[] names = GetDriverNames();
            if (names.Length == 0)
            {
                throw new ArgumentException("There is no ASIO Driver installed on your system");
            }
            if (driverIndex < 0 || driverIndex > names.Length)
            {
                throw new ArgumentException(String.Format("Invalid device number. Must be in the range [0,{0}]", names.Length));
            }
            this.driverName = names[driverIndex];
            InitFromName(this.driverName);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="AsioOut"/> is reclaimed by garbage collection.
        /// </summary>
        ~AsioOut()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (driver != null)
            {
                if (playbackState != PlaybackState.Stopped)
                {
                    driver.Stop();
                }
                driver.ReleaseDriver();
                driver = null;
            }
        }

        /// <summary>
        /// Gets the names of the installed ASIO Driver.
        /// </summary>
        /// <returns>an array of driver names</returns>
        public static String[] GetDriverNames()
        {
            return ASIODriver.GetASIODriverNames();
        }

        /// <summary>
        /// Determines whether ASIO is supported.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if ASIO is supported; otherwise, <c>false</c>.
        /// </returns>
        public static bool isSupported()
        {
            return GetDriverNames().Length > 0;
        }

        /// <summary>
        /// Inits the driver from the asio driver name.
        /// </summary>
        /// <param name="driverName">Name of the driver.</param>
        private void InitFromName(String driverName)
        {
            // Get the basic driver
            ASIODriver basicDriver = ASIODriver.GetASIODriverByName(driverName);

            // Instantiate the extended driver
            driver = new ASIODriverExt(basicDriver);
            this.ChannelOffset = 0;
        }

        /// <summary>
        /// Shows the control panel
        /// </summary>
        public void ShowControlPanel()
        {
            driver.ShowControlPanel();
        }

        /// <summary>
        /// Starts playback
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                playbackState = PlaybackState.Playing;
                driver.Start();
            }
        }

        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            driver.Stop();
            RaisePlaybackStopped(null);
        }

        /// <summary>
        /// Pauses playback
        /// </summary>
        public void Pause()
        {
            playbackState = PlaybackState.Paused;
            driver.Stop();
        }

        /// <summary>
        /// Initialises to play
        /// </summary>
        /// <param name="waveProvider">Source wave provider</param>
        public void Init(IWaveProvider waveProvider)
        {
            this.InitRecordAndPlayback(waveProvider, 0, -1);
        }

        /// <summary>
        /// Initialises to play, with optional recording
        /// </summary>
        /// <param name="waveProvider">Source wave provider - set to null for record only</param>
        /// <param name="recordChannels">Number of channels to record</param>
        /// <param name="recordOnlySampleRate">Specify sample rate here if only recording, ignored otherwise</param>
        public void InitRecordAndPlayback(IWaveProvider waveProvider, int recordChannels, int recordOnlySampleRate)
        {
            if (this.sourceStream != null)
            {
                throw new InvalidOperationException("Already initialised this instance of AsioOut - dispose and create a new one");
            }
            int desiredSampleRate = waveProvider != null ? waveProvider.WaveFormat.SampleRate : recordOnlySampleRate;

            if (waveProvider != null)
            {
                sourceStream = waveProvider;

                this.NumberOfOutputChannels = waveProvider.WaveFormat.Channels;

                // Select the correct sample convertor from WaveFormat -> ASIOFormat
                convertor = ASIOSampleConvertor.SelectSampleConvertor(waveProvider.WaveFormat, driver.Capabilities.OutputChannelInfos[0].type);
            }
            else
            {
                this.NumberOfOutputChannels = 0;
            }


            if (!driver.IsSampleRateSupported(desiredSampleRate))
            {
                throw new ArgumentException("SampleRate is not supported");
            }
            if (driver.Capabilities.SampleRate != desiredSampleRate)
            {
                driver.SetSampleRate(desiredSampleRate);
            }

            // Plug the callback
            driver.FillBufferCallback = driver_BufferUpdate;

            this.NumberOfInputChannels = recordChannels;
            // Used Prefered size of ASIO Buffer
            nbSamples = driver.CreateBuffers(NumberOfOutputChannels, NumberOfInputChannels, false);
            driver.SetChannelOffset(ChannelOffset, InputChannelOffset); // will throw an exception if channel offset is too high

            if (waveProvider != null)
            {
                // make a buffer big enough to read enough from the sourceStream to fill the ASIO buffers
                waveBuffer = new byte[nbSamples * NumberOfOutputChannels * waveProvider.WaveFormat.BitsPerSample / 8];
            }
        }

        /// <summary>
        /// driver buffer update callback to fill the wave buffer.
        /// </summary>
        /// <param name="inputChannels">The input channels.</param>
        /// <param name="outputChannels">The output channels.</param>
        void driver_BufferUpdate(IntPtr[] inputChannels, IntPtr[] outputChannels)
        {
            if (this.NumberOfInputChannels > 0)
            {
                var audioAvailable = AudioAvailable;
                if (audioAvailable != null)
                {
                    audioAvailable(this, new AsioAudioAvailableEventArgs(inputChannels, nbSamples, driver.Capabilities.InputChannelInfos[0].type));
                }
            }

            if (this.NumberOfOutputChannels > 0)
            {
                int read = sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
                if (read < waveBuffer.Length)
                {
                    // we have stopped
                }

                // Call the convertor
                unsafe
                {
                    // TODO : check if it's better to lock the buffer at initialization?
                    fixed (void* pBuffer = &waveBuffer[0])
                    {
                        convertor(new IntPtr(pBuffer), outputChannels, NumberOfOutputChannels, nbSamples);
                    }
                }

                if (read == 0)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }

        /// <summary>
        /// Driver Name
        /// </summary>
        public string DriverName
        {
            get { return this.driverName; }
        }

        /// <summary>
        /// The number of output channels we are currently using for playback
        /// (Must be less than or equal to DriverOutputChannelCount)
        /// </summary>
        public int NumberOfOutputChannels { get; private set; }

        /// <summary>
        /// The number of input channels we are currently recording from
        /// (Must be less than or equal to DriverInputChannelCount)
        /// </summary>
        public int NumberOfInputChannels { get; private set; }

        /// <summary>
        /// The maximum number of input channels this ASIO driver supports
        /// </summary>
        public int DriverInputChannelCount { get { return driver.Capabilities.NbInputChannels; } }
        
        /// <summary>
        /// The maximum number of output channels this ASIO driver supports
        /// </summary>
        public int DriverOutputChannelCount { get { return driver.Capabilities.NbOutputChannels; } }

        /// <summary>
        /// By default the first channel on the input WaveProvider is sent to the first ASIO output.
        /// This option sends it to the specified channel number.
        /// Warning: make sure you don't set it higher than the number of available output channels - 
        /// the number of source channels.
        /// n.b. Future NAudio may modify this
        /// </summary>
        public int ChannelOffset { get; set; }

        /// <summary>
        /// Input channel offset (used when recording), allowing you to choose to record from just one
        /// specific input rather than them all
        /// </summary>
        public int InputChannelOffset { get; set; }

        /// <summary>
        /// Sets the volume (1.0 is unity gain)
        /// Not supported for ASIO Out. Set the volume on the input stream instead
        /// </summary>
        [Obsolete("this function will be removed in a future NAudio as ASIO does not support setting the volume on the device")]
        public float Volume
        {
            get
            {
                return 1.0f;
            }
            set
            {
                if (value != 1.0f)
                {
                    throw new InvalidOperationException("AsioOut does not support setting the device volume");
                }
            }
        }

        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (this.syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    this.syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }
    }

    /// <summary>
    /// Raised when ASIO data has been recorded.
    /// It is important to handle this as quickly as possible as it is in the buffer callback
    /// </summary>
    public class AsioAudioAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// Initialises a new instance of AsioAudioAvailableEventArgs
        /// </summary>
        /// <param name="inputBuffers">Pointers to the ASIO buffers for each channel</param>
        /// <param name="samplesPerBuffer">Number of samples in each buffer</param>
        /// <param name="asioSampleType">Audio format within each buffer</param>
        public AsioAudioAvailableEventArgs(IntPtr[] inputBuffers, int samplesPerBuffer, AsioSampleType asioSampleType)
        {
            this.InputBuffers = inputBuffers;
            this.SamplesPerBuffer = samplesPerBuffer;
            this.AsioSampleType = asioSampleType;
        }

        /// <summary>
        /// Pointer to a buffer per input channel
        /// </summary>
        public IntPtr[] InputBuffers { get; private set; }

        /// <summary>
        /// Number of samples in each buffer
        /// </summary>
        public int SamplesPerBuffer { get; private set; }

        /// <summary>
        /// Audio format within each buffer
        /// Most commonly this will be one of, Int32LSB, Int16LSB, Int24LSB or Float32LSB
        /// </summary>
        public AsioSampleType AsioSampleType { get; private set; }

        
        /// <summary>
        /// Converts all the recorded audio into a buffer of 32 bit floating point samples, interleaved by channel
        /// </summary>
        /// <returns>The samples as 32 bit floating point, interleaved</returns>
        public float[] GetAsInterleavedSamples()
        {
            int channels = InputBuffers.Length;
            float[] samples = new float[SamplesPerBuffer * channels];
            int index = 0;
            unsafe
            {
                if (AsioSampleType == Asio.AsioSampleType.Int32LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < channels; ch++)
                        {
                            samples[index++] = *((int*)InputBuffers[ch] + n) / (float)Int32.MaxValue;
                        }
                    }
                }
                else if (AsioSampleType == Asio.AsioSampleType.Int16LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < channels; ch++)
                        {
                            samples[index++] = *((short*)InputBuffers[ch] + n) / (float)Int16.MaxValue;
                        }
                    }
                }
                else if (AsioSampleType == Asio.AsioSampleType.Int24LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < channels; ch++)
                        {
                            byte *pSample = ((byte*)InputBuffers[ch] + n * 3);

                            //int sample = *pSample + *(pSample+1) << 8 + (sbyte)*(pSample+2) << 16;
                            int sample = pSample[0] | (pSample[1] << 8) | ((sbyte)pSample[2] << 16);
                            samples[index++] = sample / 8388608.0f;
                        }
                    }
                }
                else if (AsioSampleType == Asio.AsioSampleType.Float32LSB)
                {
                    for (int n = 0; n < SamplesPerBuffer; n++)
                    {
                        for (int ch = 0; ch < channels; ch++)
                        {
                            samples[index++] = *((float*)InputBuffers[ch] + n);
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException(String.Format("ASIO Sample Type {0} not supported", AsioSampleType));
                }
            }
            return samples;
        }
    }
}
