using System;
using System.Collections.Generic;
using System.Text;
using BlueWave.Interop.Asio;
using System.Diagnostics;

namespace NAudio.Wave
{
    /// <summary>
    /// Make's use of Rob Philpot's managed ASIO wrapper
    /// http://www.codeproject.com/KB/mcpp/Asio.Net.aspx
    /// </summary>
    public class AsioOut : IWavePlayer
    {
        AsioDriver driver;
        WaveStream sourceStream;
        bool playing;
        bool paused;
        byte[] buffer;

        /// <summary>
        /// Opens an ASIO output device
        /// </summary>
        /// <param name="device">Device number (zero based)</param>
        public AsioOut(int device)
        {
            driver = AsioDriver.SelectDriver(
                AsioDriver.InstalledDrivers[device]);            
            driver.CreateBuffers();
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
            playing = true;
            driver.Start();
        }

        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop()
        {
            playing = false;
            driver.Stop();
        }

        /// <summary>
        /// Pauses playback
        /// </summary>
        public void Pause()
        {
            paused = true;
            driver.Stop();
        }

        /// <summary>
        /// Resumes playback
        /// </summary>
        public void Resume()
        {
            paused = false;
            driver.Start();
        }

        /// <summary>
        /// Initialises to play
        /// </summary>
        /// <param name="waveStream"></param>
        public void Init(WaveStream waveStream)
        {
            sourceStream = waveStream;
            Debug.Assert(driver.SampleRate == waveStream.WaveFormat.SampleRate,"Sample rates must match");            
            driver.BufferUpdate += new EventHandler(driver_BufferUpdate);
            // make a buffer big enough to read enough from the sourceStream to fill the ASIO buffers
            buffer = new byte[driver.OutputChannels[0].BufferSize * 
                sourceStream.WaveFormat.Channels * 
                sourceStream.WaveFormat.BitsPerSample / 8];

        }

        void  driver_BufferUpdate(object sender, EventArgs e)
        {
 	        // AsioDriver driver = sender as AsioDriver;

            // get the input channel and the stereo output channels
            Channel input = driver.InputChannels[0];
            Channel rightOutput = driver.OutputChannels[0];
            Channel leftOutput = driver.OutputChannels[1];

            int read = sourceStream.Read(buffer, 0, buffer.Length);
            if (read < buffer.Length)
            {
                // we have stopped
            }
            int readIndex = 0;

            for (int index = 0; index < leftOutput.BufferSize && readIndex < read; index++)
            {
                double left = 0.0;
                double right  = 0.0;
                if (sourceStream.WaveFormat.BitsPerSample == 16)
                {
                    left = BitConverter.ToInt16(buffer, readIndex) / 32768.0f;
                    readIndex += 2;
                    if (sourceStream.WaveFormat.Channels == 1)
                    {
                        right = left;
                    }
                    else
                    {
                        right = BitConverter.ToInt16(buffer, readIndex) / 32768.0f;
                        readIndex += 2;
                    }
                }
                else if (sourceStream.WaveFormat.BitsPerSample == 32)
                {
                    left = BitConverter.ToSingle(buffer, readIndex);
                    readIndex += 4;
                    if (sourceStream.WaveFormat.Channels == 1)
                    {
                        right = left;
                    }
                    else
                    {
                        right = BitConverter.ToSingle(buffer, readIndex);
                        readIndex += 4;
                    }
                }

                leftOutput[index] = left;
                rightOutput[index] = right;
            }
        }

        public bool IsPlaying
        {
            get { return playing; }
        }

        public bool IsPaused
        {
            get { return paused; }
        }

        public float Volume
        {
            get
            {
                return 1.0f;
            }
            set
            {                
                if(value != 1.0f)
                    throw new InvalidOperationException();
            }
        }

        public float Pan
        {
            get
            {
                return 0.0f;
            }
            set
            {
                if (value != 1.0f)
                    throw new InvalidOperationException();
            }
        }

        public void Dispose()
        {
            driver.Stop();
        }

    }
}
