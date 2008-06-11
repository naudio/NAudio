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
        Thread playThread;

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        /// <param name="shareMode">ShareMode - shared or exclusive</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(AudioClientShareMode shareMode, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, latency)
        {
        }

        static MMDevice GetDefaultAudioEndpoint()
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render,Role.Console);
        }

        /// <summary>
        /// Creates a new WASAPI Output
        /// </summary>
        /// <param name="device">Device to use</param>
        /// <param name="shareMode"></param>
        /// <param name="latency"></param>
        public WasapiOut(MMDevice device, AudioClientShareMode shareMode, int latency)
        {
            this.audioClient = device.AudioClient;
            this.shareMode = shareMode;
            this.latencyMilliseconds = latency;
        }

        private void PlayThread()
        {
            // fill a whole buffer
            bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = audioClient.MixFormat.Channels * audioClient.MixFormat.BitsPerSample / 8;
            readBuffer = new byte[bufferFrameCount * bytesPerFrame];
            FillBuffer(bufferFrameCount);

            audioClient.Start();

            while(playbackState != PlaybackState.Stopped)
            {                 
                // Sleep for half the buffer duration.
                Thread.Sleep(latencyMilliseconds/2);
                if (playbackState == PlaybackState.Playing)
                {
                    // See how much buffer space is available.
                    int numFramesPadding = audioClient.CurrentPadding;
                    int numFramesAvailable = bufferFrameCount - numFramesPadding;
                    if (numFramesAvailable > 0)
                    {
                        FillBuffer(numFramesAvailable);
                    }                    
                }
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
            int actualFrameCount = read / bytesPerFrame;
            renderClient.ReleaseBuffer(actualFrameCount,AudioClientBufferFlags.None);
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
                    playThread = new Thread(new ThreadStart(PlayThread));
                    playThread.Start();                    
                }

                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Stop playback and flush buffers
        /// </summary>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                playThread.Join();
                playThread = null;
            }
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
        /// <param name="waveStream">Wavestream to play</param>
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
                this.sourceStream = new ResamplerDmoStream(waveStream,
                    correctSampleRateFormat);
            }
            else
            {
                this.sourceStream = waveStream;
            }

            audioClient.Initialize(shareMode, AudioClientStreamFlags.None, latencyRefTimes, latencyRefTimes,
                sourceStream.WaveFormat, Guid.Empty);
            renderClient = audioClient.AudioRenderClient;
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
            Stop();
            // allow GC to release the COM object when it runs
            audioClient = null;
            renderClient = null;

        }

        #endregion
    }
}
