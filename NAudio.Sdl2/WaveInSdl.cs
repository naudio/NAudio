using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Sdl2.Structures;
using NAudio.Sdl2.Interop;
using NAudio.Wave;
using static NAudio.Sdl2.Interop.SDL;

// ReSharper disable once CheckNamespace
namespace NAudio.Sdl2
{
    /// <summary>
    /// WaveIn provider via SDL2 backend
    /// </summary>
    public class WaveInSdl : IWaveIn
    {
        private readonly SynchronizationContext syncContext;
        private uint deviceNumber;
        private volatile CaptureState captureState;
        private float peakLevel;
        private object peakLevelLock;
        private ushort frameSize;
        private uint frameSizeDoubled;
        private byte[] frameBuffer;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Prepares a wave input device for recording
        /// </summary>
        public WaveInSdl()
        {
            syncContext = SynchronizationContext.Current;
            captureState = CaptureState.Stopped;
            peakLevelLock = new object();
            DeviceId = -1;
            WaveFormat = new WaveFormat(44100, 16, 1);
            AudioConversion = AudioConversion.None;
            BufferMilliseconds = 100;
        }

        /// <summary>
        /// Returns the number of WaveInSdl devices available in the system
        /// </summary>
        public static int DeviceCount => SdlBindingWrapper.GetRecordingDevicesNumber();

