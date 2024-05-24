using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NAudio.Sdl2.Interop;
using NAudio.Sdl2.Structures;
using NAudio.Wave;
using static NAudio.Sdl2.Interop.SDL;

// ReSharper disable once CheckNamespace
namespace NAudio.Sdl2
{
    /// <summary>
    /// WaveOut provider via SDL2 backend
    /// </summary>
    public class WaveOutSdl : IWavePlayer
    {
        private readonly SynchronizationContext syncContext;
        private IWaveProvider waveStream;
        private uint deviceNumber;
        private volatile PlaybackState playbackState;
        private AutoResetEvent callbackEvent;
        private double adjustLatencyPercent;
        private ushort frameSize;
        private byte[] frameBuffer;
        private byte[] frameVolumeBuffer;
        private float volume;
        private object volumeLock;
        private SDL_AudioSpec obtainedAudioSpec;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Prepares a wave output device for recording
        /// </summary>
        public WaveOutSdl()
        {
            syncContext = SynchronizationContext.Current;
            volumeLock = new object();
            DeviceId = -1;
            AudioConversion = AudioConversion.None;
            DesiredLatency = 300;
            AdjustLatencyPercent = 0.1;
            Volume = 1.28f;
        }

        /// <summary>
        /// Returns the number of WaveOutSdl devices available in the system
        /// </summary>
        public static int DeviceCount => SdlBindingWrapper.GetPlaybackDevicesNumber();

        /// <summary>
        /// Retrieves the capabilities of a WaveOutSdl device
        /// </summary>
        /// <param name="deviceId">Device to test</param>
        /// <returns>The WaveOutSdl device capabilities</returns>
        /// <remarks>
        /// This function only returns DeviceNumber and DeviceName on versions below SDL 2.0.16
        /// <para>Use the <see cref="WaveOutSdlCapabilities.IsAudioCapabilitiesValid"/> property to check if all capabilities are available</para>
        /// </remarks>
        public static WaveOutSdlCapabilities GetCapabilities(int deviceId)
        {
            var deviceName = SdlBindingWrapper.GetPlaybackDeviceName(deviceId);
            var runtimeSdlVersion = SdlBindingWrapper.GetRuntimeSdlVersion();
            var currentVersion = new Version(runtimeSdlVersion.major, runtimeSdlVersion.minor, runtimeSdlVersion.patch);
            var minimumRequiredVersion = new Version(2, 0, 16);
            if (currentVersion >= minimumRequiredVersion)
            {
                var deviceAudioSpec = SdlBindingWrapper.GetPlaybackDeviceAudioSpec(deviceId);
                var deviceBitSize = SdlBindingWrapper.GetAudioFormatBitSize(deviceAudioSpec.format);
                return new WaveOutSdlCapabilities
                {
                    DeviceNumber = deviceId,
                    DeviceName = deviceName,
                    Bits = deviceBitSize,
                    Channels = deviceAudioSpec.channels,
                    Format = deviceAudioSpec.format,
                    Frequency = deviceAudioSpec.freq,
                    Samples = deviceAudioSpec.samples,
                    Silence = deviceAudioSpec.silence,
                    Size = deviceAudioSpec.size,
                    IsAudioCapabilitiesValid = true
                };
            }

            return new WaveOutSdlCapabilities
            {
                DeviceNumber = deviceId,
                DeviceName = deviceName,
                IsAudioCapabilitiesValid = false
            };
        }

        /// <summary>
        /// Retrieves the capabilities list of a WaveOutSdl devices
        /// </summary>
        /// <returns>The WaveOutSdl capabilities list</returns>
        /// <remarks>
        /// This function only returns DeviceNumber and DeviceName on versions below SDL 2.0.16
        /// <para>Use the <see cref="WaveOutSdlCapabilities.IsAudioCapabilitiesValid"/> property to check if all capabilities are available</para>
        /// </remarks>
        public static List<WaveOutSdlCapabilities> GetCapabilitiesList()
        {
            List<WaveOutSdlCapabilities> list = new List<WaveOutSdlCapabilities>();
            var deviceCount = WaveOutSdl.DeviceCount;
            for (int index = 0; index < deviceCount; index++)
            {
                list.Add(GetCapabilities(index));
            }
            return list;
        }

