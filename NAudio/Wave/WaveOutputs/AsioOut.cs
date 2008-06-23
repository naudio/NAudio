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
        WaveStream sourceStream;
        private WaveFormat waveFormat;
        PlaybackState playbackState;
        private int nbSamples;
        private byte[] buffer;
        private SampleConvertor convertor;

        private delegate void SampleConvertor(IntPtr inputBuffer, IntPtr[] asioOutputBuffers);

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
        /// <param name="waveStream"></param>
        public void Init(WaveStream waveStream)
        {
            sourceStream = waveStream;
            waveFormat = waveStream.WaveFormat;

            // Select the correct sample convertor from WaveFormat -> ASIOFormat
            SelectConvertor();

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
            buffer = new byte[nbSamples * waveFormat.Channels * waveFormat.BitsPerSample / 8];
        }

        /// <summary>
        /// Selects the convertor from WaveFormat -> ASIOFormat.
        /// </summary>
        private void SelectConvertor()
        {
            bool is2Channels = waveFormat.Channels == 2;

            if (waveFormat.BitsPerSample != 16 && waveFormat.BitsPerSample != 32 )
            {
                throw new ArgumentException(String.Format("WaveFormat BitsPerSample {0} is not yet supported", waveFormat.BitsPerSample));
            }

            // TODO : IMPLEMENTS OTHER CONVERTOR TYPES
            switch (driver.Capabilities.OutputChannelInfos[0].type)
            {
                case ASIOSampleType.ASIOSTInt32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToInt2Channels : (SampleConvertor)ConvertorShortToIntGeneric;
                            break;
                        case 32:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToInt2Channels : (SampleConvertor)ConvertorFloatToIntGeneric;
                            break;
                    }
                    break;
                case ASIOSampleType.ASIOSTInt16LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToShort2Channels : (SampleConvertor)ConvertorShortToShortGeneric;
                            break;
                        case 32:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToShort2Channels : (SampleConvertor)ConvertorFloatToShortGeneric;
                            break;
                    }
                    break;
                default:
                    throw new ArgumentException(
                        String.Format("ASIO Buffer Type {0} is not yet supported. ASIO Int32 buffer is only supported.",
                                      Enum.GetName(typeof(ASIOSampleType), driver.Capabilities.OutputChannelInfos[0].type)));                
            }
        }

        /// <summary>
        /// driver buffer update callback to fill the wave buffer.
        /// </summary>
        /// <param name="bufferChannels">The buffer channels.</param>
        void driver_BufferUpdate(IntPtr[] bufferChannels)
        {
            // AsioDriver driver = sender as AsioDriver;
            int read = sourceStream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
            {
                // we have stopped
            }

            // Call the convertor
            unsafe
            {
                // TODO : check if it's better to lock the buffer at initialization?
                fixed (void* pBuffer = &buffer[0])
                {
                    convertor(new IntPtr(pBuffer), bufferChannels);
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

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            driver.Stop();
            driver.ReleaseDriver();
        }

        #region Sample Convertors from WaveFormat to ASIOFormat


        private static int clampToInt(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (int)(sampleValue * 2147483647.0);
        }

        private static short clampToShort(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (short)(sampleValue * 32767.0);
        }

        /// <summary>
        /// Optimized convertor for 2 channels SHORT
        /// </summary>
        private void ConvertorShortToInt2Channels(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                short* inputSamples = (short*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                leftSamples++;
                rightSamples++;
                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples = inputSamples[0];
                    *rightSamples = inputSamples[1];
                    // Go to next sample
                    inputSamples += 2;
                    // Add 4 Bytes
                    leftSamples += 2;
                    rightSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor for SHORT
        /// </summary>
        private void ConvertorShortToIntGeneric(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                int channels = waveFormat.Channels;
                short* inputSamples = (short*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[channels];
                for (int i = 0; i < channels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                    // Point to upper 16 bits of the 32Bits.
                    samples[i]++;
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < channels; j++)
                    {
                        *samples[i] = *inputSamples++;
                        samples[i] += 2;
                    }
                }
            }
        }


        /// <summary>
        /// Optimized convertor for 2 channels FLOAT
        /// </summary>
        private void ConvertorFloatToInt2Channels(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                float* inputSamples = (float*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                int* leftSamples = (int*)asioOutputBuffers[0];
                int* rightSamples = (int*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToInt(inputSamples[0]);
                    *rightSamples++ = clampToInt(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor SHORT
        /// </summary>
        private void ConvertorFloatToIntGeneric(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                int channels = waveFormat.Channels;
                float* inputSamples = (float*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                float*[] samples = new float*[channels];
                for (int i = 0; i < channels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < channels; j++)
                    {
                        *samples[i]++ = clampToInt(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Optimized convertor for 2 channels SHORT
        /// </summary>
        private void ConvertorShortToShort2Channels(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                short* inputSamples = (short*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = inputSamples[0];
                    *rightSamples++ = inputSamples[1];
                    // Go to next sample
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor for SHORT
        /// </summary>
        private void ConvertorShortToShortGeneric(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                int channels = waveFormat.Channels;
                short* inputSamples = (short*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[channels];

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < channels; j++)
                    {
                        *(samples[i]++) = *inputSamples++;
                    }
                }
            }
        }


        /// <summary>
        /// Optimized convertor for 2 channels FLOAT
        /// </summary>
        private void ConvertorFloatToShort2Channels(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                float* inputSamples = (float*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToShort(inputSamples[0]);
                    *rightSamples++ = clampToShort(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Generic convertor SHORT
        /// </summary>
        private void ConvertorFloatToShortGeneric(IntPtr inputBuffer, IntPtr[] asioOutputBuffers)
        {
            unsafe
            {
                int channels = waveFormat.Channels;
                float* inputSamples = (float*)inputBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[channels];
                for (int i = 0; i < channels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < channels; j++)
                    {
                        *(samples[i]++) = clampToShort(*inputSamples++);
                    }
                }
            }
        }

        #endregion
    }
}
