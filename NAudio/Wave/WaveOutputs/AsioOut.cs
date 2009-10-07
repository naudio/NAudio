using System;
using NAudio.Wave.Asio;

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
        ASIODriverExt driver;
        IWaveProvider sourceStream;
        private WaveFormat waveFormat;
        PlaybackState playbackState;
        private int nbSamples;
        private byte[] waveBuffer;
        private ASIOSampleConvertor.SampleConvertor convertor;

        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler PlaybackStopped;

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
            initFromName(driverName);
        }

        /// <summary>
        /// Opens an ASIO output device
        /// </summary>
        /// <param name="driverIndex">Device number (zero based)</param>
        public AsioOut(int driverIndex)
        {
            String[] names = GetDriverNames();
            if (names.Length == 0)
            {
                throw new ArgumentException("There is no ASIO Driver installed on your system");
            }
            if (driverIndex < 0 || driverIndex > names.Length)
            {
                throw new ArgumentException(String.Format("Invalid device number. Must be in the range [0,{0}]", names.Length));
            }
            initFromName(names[driverIndex]);
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
        /// 	<c>true</c> if ASIO is supported; otherwise, <c>false</c>.
        /// </returns>
        public static bool isSupported()
        {
            return GetDriverNames().Length > 0;
        }

        /// <summary>
        /// Inits the driver from the asio driver name.
        /// </summary>
        /// <param name="driverName">Name of the driver.</param>
        private void initFromName(String driverName)
        {
            // Get the basic driver
            ASIODriver basicDriver = ASIODriver.GetASIODriverByName(driverName);

            // Instantiate the extended driver
            driver = new ASIODriverExt(basicDriver);
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
        /// <param name="waveProvider"></param>
        public void Init(IWaveProvider waveProvider)
        {
            sourceStream = waveProvider;
            waveFormat = waveProvider.WaveFormat;

            // Select the correct sample convertor from WaveFormat -> ASIOFormat
            convertor = ASIOSampleConvertor.SelectSampleConvertor(waveFormat, driver.Capabilities.OutputChannelInfos[0].type);

            if (!driver.IsSampleRateSupported(waveFormat.SampleRate))
            {
                throw new ArgumentException("SampleRate is not supported. TODO, implement Resampler");
            }
            if (driver.Capabilities.SampleRate != waveFormat.SampleRate)
            {
                driver.SetSampleRate(waveFormat.SampleRate);
            }

            // Plug the callback
            driver.FillBufferCalback = driver_BufferUpdate;

            // Used Prefered size of ASIO Buffer
            nbSamples = driver.CreateBuffers(waveFormat.Channels, false);

            // make a buffer big enough to read enough from the sourceStream to fill the ASIO buffers
            waveBuffer = new byte[nbSamples * waveFormat.Channels * waveFormat.BitsPerSample / 8];
        }

        /// <summary>
        /// driver buffer update callback to fill the wave buffer.
        /// </summary>
        /// <param name="bufferChannels">The buffer channels.</param>
        void driver_BufferUpdate(IntPtr[] bufferChannels)
        {
            // AsioDriver driver = sender as AsioDriver;

            
            int read = sourceStream.Read(waveBuffer,0,waveBuffer.Length);
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
                    convertor(new IntPtr(pBuffer), bufferChannels, waveFormat.Channels, nbSamples);
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
        /// Sets the volume (1.0 is unity gain)
        /// </summary>
        public float Volume
        {
            get
            {
                return 1.0f;
            }
            set
            {
                if (value != 1.0f)
                    throw new InvalidOperationException();
            }
        }

        private void RaisePlaybackStopped()
        {
            if (PlaybackStopped != null)
            {
                PlaybackStopped(this, EventArgs.Empty);
            }
        }
    }
}
