using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Threading;
using System.Runtime.InteropServices;
using NAudio.Utils;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Support for playback using Wasapi
    /// </summary>
    public class WasapiOut : IWavePlayer, IWavePosition
    {
        private AudioClient audioClient;
        private readonly MMDevice mmDevice;
        private readonly AudioClientShareMode shareMode;
        private AudioRenderClient renderClient;
        private IWaveProvider sourceProvider;
        private int latencyMilliseconds;
        private int bufferFrameCount;
        private int bytesPerFrame;
        private readonly bool isUsingEventSync;
        private EventWaitHandle frameEventWaitHandle;
        private byte[] readBuffer;
        private volatile PlaybackState playbackState;
        private Thread playThread;
        private readonly SynchronizationContext syncContext;
        private bool dmoResamplerNeeded;
        
        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// WASAPI Out shared mode, default
        /// </summary>
        public WasapiOut() :
            this(GetDefaultAudioEndpoint(), AudioClientShareMode.Shared, true, 200)
        {

        }

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        /// <param name="shareMode">ShareMode - shared or exclusive</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(AudioClientShareMode shareMode, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, true, latency)
        {

        }

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        /// <param name="shareMode">ShareMode - shared or exclusive</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(AudioClientShareMode shareMode, bool useEventSync, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, useEventSync, latency)
        {

        }

        /// <summary>
        /// Creates a new WASAPI Output
        /// </summary>
        /// <param name="device">Device to use</param>
        /// <param name="shareMode"></param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(MMDevice device, AudioClientShareMode shareMode, bool useEventSync, int latency)
        {
            audioClient = device.AudioClient;
            mmDevice = device;
            this.shareMode = shareMode;
            isUsingEventSync = useEventSync;
            latencyMilliseconds = latency;
            syncContext = SynchronizationContext.Current;
            OutputWaveFormat = audioClient.MixFormat; // allow the user to query the default format for shared mode streams
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

        private void PlayThread()
        {
            ResamplerDmoStream resamplerDmoStream = null;
            IWaveProvider playbackProvider = sourceProvider;
            Exception exception = null;
            try
            {
                if (dmoResamplerNeeded)
                {
                    resamplerDmoStream = new ResamplerDmoStream(sourceProvider, OutputWaveFormat);
                    playbackProvider = resamplerDmoStream;
                }
                // fill a whole buffer
                bufferFrameCount = audioClient.BufferSize;
                bytesPerFrame = OutputWaveFormat.Channels * OutputWaveFormat.BitsPerSample / 8;
                readBuffer = BufferHelpers.Ensure(readBuffer, bufferFrameCount * bytesPerFrame);
                if (FillBuffer(playbackProvider, bufferFrameCount))
                {
                    // played a zero length stream - exit immediately
                    return;
                }
                // to calculate buffer duration but does always seem to match latency
                // var bufferDurationMilliseconds = (bufferFrameCount * 1000) /OutputWaveFormat.SampleRate;
                // Create WaitHandle for sync
                var waitHandles = new WaitHandle[] { frameEventWaitHandle };

                audioClient.Start();

                while (playbackState != PlaybackState.Stopped)
                {
                    // If using Event Sync, Wait for notification from AudioClient or Sleep half latency
                    if (isUsingEventSync)
                    {
                        WaitHandle.WaitAny(waitHandles, 3 * latencyMilliseconds, false);
                    }
                    else
                    {
                        Thread.Sleep(latencyMilliseconds / 2);
                    }

                    // If still playing
                    if (playbackState == PlaybackState.Playing)
                    {
                        // See how much buffer space is available.
                        int numFramesPadding;
                        if (isUsingEventSync)
                        {
                            // In exclusive mode, always ask the max = bufferFrameCount = audioClient.BufferSize
                            numFramesPadding = (shareMode == AudioClientShareMode.Shared) ? audioClient.CurrentPadding : 0;
                        }
                        else
                        {
                            numFramesPadding = audioClient.CurrentPadding;
                        }
                        int numFramesAvailable = bufferFrameCount - numFramesPadding;
                        if (numFramesAvailable > 10) // see https://naudio.codeplex.com/workitem/16363
                        {
                            if (FillBuffer(playbackProvider, numFramesAvailable))
                            {
                                // reached the end
                                break;
                            }
                        }
                    }
                }
                if (playbackState == PlaybackState.Playing)
                {
                    // we got here by reaching the end of the input file, so
                    // let's make sure the last buffer has time to play
                    // (otherwise the user requested stop, so we'll just stop
                    // immediately
                    Thread.Sleep(isUsingEventSync ? latencyMilliseconds : latencyMilliseconds / 2);
                }
                audioClient.Stop();
                // set if we got here by reaching the end
                playbackState = PlaybackState.Stopped;
                audioClient.Reset();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (resamplerDmoStream != null)
                {
                    resamplerDmoStream.Dispose();
                }
                RaisePlaybackStopped(exception);
            }
        }

        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        /// <summary>
        /// returns true if reached the end
        /// </summary>
        private bool FillBuffer(IWaveProvider playbackProvider, int frameCount)
        {
            var readLength = frameCount * bytesPerFrame;
            int read = playbackProvider.Read(readBuffer, 0, readLength);
            if (read == 0)
            {
                return true;
            }
            var buffer = renderClient.GetBuffer(frameCount);
            Marshal.Copy(readBuffer, 0, buffer, read);
            if (this.isUsingEventSync && this.shareMode == AudioClientShareMode.Exclusive)
            {
                if (read < readLength)
                {
                    // need to zero the end of the buffer as we have to
                    // pass frameCount
                    unsafe
                    {
                        byte* pByte = (byte*)buffer;
                        while(read < readLength)
                        {
                            pByte[read++] = 0;
                        }
                    }
                }

                renderClient.ReleaseBuffer(frameCount, AudioClientBufferFlags.None);
            }
            else
            {
                int actualFrameCount = read / bytesPerFrame;
                /*if (actualFrameCount != frameCount)
                {
                    Debug.WriteLine(String.Format("WASAPI wanted {0} frames, supplied {1}", frameCount, actualFrameCount ));
                }*/
                renderClient.ReleaseBuffer(actualFrameCount, AudioClientBufferFlags.None);
            }
            return false;
        }

        private WaveFormat GetFallbackFormat()
        {
            var deviceSampleRate = audioClient.MixFormat.SampleRate;
            var deviceChannels = audioClient.MixFormat.Channels; // almost certain to be stereo

            // we are in exclusive mode
            // First priority is to try the sample rate you provided.
            var sampleRatesToTry = new List<int>() { OutputWaveFormat.SampleRate };
            // Second priority is to use the sample rate the device wants
            if (!sampleRatesToTry.Contains(deviceSampleRate)) sampleRatesToTry.Add(deviceSampleRate);
            // And if we've not already got 44.1 and 48kHz in the list, let's try them too
            if (!sampleRatesToTry.Contains(44100)) sampleRatesToTry.Add(44100);
            if (!sampleRatesToTry.Contains(48000)) sampleRatesToTry.Add(48000);

            var channelCountsToTry = new List<int>() { OutputWaveFormat.Channels };
            if (!channelCountsToTry.Contains(deviceChannels)) channelCountsToTry.Add(deviceChannels);
            if (!channelCountsToTry.Contains(2)) channelCountsToTry.Add(2);

            var bitDepthsToTry = new List<int>() { OutputWaveFormat.BitsPerSample };
            if (!bitDepthsToTry.Contains(32)) bitDepthsToTry.Add(32);
            if (!bitDepthsToTry.Contains(24)) bitDepthsToTry.Add(24);
            if (!bitDepthsToTry.Contains(16)) bitDepthsToTry.Add(16);

            foreach (var sampleRate in sampleRatesToTry)
            {
                foreach (var channelCount in channelCountsToTry)
                {
                    foreach (var bitDepth in bitDepthsToTry)
                    {
                        var format = new WaveFormatExtensible(sampleRate, bitDepth, channelCount);
                        if (audioClient.IsFormatSupported(shareMode, format))
                            return format;
                    }
                }
            }
            throw new NotSupportedException("Can't find a supported format to use");
        }

        /// <summary>
        /// Gets the current position in bytes from the wave output device.
        /// (n.b. this is not the same thing as the position within your reader
        /// stream)
        /// </summary>
        /// <returns>Position in bytes</returns>
        public long GetPosition()
        {
            ulong pos;
            switch (playbackState)
            {
                case PlaybackState.Stopped:
                    return 0;
                case PlaybackState.Playing:
                    pos = audioClient.AudioClockClient.AdjustedPosition;
                    break;
                default: // PlaybackState.Paused
                    audioClient.AudioClockClient.GetPosition(out pos, out _);
                    break;
            }
            return ((long)pos * OutputWaveFormat.AverageBytesPerSecond) / (long)audioClient.AudioClockClient.Frequency;
        }

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat { get; private set; }

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
                    playThread = new Thread(PlayThread)
                    {
                        IsBackground = true,
                    };
                    playbackState = PlaybackState.Playing;
                    playThread.Start();                    
                }
                else
                {
                    playbackState = PlaybackState.Playing;
                }                
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
        /// <param name="waveProvider">IWaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            long latencyRefTimes = latencyMilliseconds * 10000L;
            OutputWaveFormat = waveProvider.WaveFormat;

            // allow auto sample rate conversion - works for shared mode
            var flags = AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
            sourceProvider = waveProvider;

            if (shareMode == AudioClientShareMode.Exclusive)
            {
                flags = AudioClientStreamFlags.None;
                if (!audioClient.IsFormatSupported(shareMode, OutputWaveFormat, out WaveFormatExtensible closestSampleRateFormat))
                {
                    // Use closesSampleRateFormat (in sharedMode, it equals usualy to the audioClient.MixFormat)
                    // See documentation : http://msdn.microsoft.com/en-us/library/ms678737(VS.85).aspx 
                    // They say : "In shared mode, the audio engine always supports the mix format"
                    // The MixFormat is more likely to be a WaveFormatExtensible.
                    if (closestSampleRateFormat == null)
                    {

                        OutputWaveFormat = GetFallbackFormat();
                    }
                    else
                    {
                        OutputWaveFormat = closestSampleRateFormat;
                    }

                    try
                    {
                        // just check that we can make it.
                        using (new ResamplerDmoStream(waveProvider, OutputWaveFormat))
                        {
                        }
                    }
                    catch (Exception)
                    {
                        // On Windows 10 some poorly coded drivers return a bad format in to closestSampleRateFormat
                        // In that case, try and fallback as if it provided no closest (e.g. force trying the mix format)
                        OutputWaveFormat = GetFallbackFormat();
                        using (new ResamplerDmoStream(waveProvider, OutputWaveFormat))
                        {
                        }
                    }
                    dmoResamplerNeeded = true;
                }
                else
                {
                    dmoResamplerNeeded = false;
                }
            }

            // If using EventSync, setup is specific with shareMode
            if (isUsingEventSync)
            {
                // Init Shared or Exclusive
                if (shareMode == AudioClientShareMode.Shared)
                {
                    // With EventCallBack and Shared, both latencies must be set to 0 (update - not sure this is true anymore)
                    // 
                    audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags, latencyRefTimes, 0,
                        OutputWaveFormat, Guid.Empty);

                    // Windows 10 returns 0 from stream latency, resulting in maxing out CPU usage later
                    var streamLatency = audioClient.StreamLatency;
                    if (streamLatency != 0)
                    {
                        // Get back the effective latency from AudioClient
                        latencyMilliseconds = (int)(streamLatency / 10000);
                    }
                }
                else
                {
                    try
                    {
                        // With EventCallBack and Exclusive, both latencies must equals
                        audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags, latencyRefTimes, latencyRefTimes,
                                            OutputWaveFormat, Guid.Empty);
                    }
                    catch (COMException ex)
                    {
                        // Starting with Windows 7, Initialize can return AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED for a render device.
                        // We should to initialize again.
                        if (ex.ErrorCode != AudioClientErrorCode.BufferSizeNotAligned)
                            throw;

                        // Calculate the new latency.
                        long newLatencyRefTimes = (long)(10000000.0 /
                            (double)this.OutputWaveFormat.SampleRate *
                            (double)this.audioClient.BufferSize + 0.5);

                        this.audioClient.Dispose();
                        this.audioClient = this.mmDevice.AudioClient;
                        this.audioClient.Initialize(this.shareMode, AudioClientStreamFlags.EventCallback | flags,
                                            newLatencyRefTimes, newLatencyRefTimes, this.OutputWaveFormat, Guid.Empty);
                    }
                }

                // Create the Wait Event Handle
                frameEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEventWaitHandle.SafeWaitHandle.DangerousGetHandle());
            }
            else
            {
                // Normal setup for both sharedMode
                audioClient.Initialize(shareMode, flags, latencyRefTimes, 0,
                                    OutputWaveFormat, Guid.Empty);
            }

            // Get the RenderClient
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
                return mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar;                                
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "Volume must be between 0.0 and 1.0");
                if (value > 1) throw new ArgumentOutOfRangeException("value", "Volume must be between 0.0 and 1.0");
                mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
            }
        }

        /// <summary>
        /// Retrieve the AudioStreamVolume object for this audio stream
        /// </summary>
        /// <remarks>
        /// This returns the AudioStreamVolume object ONLY for shared audio streams.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// This is thrown when an exclusive audio stream is being used.
        /// </exception>
        public AudioStreamVolume AudioStreamVolume
        {
            get 
            {
                if (shareMode == AudioClientShareMode.Exclusive)
                {
                    throw new InvalidOperationException("AudioStreamVolume is ONLY supported for shared audio streams.");
                }
                return audioClient.AudioStreamVolume;  
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
        }

#endregion
    }
}