        /// <summary>
        /// Retrieves the capabilities of a WaveOutSdl default device
        /// <para>This function is available since SDL 2.24.0</para>
        /// </summary>
        /// <returns>The WaveOutSdl default device capabilities</returns>
        public static WaveOutSdlCapabilities GetDefaultDeviceCapabilities()
        {
            SdlBindingWrapper.GetPlaybackDeviceDefaultAudioInfo(out var deviceName, out var deviceAudioSpec);
            var deviceBitSize = SdlBindingWrapper.GetAudioFormatBitSize(deviceAudioSpec.format);
            return new WaveOutSdlCapabilities
            {
                DeviceNumber = -1,
                DeviceName = deviceName,
                Bits = deviceBitSize,
                Channels = deviceAudioSpec.channels,
                Format = deviceAudioSpec.format,
                Frequency = deviceAudioSpec.freq,
                Samples = deviceAudioSpec.samples,
                Silence = deviceAudioSpec.silence,
                Size = deviceAudioSpec.size,
                IsAudioCapabilitiesValid = true
            };
        }

        /// <summary>
        /// Gets or sets the device id
        /// Should be set before a call to <see cref="Init(IWaveProvider)"/>
        /// <para>This must be between -1 and <see cref="DeviceCount"/> - 1</para>
        /// <para>-1 means stick to default device</para>
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the desired latency in milliseconds
        /// <para>Should be set before a call to <see cref="Init(IWaveProvider)"/></para>
        /// </summary>
        public int DesiredLatency { get; set; }

        /// <summary>
        /// Gets or sets the desired latency adjustment in percent
        /// <para>Value must be between 0 and 1</para>
        /// <para>This percent only affects the playback wait</para>
        /// </summary>
        public double AdjustLatencyPercent
        {
            get => adjustLatencyPercent;
            set => adjustLatencyPercent = value >= 0 && value <= 1
                ? value
                : throw new SdlException("The percent value must be between 0 and 1");
        }

        /// <summary>
        /// Volume for this device ranges from 0 to 1.28
        /// </summary>
        public float Volume
        {
            get
            {
                lock (volumeLock)
                {
                    return volume;
                }
            }
            set
            {
                lock (volumeLock)
                {
                    if (value < 0 || value > 1.28f)
                        throw new SdlException("The playback device volume must be between 0 and 1.28");
                    volume = value;
                }
            }
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// Gets playback state directly from sdl
        /// </summary>
        public PlaybackState SdlState
        {
            get
            {
                var status = SdlBindingWrapper.GetDeviceStatus(deviceNumber);
                switch (status)
                {
                    case SDL_AudioStatus.SDL_AUDIO_PLAYING:
                        return PlaybackState.Playing;
                    case SDL_AudioStatus.SDL_AUDIO_PAUSED:
                        return PlaybackState.Paused;
                    default:
                        return PlaybackState.Stopped;
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat => waveStream.WaveFormat;

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating what the format is actually using.
        /// </summary>
        /// <remarks>
        /// <para>This property accessible after <see cref="Init(IWaveProvider)"/> call</para>
        /// <para>If the <see cref="AudioConversion"/> is set to <see cref="AudioConversion.None"/> then this is the same as <see cref="OutputWaveFormat"/></para>
        /// </remarks>
        public WaveFormat ActualOutputWaveFormat { get; private set; }

        /// <summary>
        /// Audio conversion features
        /// </summary>
        /// <remarks>
        /// These flags specify how SDL should behave when a device cannot offer a specific feature<br/>
        /// If the application requests a feature that the hardware doesn't offer, SDL will always try to get the closest equivalent<br/>
        /// For example, if you ask for float32 audio format, but the sound card only supports int16, SDL will set the hardware to int16
        /// <para>If your application can only handle one specific data format, pass a <see cref="AudioConversion.None" /> for <see cref="AudioConversion"/> and let SDL transparently handle any differences</para>
        /// </remarks>
        public AudioConversion AudioConversion { get; set; }

        /// <summary>
        /// Initializes the WaveOut device
        /// </summary>
        /// <param name="waveProvider">WaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            if (playbackState != PlaybackState.Stopped)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }
            callbackEvent = new AutoResetEvent(false);
            waveStream = waveProvider;
            frameSize = (ushort)waveProvider.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            frameBuffer = new byte[frameSize];
            frameVolumeBuffer = new byte[frameSize];
            var desiredSpec = new SDL_AudioSpec();
            desiredSpec.freq = waveProvider.WaveFormat.SampleRate;
            desiredSpec.format = GetAudioDataFormat();
            desiredSpec.channels = (byte)waveProvider.WaveFormat.Channels;
            desiredSpec.silence = 0;
            desiredSpec.samples = frameSize;
            var deviceName = SdlBindingWrapper.GetPlaybackDeviceName(DeviceId);
            var openDeviceNumber = SdlBindingWrapper.OpenPlaybackDevice(deviceName, ref desiredSpec, out obtainedAudioSpec, AudioConversion);
            ActualOutputWaveFormat = GetWaveFormat(obtainedAudioSpec);
            deviceNumber = openDeviceNumber;
        }

        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (waveStream == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }
            if (playbackState == PlaybackState.Stopped)
            {
                Resume();
                ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
            }
            else if (playbackState == PlaybackState.Paused)
            {
                Resume();
            }
        }

        /// <summary>
        /// Stop the audio
        /// </summary>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                SdlBindingWrapper.StopPlaybackDevice(deviceNumber);
                SdlBindingWrapper.ClearQueuedAudio(deviceNumber);
                callbackEvent.Set(); // give the thread a kick, make sure we exit
            }
        }

