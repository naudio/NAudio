using System;
using System.Collections.Generic;
using System.Text;
using NAudio.CoreAudioApi;
using System.Threading;
using System.Runtime.InteropServices;

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
        int latencyMilliseconds;
        int bufferFrameCount;
        int bytesPerFrame;
        byte[] readBuffer;
        PlaybackState playbackState;

        public WasapiOut(AudioClientShareMode shareMode, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, latency)
        {
        }

        static MMDevice GetDefaultAudioEndpoint()
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render,Role.Console);
        }

        public WasapiOut(MMDevice device, AudioClientShareMode shareMode, int latency)
        {
            this.audioClient = device.AudioClient;
            this.shareMode = shareMode;
            this.latencyMilliseconds = latency;
        }

        private void PlayThread(object state)
        {
            // fill a whole buffer
            bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = audioClient.MixFormat.Channels * audioClient.MixFormat.BitsPerSample / 8;
            readBuffer = new byte[bufferFrameCount * bytesPerFrame];
            FillBuffer(bufferFrameCount);

            audioClient.Start();

            while(playbackState == PlaybackState.Playing)
            {                 
                // Sleep for half the buffer duration.
                Thread.Sleep(latencyMilliseconds/2);

                // See how much buffer space is available.
                int numFramesPadding = audioClient.CurrentPadding;       
                int numFramesAvailable = bufferFrameCount - numFramesPadding;
                FillBuffer(numFramesAvailable);
            }
            Thread.Sleep(latencyMilliseconds / 2);
            audioClient.Stop();
            if (playbackState == PlaybackState.Stopped)
            {
                audioClient.Reset();
            }
        }

        private void FillBuffer(int frameCount)
        {
            IntPtr buffer = renderClient.GetBuffer(frameCount);
            int read = sourceStream.Read(readBuffer,0,frameCount * bytesPerFrame);
            if (read == 0)
            {
                playbackState = PlaybackState.Stopped;
            }
            Marshal.Copy(readBuffer,0,buffer,read);
            renderClient.ReleaseBuffer(frameCount,AudioClientBufferFlags.None);
        }

        #region IWavePlayer Members

        /// <summary>
        /// Begin Playback
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                playbackState = PlaybackState.Playing;
                ThreadPool.QueueUserWorkItem(new WaitCallback(PlayThread));
            }
        }

        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            // TODO: block on playback thread actually stopping
        }

        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
            }
            
        }

        public void Init(WaveStream waveStream)
        {
            long latencyRefTimes = latencyMilliseconds * 10000;

            if (!audioClient.IsFormatSupported(shareMode, waveStream.WaveFormat))
            {
                // for now, assume that WASAPI is working with IEEE floating point
                WaveFormat correctSampleRateFormat = 
                    WaveFormat.CreateIeeeFloatWaveFormat(
                        audioClient.MixFormat.SampleRate,
                        audioClient.MixFormat.Channels);
                if (!audioClient.IsFormatSupported(shareMode, correctSampleRateFormat))
                {
                    throw new NotSupportedException("Can't find a supported format to use");
                }
                this.sourceStream = new WaveFormatConversionStream(
                    correctSampleRateFormat, waveStream);
            }
            else
            {
                this.sourceStream = waveStream;
            }

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
                if (value != 1.0f)
                {
                    throw new NotImplementedException();
                }
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
