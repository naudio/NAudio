using System;
using System.Diagnostics;
using System.Threading;
using NAudio.Sdl2.Interop;
using NAudio.Sdl2.Structures;
using NAudio.Wave;
using static SDL2.SDL;

// ReSharper disable once CheckNamespace
namespace NAudio.Sdl2
{
    public class WaveOutSdl : IWavePlayer
    {
        private readonly SynchronizationContext _syncContext;
        private IWaveProvider _waveStream;
        private uint _deviceNumber;
        private volatile PlaybackState _playbackState;
        private AutoResetEvent _callbackEvent;
        private double _adjustLatencyPercent;
        private ulong _position;
        private object _positionLock;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Indicates playback position has changed
        /// </summary>
        public event EventHandler<PositionChangedEventArgs> PositionChanged;

        public WaveOutSdl()
        {
            _syncContext = SynchronizationContext.Current;
            _positionLock = new object();
            DeviceId = 0;
            AudioConversion = AudioConversion.None;
            DesiredLatency = 300;
            AdjustLatencyPercent = 0.1;
        }

        /// <summary>
        /// Returns the number of WaveOutSdl devices available in the system
        /// </summary>
        public static int DeviceCount => Sdl2Interop.GetPlaybackDevicesNumber();

        /// <summary>
        /// Retrieves the capabilities of a WaveOutSdl device
        /// </summary>
        /// <param name="deviceId">Device to test</param>
        /// <returns>The WaveOutSdl device capabilities</returns>
        public static WaveOutSdlCapabilities GetCapabilities(int deviceId)
        {
            var deviceName = Sdl2Interop.GetPlaybackDeviceName(deviceId);
            var deviceSpec = Sdl2Interop.GetPlaybackDeviceSpec(deviceId);
            var deviceBitSize = Sdl2Interop.GetAudioFormatBitSize(deviceSpec.format);
            return new WaveOutSdlCapabilities
            {
                DeviceNumber = deviceId,
                DeviceName = deviceName,
                Bits = deviceBitSize,
                Channels = deviceSpec.channels,
                Format = deviceSpec.format,
                Frequency = deviceSpec.freq,
                Samples = deviceSpec.samples,
                Silence = deviceSpec.silence,
                Size = deviceSpec.size
            };
        }

        /// <summary>
        /// The device id to use
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the desired latency in milliseconds
        /// <para>Should be set before a call to Init</para>
        /// </summary>
        public int DesiredLatency { get; set; }

        /// <summary>
        /// Gets or sets the desired latency adjustment in percent
        /// <para>This percent only affects the playback wait</para>
        /// </summary>
        public double AdjustLatencyPercent
        {
            get => _adjustLatencyPercent;
            set => _adjustLatencyPercent = value >= 0 && value <= 1
                ? value
                : throw new Exception("The percent value must be between 0 and 1");
        }

        /// <summary>
        /// Volume for this device 1.0 is full scale
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState => _playbackState;

        /// <summary>
        /// Gets playback state directly from sdl
        /// </summary>
        public PlaybackState SdlState
        {
            get
            {
                var status = Sdl2Interop.GetDeviceStatus(_deviceNumber);
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
        public WaveFormat OutputWaveFormat => _waveStream.WaveFormat;

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating what the format is actually using.
        /// <para>This property accessible after <see cref="Init(IWaveProvider)" method/></para>
        /// </summary>
        public WaveFormat ActualOutputWaveFormat { get; private set; }

        /// <summary>
        /// Audio conversion features
        /// </summary>
        public AudioConversion AudioConversion { get; set; }

        /// <summary>
        /// Approximate position value in milliseconds
        /// <para>Return zero if it exceeds the max value</para>
        /// </summary>
        public ulong Position
        {
            get
            {
                lock (_positionLock)
                {
                    return _position;
                }
            }
            private set
            {
                lock (_positionLock)
                {
                    _position = value;
                }
            }
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="waveProvider">WaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            if (_playbackState != PlaybackState.Stopped)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }
            _callbackEvent = new AutoResetEvent(false);
            _waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            var desiredSpec = new SDL_AudioSpec();
            desiredSpec.freq = waveProvider.WaveFormat.SampleRate;
            desiredSpec.format = GetAudioDataFormat();
            desiredSpec.channels = (byte)waveProvider.WaveFormat.Channels;
            desiredSpec.silence = 0;
            desiredSpec.samples = (ushort)bufferSize;
            var deviceName = Sdl2Interop.GetPlaybackDeviceName(DeviceId);
            var deviceNumber = Sdl2Interop.OpenPlaybackDevice(deviceName, ref desiredSpec, out var obtainedSpec, AudioConversion);
            var bitSize = Sdl2Interop.GetAudioFormatBitSize(obtainedSpec.format);
            ActualOutputWaveFormat = new WaveFormat(obtainedSpec.freq, bitSize, obtainedSpec.channels);
            _deviceNumber = deviceNumber;
        }

        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (_waveStream == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }
            if (_playbackState == PlaybackState.Stopped)
            {
                Resume();
                ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
            }
            else if (_playbackState == PlaybackState.Paused)
            {
                Resume();
            }
        }