        /// <summary>
        /// Pause the audio
        /// </summary>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
                SdlBindingWrapper.StopPlaybackDevice(deviceNumber);
            }
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Stop();
            if (disposing)
            {
                DisposeBuffers();
            }
            CloseWaveOutSdl();
        }

        /// <summary>
        /// Resume playing
        /// </summary>
        private void Resume()
        {
            SdlBindingWrapper.StartPlaybackDevice(deviceNumber);
            playbackState = PlaybackState.Playing;
            callbackEvent.Set();
        }

        /// <summary>
        /// Closes WaveOutSdl device
        /// </summary>
        private void CloseWaveOutSdl()
        {
            if (callbackEvent != null)
            {
                callbackEvent.Close();
                callbackEvent = null;
            }
            SdlBindingWrapper.ClosePlaybackDevice(deviceNumber);
        }

        /// <summary>
        /// Disposes frame buffers
        /// </summary>
        private void DisposeBuffers()
        {
            frameBuffer = null;
            frameVolumeBuffer = null;
        }

        /// <summary>
        /// Return WaveFormat guessed by <see cref="SDL_AudioSpec"/>
        /// </summary>
        /// <param name="spec">Audio spec</param>
        /// <returns>Wave format</returns>
        private WaveFormat GetWaveFormat(SDL_AudioSpec spec)
        {
            var bitSize = SdlBindingWrapper.GetAudioFormatBitSize(spec.format);
            if (spec.format == AUDIO_F32
                || spec.format == AUDIO_F32LSB
                || spec.format == AUDIO_F32MSB
                || spec.format == AUDIO_F32SYS)
            {
                return WaveFormat.CreateIeeeFloatWaveFormat(spec.freq, spec.channels);
            }
            return new WaveFormat(spec.freq, bitSize, spec.channels);
        }

        /// <summary>
        /// Returns the audio format guessed by <see cref="WaveFormat.BitsPerSample"/>
        /// </summary>
        /// <returns>Audio format</returns>
        private ushort GetAudioDataFormat()
        {
            switch (OutputWaveFormat.BitsPerSample)
            {
                case 8:
                    return AUDIO_S8;
                case 16:
                    return AUDIO_S16SYS;
                case 32:
                    return AUDIO_S32SYS;
                default:
                    return (ushort)OutputWaveFormat.BitsPerSample;
            }
        }

        /// <summary>
        /// Thread at which playback is happen
        /// </summary>
        private void PlaybackThread()
        {
            Exception exception = null;
            try
            {
                DoPlayback();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                playbackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        /// <summary>
        /// Playback process
        /// </summary>
        private unsafe void DoPlayback()
        {
            while (playbackState != PlaybackState.Stopped)
            {
                // workaround to get rid of stuttering
                // i assume on different hardware adjusting must be different
                // is it possible that callbacks will help?
                var adjustedLatency = DesiredLatency - (int)(DesiredLatency * AdjustLatencyPercent);
                if (!callbackEvent.WaitOne(adjustedLatency))
                {
                    if (playbackState == PlaybackState.Playing)
                    {
                        Debug.WriteLine("WARNING: WaveOutSdl callback event timeout");
                    }
                }

                if (playbackState == PlaybackState.Playing)
                {
                    Array.Clear(frameBuffer, 0, frameBuffer.Length);
                    var readSize = waveStream.Read(frameBuffer, 0, frameBuffer.Length);

                    if (readSize == 0)
                    {
                        playbackState = PlaybackState.Stopped;
                        callbackEvent.Set();
                    }

                    Array.Clear(frameVolumeBuffer, 0, frameVolumeBuffer.Length);
                    SdlBindingWrapper.ChangePlaybackDeviceVolume(
                        frameVolumeBuffer,
                        frameBuffer,
                        obtainedAudioSpec.format,
                        (uint)frameVolumeBuffer.Length,
                        Volume);

                    fixed (byte* ptr = &frameVolumeBuffer[0])
                    {
                        SdlBindingWrapper.QueueAudio(deviceNumber, (IntPtr)ptr, (uint)readSize);
                    }
                }
            }
        }

        /// <summary>
        /// Raise playback stopped event
        /// </summary>
        /// <param name="e"></param>
        private void RaisePlaybackStoppedEvent(Exception e)
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
    }
}