        /// <summary>
        /// Retrieves the capabilities of a WaveInSdl device
        /// </summary>
        /// <param name="deviceId">Device to test</param>
        /// <returns>The WaveInSdl device capabilities</returns>
        /// <remarks>
        /// This function only returns DeviceNumber and DeviceName on versions below SDL 2.0.16
        /// <para>Use the <see cref="WaveInSdlCapabilities.IsAudioCapabilitiesValid"/> property to check if all capabilities are available</para>
        /// </remarks>
        public static WaveInSdlCapabilities GetCapabilities(int deviceId)
        {
            var deviceName = SdlBindingWrapper.GetRecordingDeviceName(deviceId);
            var runtimeSdlVersion = SdlBindingWrapper.GetRuntimeSdlVersion();
            var currentVersion = new Version(runtimeSdlVersion.major, runtimeSdlVersion.minor, runtimeSdlVersion.patch);
            var minimumRequiredVersion = new Version(2, 0, 16);
            if (currentVersion >= minimumRequiredVersion)
            {
                var deviceAudioSpec = SdlBindingWrapper.GetRecordingDeviceAudioSpec(deviceId);
                var deviceBitSize = SdlBindingWrapper.GetAudioFormatBitSize(deviceAudioSpec.format);
                return new WaveInSdlCapabilities
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

            return new WaveInSdlCapabilities
            {
                DeviceNumber = deviceId,
                DeviceName = deviceName,
                IsAudioCapabilitiesValid = false
            };
        }

        /// <summary>
        /// Retrieves the capabilities list of a WaveInSdl devices
        /// </summary>
        /// <returns>The WaveInSdlCapabilities list</returns>
        /// <remarks>
        /// This function only returns DeviceNumber and DeviceName on versions below SDL 2.0.16
        /// <para>Use the <see cref="WaveInSdlCapabilities.IsAudioCapabilitiesValid"/> property to check if all capabilities are available</para>
        /// </remarks>
        public static List<WaveInSdlCapabilities> GetCapabilitiesList()
        {
            List<WaveInSdlCapabilities> list = new List<WaveInSdlCapabilities>();
            var deviceCount = WaveInSdl.DeviceCount;
            for (int index = 0; index < deviceCount; index++)
            {
                list.Add(GetCapabilities(index));
            }
            return list;
        }

        /// <summary>
        /// Retrieves the capabilities of a WaveInSdl default device
        /// </summary>
        /// <returns>The WaveInSdl default device capabilities</returns>
        /// <remarks>This function is available since SDL 2.24.0</remarks>
        public static WaveInSdlCapabilities GetDefaultDeviceCapabilities()
        {
            SdlBindingWrapper.GetRecordingDeviceDefaultAudioInfo(out var deviceName, out var deviceAudioSpec);
            var deviceBitSize = SdlBindingWrapper.GetAudioFormatBitSize(deviceAudioSpec.format);
            return new WaveInSdlCapabilities
            {
                DeviceNumber = -1,
                DeviceName = deviceName,
                Bits = deviceBitSize,
                Channels = deviceAudioSpec.channels,
                Format = deviceAudioSpec.format,
                Frequency = deviceAudioSpec.freq,
                Samples = deviceAudioSpec.samples,
                Silence = deviceAudioSpec.silence,
                Size = deviceAudioSpec.size
            };
        }

        /// <summary>
        /// Gets or sets the device id
        /// <para>This must be between -1 and <see cref="DeviceCount"/> - 1</para>
        /// <para>-1 means stick to default device</para>
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
        /// <remarks>
        /// <para>This property accessible after <see cref="StartRecording"/> call</para>
        /// <para>If the <see cref="AudioConversion"/> is set to <see cref="AudioConversion.None"/> then this is the same as <see cref="WaveFormat"/></para>
        /// </remarks>
        public WaveFormat ActualWaveFormat { get; private set; }

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
        /// WaveInSdl peak level
        /// </summary>
        public float PeakLevel
        {
            get
            {
                lock (peakLevelLock)
                {
                    return peakLevel;
                }
            }
            private set
            {
                lock (peakLevelLock)
                {
                    peakLevel = value;
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
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");
            deviceNumber = OpenWaveInSdlDevice();
            SdlBindingWrapper.StartRecordingDevice(deviceNumber);
            captureState = CaptureState.Starting;
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
            {
                captureState = CaptureState.Stopping;
                SdlBindingWrapper.StopRecordingDevice(deviceNumber);
                SdlBindingWrapper.CloseRecordingDevice(deviceNumber);
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
                if (captureState != CaptureState.Stopped)
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
            frameSize = (ushort)(BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000);
            frameSizeDoubled = (uint)frameSize * 2;
            frameBuffer = new byte[frameSizeDoubled];
            var desiredSpec = new SDL_AudioSpec();
            desiredSpec.freq = WaveFormat.SampleRate;
            desiredSpec.format = GetAudioDataFormat();
            desiredSpec.channels = (byte)WaveFormat.Channels;
            desiredSpec.silence = 0;
            desiredSpec.samples = frameSize;
            var deviceName = SdlBindingWrapper.GetRecordingDeviceName(DeviceId);
            var openDeviceNumber = SdlBindingWrapper.OpenRecordingDevice(deviceName, ref desiredSpec, out var obtainedSpec, AudioConversion);
            var bitSize = SdlBindingWrapper.GetAudioFormatBitSize(obtainedSpec.format);
            ActualWaveFormat = new WaveFormat(obtainedSpec.freq, bitSize, obtainedSpec.channels);
            return openDeviceNumber;
        }

        /// <summary>
        /// Returns the audio format guessed by <see cref="WaveFormat.BitsPerSample"/>
        /// </summary>
        /// <returns>Audio format</returns>
        private ushort GetAudioDataFormat()
        {
            if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                return AUDIO_F32SYS;
            }

            switch (WaveFormat.BitsPerSample)
            {
                case 8:
                    return AUDIO_S8;
                case 16:
                    return AUDIO_S16SYS;
                case 32:
                    return AUDIO_S32SYS;
                default:
                    throw new SdlException("Unsupported bit depth for audio format");
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
                captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        /// <summary>
        /// Recording process
        /// </summary>
        private unsafe void DoRecording()
        {
            captureState = CaptureState.Capturing;
            while (captureState == CaptureState.Capturing)
            {
                uint size = 0;
                do
                {
                    size = SdlBindingWrapper.GetQueuedAudioSize(deviceNumber);
                    if (size >= frameSizeDoubled)
                    {
                        byte[] buffer = frameSize != 0 ? frameBuffer : new byte[size];
                        Array.Clear(buffer, 0, buffer.Length);

                        fixed (byte* ptr = &buffer[0])
                        {
                            uint recordedSize = SdlBindingWrapper.DequeueAudio(deviceNumber, (IntPtr)ptr, (uint)buffer.Length);
                            DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, (int)recordedSize));
                            PeakLevel = GetPeakLevel(buffer);
                        }

                        size -= (uint)buffer.Length;
                    }
                } while (size >= frameSize);
            }
        }

        /// <summary>
        /// Returns peak level
        /// </summary>
        /// <param name="buffer">Buffer from which peak is calculated</param>
        /// <returns>Peak level</returns>
        private float GetPeakLevel(byte[] buffer)
        {
            // It will always work or not ?
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