        /// <summary>
        /// Stop the audio
        /// </summary>
        public void Stop()
        {
            if (_playbackState != PlaybackState.Stopped)
            {
                _playbackState = PlaybackState.Stopped;
                Sdl2Interop.StopPlaybackDevice(_deviceNumber);
                Sdl2Interop.ClosePlaybackDevice(_deviceNumber);
            }
        }

        /// <summary>
        /// Pause the audio
        /// </summary>
        public void Pause()
        {
            if (_playbackState == PlaybackState.Playing)
            {
                _playbackState = PlaybackState.Paused;
                Sdl2Interop.StopPlaybackDevice(_deviceNumber);
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
            if (disposing)
            {
                Stop();
            }
        }

        /// <summary>
        /// Resume playing
        /// </summary>
        private void Resume()
        {
            var status = Sdl2Interop.StartPlaybackDevice(_deviceNumber);
            if (status != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("Sdl failed to unpause playback device");
            _playbackState = PlaybackState.Playing;
            _callbackEvent.Set();
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
                _playbackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        /// <summary>
        /// Playback process
        /// </summary>
        private unsafe void DoPlayback()
        {
            while (_playbackState != PlaybackState.Stopped)
            {
                // workaround to get rid of stuttering
                // i assume on different hardware adjusting must be different
                // is it possible that callbacks will help?
                var adjustedLatency = DesiredLatency - (int)(DesiredLatency * AdjustLatencyPercent);
                if (!_callbackEvent.WaitOne(adjustedLatency))
                {
                    if (_playbackState == PlaybackState.Playing)
                    {
                        Debug.WriteLine("WARNING: WaveOutSdl callback event timeout");
                    }
                }

                var deviceStatus = Sdl2Interop.GetDeviceStatus(_deviceNumber);
                if (_playbackState == PlaybackState.Playing
                    && deviceStatus != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                {
                    throw new SdlException("WaveOutSdl playback unexpected finished");
                }

                unchecked 
                { 
                    Position += (ulong)DesiredLatency;
                }
                
                RaisePositionChangedEvent(Position);

                if (_playbackState == PlaybackState.Playing)
                {
                    uint frameSize = (uint)(DesiredLatency * OutputWaveFormat.AverageBytesPerSecond / 1000);
                    byte[] frameBuffer = new byte[frameSize];
                    var readSize = _waveStream.Read(frameBuffer, 0, frameBuffer.Length);

                    if (readSize == 0)
                    {
                        _playbackState = PlaybackState.Stopped;
                        _callbackEvent.Set();
                    }

                    fixed (byte* ptr = &frameBuffer[0])
                    {
                        var queue = Sdl2Interop.QueueAudio(_deviceNumber, (IntPtr)ptr, (uint)readSize);
                    }
                }
            }
        }

        /// <summary>
        /// Raise position changed event
        /// </summary>
        /// <param name="e"></param>
        private void RaisePositionChangedEvent(ulong position)
        {
            var handler = PositionChanged;
            if (handler != null)
            {
                if (_syncContext == null)
                {
                    handler(this, new PositionChangedEventArgs(position));
                }
                else
                {
                    _syncContext.Post(state => handler(this, new PositionChangedEventArgs(position)), null);
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
                if (_syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    _syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }
    }
}
