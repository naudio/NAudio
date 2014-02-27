using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioDemo.MediaFoundationDemo
{
    /// <summary>
    /// An experimental example of how WASAPI could be used on a GUI thread
    /// (to get round STA/MTA threading issues)
    /// Uses the WinForms timer, for WPF might be best to use the DispatcherTimer
    /// </summary>
    public class WasapiOutGuiThread : IWavePlayer
    {
        private readonly AudioClientShareMode shareMode;
        private readonly Timer timer;
        private readonly int latencyMilliseconds;
        private AudioClient audioClient;
        private AudioRenderClient renderClient;
        private IWaveProvider sourceProvider;
        private int bufferFrameCount;
        private int bytesPerFrame;
        private byte[] readBuffer;
        private PlaybackState playbackState;
        private WaveFormat outputFormat;
        private ResamplerDmoStream resamplerDmoStream;

        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        public WasapiOutGuiThread() :
            this(GetDefaultAudioEndpoint(), AudioClientShareMode.Shared, 200)
        {
        }

        /// <summary>
        /// Creates a new WASAPI Output device
        /// </summary>
        /// <param name="device">Device to use</param>
        /// <param name="shareMode">Share mode to use</param>
        /// <param name="latency">Latency in milliseconds</param>
        public WasapiOutGuiThread(MMDevice device, AudioClientShareMode shareMode, int latency)
        {
            audioClient = device.AudioClient;
            outputFormat = audioClient.MixFormat;
            this.shareMode = shareMode;
            latencyMilliseconds = latency;
            timer = new Timer();
            timer.Tick += TimerOnTick;
            timer.Interval = latency/2;
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (playbackState == PlaybackState.Playing)
            {
                try
                {
                    // See how much buffer space is available.
                    int numFramesPadding = audioClient.CurrentPadding;
                    int numFramesAvailable = bufferFrameCount - numFramesPadding;
                    if (numFramesAvailable > 0)
                    {
                        FillBuffer(sourceProvider, numFramesAvailable);
                    }
                }
                catch (Exception e)
                {
                    PerformStop(e);
                }
            }
            else if (playbackState == PlaybackState.Stopped)
            {
                // user requested stop
                PerformStop(null);
            }
        }

        private void PerformStop(Exception e)
        {
            timer.Enabled = false;
            audioClient.Stop();
            RaisePlaybackStopped(e);
            audioClient.Reset();
        }

        static MMDevice GetDefaultAudioEndpoint()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("WASAPI supported only on Windows Vista and above");
            }
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                handler(this, new StoppedEventArgs(e));
            }
        }

        private void FillBuffer(IWaveProvider playbackProvider, int frameCount)
        {
            IntPtr buffer = renderClient.GetBuffer(frameCount);
            int readLength = frameCount * bytesPerFrame;
            int read = playbackProvider.Read(readBuffer, 0, readLength);
            if (read == 0)
            {
                playbackState = PlaybackState.Stopped;
            }
            Marshal.Copy(readBuffer, 0, buffer, read);
            int actualFrameCount = read / bytesPerFrame;
            /*if (actualFrameCount != frameCount)
            {
                Debug.WriteLine(String.Format("WASAPI wanted {0} frames, supplied {1}", frameCount, actualFrameCount ));
            }*/
            renderClient.ReleaseBuffer(actualFrameCount, AudioClientBufferFlags.None);
        }

        #region IWavePlayer Members

        /// <summary>
        /// Begin Playback
        /// </summary>
        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                // fill a whole buffer
                FillBuffer(sourceProvider, bufferFrameCount);
                audioClient.Start();
                playbackState = PlaybackState.Playing;
                timer.Enabled = true;
            }
            else if (playbackState == PlaybackState.Paused)
            {
                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Stop playback and flush buffers
        /// </summary>
        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
        }

        /// <summary>
        /// Stop playback without flushing buffers
        /// </summary>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
            }
        }

        /// <summary>
        /// Initialize for playing the specified wave stream
        /// </summary>
        /// <param name="waveProvider">IWaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            long latencyRefTimes = latencyMilliseconds * 10000;
            outputFormat = waveProvider.WaveFormat;
            // first attempt uses the WaveFormat from the WaveStream
            WaveFormatExtensible closestSampleRateFormat;
            if (!audioClient.IsFormatSupported(shareMode, outputFormat, out closestSampleRateFormat))
            {
                // Use closesSampleRateFormat (in sharedMode, it equals usualy to the audioClient.MixFormat)
                // See documentation : http://msdn.microsoft.com/en-us/library/ms678737(VS.85).aspx 
                // They say : "In shared mode, the audio engine always supports the mix format"
                // The MixFormat is more likely to be a WaveFormatExtensible.
                if (closestSampleRateFormat == null)
                {
                    WaveFormat correctSampleRateFormat = audioClient.MixFormat;

                    if (!audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                    {
                        // Iterate from Worst to Best Format
                        WaveFormatExtensible[] bestToWorstFormats = {
                                                                        new WaveFormatExtensible(
                                                                            outputFormat.SampleRate, 32,
                                                                            outputFormat.Channels),
                                                                        new WaveFormatExtensible(
                                                                            outputFormat.SampleRate, 24,
                                                                            outputFormat.Channels),
                                                                        new WaveFormatExtensible(
                                                                            outputFormat.SampleRate, 16,
                                                                            outputFormat.Channels),
                                                                    };

                        // Check from best Format to worst format ( Float32, Int24, Int16 )
                        for (int i = 0; i < bestToWorstFormats.Length; i++)
                        {
                            correctSampleRateFormat = bestToWorstFormats[i];
                            if (audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                            {
                                break;
                            }
                            correctSampleRateFormat = null;
                        }

                        // If still null, then test on the PCM16, 2 channels
                        if (correctSampleRateFormat == null)
                        {
                            // Last Last Last Chance (Thanks WASAPI)
                            correctSampleRateFormat = new WaveFormatExtensible(outputFormat.SampleRate, 16, 2);
                            if (!audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                            {
                                throw new NotSupportedException("Can't find a supported format to use");
                            }
                        }
                    }
                    outputFormat = correctSampleRateFormat;
                }
                else
                {
                    outputFormat = closestSampleRateFormat;
                }

                // just check that we can make it.
                resamplerDmoStream = new ResamplerDmoStream(waveProvider, outputFormat);
                sourceProvider = resamplerDmoStream;
            }
            else
            {
                sourceProvider = waveProvider;
            }

            // Normal setup for both sharedMode
            audioClient.Initialize(shareMode, AudioClientStreamFlags.None, latencyRefTimes, 0,
                                    outputFormat, Guid.Empty);
            

            // Get the RenderClient
            renderClient = audioClient.AudioRenderClient;

            // set up the read buffer
            bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = outputFormat.Channels * outputFormat.BitsPerSample / 8;
            readBuffer = new byte[bufferFrameCount * bytesPerFrame];
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }

        /// <summary>
        /// Volume
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
                {
                    throw new NotImplementedException();
                }
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioClient != null)
            {
                Stop();

                audioClient.Dispose();
                audioClient = null;
                renderClient = null;
            }
            if (resamplerDmoStream != null)
            {
                resamplerDmoStream.Dispose();
                resamplerDmoStream = null;
            }

        }

        #endregion
    }
}