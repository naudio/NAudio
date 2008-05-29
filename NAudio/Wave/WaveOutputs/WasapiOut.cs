using System;
using System.Collections.Generic;
using System.Text;
using NAudio.CoreAudioApi;

namespace NAudio.Wave
{
    /// <summary>
    /// Support for playback using Wasapi
    /// </summary>
    public class WasapiOut : IWavePlayer
    {
        AudioClient audioClient;
        AudioClientShareMode shareMode;
        AudioRenderClient renderClient;
        WaveStream sourceStream;
        int latency;
        PlaybackState playbackState;

        public WasapiOut(MMDevice device, AudioClientShareMode shareMode, int latency)
        {
            this.audioClient = device.AudioClient;
            this.shareMode = shareMode;
            this.latency = latency;
        }

        private void PlayThread()
        {
            
        }

        #region IWavePlayer Members

        /// <summary>
        /// Begin Playback
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                
                if (playbackState == PlaybackState.Stopped)
                {
                    // TODO: enqueue buffers
                }
                audioClient.Start();
                playbackState = PlaybackState.Playing;
            }
        }

        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            audioClient.Stop();
            // flush the buffers
            audioClient.Reset();            
        }

        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
                audioClient.Stop();
            }
            
        }

        public void Init(WaveStream waveStream)
        {
            long latencyRefTimes = latency * 10000;
            this.sourceStream = waveStream;

            audioClient.Initialize(shareMode, AudioClientStreamFlags.None, latencyRefTimes, latencyRefTimes,
                waveStream.WaveFormat, Guid.Empty);
            renderClient = audioClient.AudioRenderClient;
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }

        public float Volume
        {
            get
            {
                return 1.0f;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
