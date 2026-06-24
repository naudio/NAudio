using System;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioDemo.MediaFoundationDemo
{
    /// <summary>
    /// An experimental example of how WasapiPlayer's approach could be used on a GUI thread
    /// (to get round STA/MTA threading issues).
    /// Uses the WinForms Timer; for WPF, a DispatcherTimer would be more appropriate.
    /// Unlike WasapiPlayer, this uses timer-based polling instead of a background thread,
    /// and uses the zero-copy Span-based IWaveProvider interface.
    /// </summary>
    public class WasapiPlayerGuiThread : IWavePlayer
    {
        private readonly AudioClientShareMode shareMode;
        private readonly Timer timer;
        private readonly int latencyMilliseconds;
        private AudioClient audioClient;
        private AudioRenderClient renderClient;
        private IWaveProvider waveProvider;
        private int bufferFrameCount;
        private int bytesPerFrame;
        private PlaybackState playbackState;
        private WaveFormat outputFormat;

        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Creates a WasapiPlayerGuiThread using the default audio endpoint.
        /// </summary>
        public WasapiPlayerGuiThread() :
            this(GetDefaultAudioEndpoint(), AudioClientShareMode.Shared, 200)
        {
        }

        /// <summary>
        /// Creates a new WasapiPlayerGuiThread.
        /// </summary>
        /// <param name="device">Device to use</param>
        /// <param name="shareMode">Share mode to use</param>
        /// <param name="latency">Latency in milliseconds</param>
        public WasapiPlayerGuiThread(MMDevice device, AudioClientShareMode shareMode, int latency)
        {
            audioClient = device.CreateAudioClient();
            outputFormat = audioClient.MixFormat;
            this.shareMode = shareMode;
            latencyMilliseconds = latency;
            timer = new Timer();
            timer.Tick += TimerOnTick;
            timer.Interval = latency / 2;
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (playbackState == PlaybackState.Playing)
            {
                try
                {
                    int numFramesPadding = audioClient.CurrentPadding;
                    int numFramesAvailable = bufferFrameCount - numFramesPadding;
                    if (numFramesAvailable > 0)
                    {
                        if (FillBuffer(numFramesAvailable))
                        {
                            playbackState = PlaybackState.Stopped;
                        }
                    }
                }
                catch (Exception e)
                {
                    PerformStop(e);
                }
            }
            else if (playbackState == PlaybackState.Stopped)
            {
                PerformStop(null);
            }
        }

        private void PerformStop(Exception e)
        {
            timer.Enabled = false;
            audioClient.Stop();
            audioClient.Reset();
            PlaybackStopped?.Invoke(this, new StoppedEventArgs(e));
        }

        static MMDevice GetDefaultAudioEndpoint()
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        /// <summary>
        /// Fills the WASAPI render buffer directly from the audio source using Span (zero-copy).
        /// Returns true if the source has ended.
        /// </summary>
        private bool FillBuffer(int frameCount)
        {
            using var lease = renderClient.GetBufferLease(frameCount, bytesPerFrame);
            int bytesRead = waveProvider.Read(lease.Buffer);
            if (bytesRead == 0)
            {
                lease.Release(0, AudioClientBufferFlags.Silent);
                return true;
            }

            int framesRead = bytesRead / bytesPerFrame;
            lease.Release(framesRead);
            return false;
        }

        /// <summary>
        /// Begin Playback
        /// </summary>
        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                FillBuffer(bufferFrameCount);
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
        /// Initialize for playing the specified audio source.
        /// </summary>
        /// <param name="waveProvider">IWaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            this.waveProvider = waveProvider;
            long latencyRefTimes = latencyMilliseconds * 10000L;
            outputFormat = waveProvider.WaveFormat;

            var flags = shareMode == AudioClientShareMode.Shared
                ? AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality
                : AudioClientStreamFlags.None;

            audioClient.Initialize(shareMode, flags, latencyRefTimes, 0,
                outputFormat, Guid.Empty);

            renderClient = audioClient.AudioRenderClient;
            bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = outputFormat.BlockAlign;
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// Volume
        /// </summary>
        public float Volume
        {
            get => 1.0f;
            set
            {
                if (value != 1.0f)
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// The output format being sent to the audio device.
        /// </summary>
        public WaveFormat OutputWaveFormat => outputFormat;

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (audioClient != null)
            {
                Stop();
                audioClient.Dispose();
                audioClient = null;
                renderClient = null;
            }
        }
    }
}
