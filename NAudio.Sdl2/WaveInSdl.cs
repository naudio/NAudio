using System;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Sdl2.Structures;
using NAudio.Sdl2.Interop;
using NAudio.Wave;
using static SDL2.SDL;

// ReSharper disable once CheckNamespace
namespace NAudio.Sdl2
{
    /// <summary>
    /// Recording using SDL2 backend
    /// </summary>
    public class WaveInSdl : IWaveIn
    {
        private readonly SynchronizationContext _syncContext;
        private uint _deviceNumber;
        private volatile CaptureState _captureState;
        private float _peakLevel;
        private object _pealLevelLock = new object();

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        public WaveInSdl()
        {
            _syncContext = SynchronizationContext.Current;
            _captureState = CaptureState.Stopped;
            DeviceId = 0;
            WaveFormat = new WaveFormat(48000, 16, 1);
            AudioConversion = AudioConversion.None;
            BufferMilliseconds = 100;
        }

        /// <summary>
        /// Returns the number of WaveInSdl devices available in the system
        /// </summary>
        public static int DeviceCount => Sdl2Interop.GetRecordingDevicesNumber();

        /// <summary>
        /// Retrieves the capabilities of a WaveInSdl device
        /// </summary>
        /// <param name="deviceId">Device to test</param>
        /// <returns>The WaveInSdl device capabilities</returns>
        public static WaveInSdlCapabilities GetCapabilities(int deviceId)
        {
            var deviceName = Sdl2Interop.GetRecordingDeviceName(deviceId);
            var deviceSpec = Sdl2Interop.GetRecordingDeviceSpec(deviceId);
            var deviceBitSize = Sdl2Interop.GetAudioFormatBitSize(deviceSpec.format);
            return new WaveInSdlCapabilities
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
        /// The device id to use.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// WaveFormat we are expect recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// WaveFormat we are actually recording in
        /// </summary>
        public WaveFormat ActualWaveFormat { get; private set; }

        /// <summary>
        /// Audio conversion features
        /// </summary>
        public AudioConversion AudioConversion { get; set; }

        /// <summary>
        /// WaveInSdl peak level
        /// </summary>
        public float PeakLevel
        {
            get
            {
                lock (_pealLevelLock)
                {
                    return _peakLevel;
                }
            }
            private set
            {
                lock (_pealLevelLock)
                {
                    _peakLevel = value;
                }
            }
        }

        /// <summary>
        /// Gets recorder state directly from sdl
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
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            if (_captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");
            _deviceNumber = OpenWaveInSdlDevice();
            var status = Sdl2Interop.StartRecordingDevice(_deviceNumber);
            if (status != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("Sdl failed to unpause recording device");
            _captureState = CaptureState.Starting;
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (_captureState != CaptureState.Stopped)
            {
                _captureState = CaptureState.Stopping;
                Sdl2Interop.StopRecordingDevice(_deviceNumber);
                Sdl2Interop.CloseRecordingDevice(_deviceNumber);
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
                if (_captureState != CaptureState.Stopped)
                {
                    StopRecording();
                }
            }
        }

        /// <summary>
        /// Opens WaveInSdl device
        /// </summary>
        /// <returns></returns>
        private uint OpenWaveInSdlDevice()
        {
            var bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            var desiredSpec = new SDL_AudioSpec();
            desiredSpec.freq = WaveFormat.SampleRate;
            desiredSpec.format = GetAudioDataFormat();
            desiredSpec.channels = (byte)WaveFormat.Channels;
            desiredSpec.silence = 0;
            desiredSpec.samples = (ushort)bufferSize;
            var deviceName = Sdl2Interop.GetRecordingDeviceName(DeviceId);
            var deviceNumber = Sdl2Interop.OpenRecordingDevice(deviceName, ref desiredSpec, out var obtainedSpec, AudioConversion);
            var bitSize = Sdl2Interop.GetAudioFormatBitSize(obtainedSpec.format);
            ActualWaveFormat = new WaveFormat(obtainedSpec.freq, bitSize, obtainedSpec.channels);
            return deviceNumber;
        }

        /// <summary>
        /// Returns the audio format guessed by <see cref="WaveFormat.BitsPerSample"/>
        /// </summary>
        /// <returns>Audio format</returns>
        private ushort GetAudioDataFormat()
        {
            switch (WaveFormat.BitsPerSample)
            {
                case 8:
                    return AUDIO_S8;
                case 16:
                    return AUDIO_S16SYS;
                case 32:
                    return AUDIO_S32SYS;
                default:
                    return (ushort)WaveFormat.BitsPerSample;
            }
        }

        /// <summary>
        /// Thread at which recording is happen
        /// </summary>
        private void RecordThread()
        {
            Exception exception = null;
            try
            {
                DoRecording();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                _captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        /// <summary>
        /// Recording process
        /// </summary>
        private unsafe void DoRecording()
        {
            _captureState = CaptureState.Capturing;
            uint frameSize = (uint)(BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000);
            while (_captureState == CaptureState.Capturing)
            {
                uint size = 0;
                do
                {
                    var deviceStatus = Sdl2Interop.GetDeviceStatus(_deviceNumber);
                    if (_captureState == CaptureState.Capturing 
                        && deviceStatus != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                    {
                        throw new SdlException("WaveInSdl capturing unexpected finished");
                    }
                    size = Sdl2Interop.GetQueuedAudioSize(_deviceNumber);
                    if (size >= (frameSize * 2))
                    {
                        var bufferSize = frameSize != 0  
                            ? frameSize * 2 
                            : size;

                        byte[] buffer = new byte[bufferSize];

                        fixed (byte* ptr = &buffer[0])
                        {
                            uint recordedSize = Sdl2Interop.DequeueAudio(_deviceNumber, (IntPtr)ptr, bufferSize);
                            DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, (int)recordedSize));
                            PeakLevel = GetPeakLevel(buffer);
                        }

                        size -= bufferSize;
                    }
                } while (size >= frameSize);
            }
        }

        private float GetPeakLevel(byte[] buffer)
        {
            float max = buffer
                .Take((int)buffer.Length * 2)
                .Where((x, i) => i % 2 == 0)
                .Select((y, i) => BitConverter.ToInt16(buffer, i * 2) / 32768f)
                .Max();
            return max;
        }

        /// <summary>
        /// Raise recording stopped event
        /// </summary>
        /// <param name="e"></param>
        private void RaiseRecordingStoppedEvent(Exception e)
        {
            var handler = RecordingStopped;
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

