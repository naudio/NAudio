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
            BufferMilliseconds = 100;
            SdlBindingWrapper.Initialize();
        }

        /// <summary>
        /// Returns the number of WaveInSdl devices available in the system
        /// </summary>
        public static int DeviceCount
        {
            get
            {
                try
                {
                    SdlBindingWrapper.Initialize();
                    var deviceCount = SdlBindingWrapper.GetRecordingDevicesNumber();
                    return deviceCount;
                }
                finally
                {
                    SdlBindingWrapper.Terminate();
                }
            }
        }

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
            try
            {
                SdlBindingWrapper.Initialize();
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
            finally
            {
                SdlBindingWrapper.Terminate();
            }
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
            try
            {
                SdlBindingWrapper.Initialize();
                List<WaveInSdlCapabilities> list = new List<WaveInSdlCapabilities>();
                var deviceCount = WaveInSdl.DeviceCount;
                for (int index = 0; index < deviceCount; index++)
                {
                    list.Add(GetCapabilities(index));
                }
                return list;
            }
            finally
            {
                SdlBindingWrapper.Terminate();
            }
        }

        /// <summary>
        /// Retrieves the capabilities of a WaveInSdl default device
        /// </summary>
        /// <returns>The WaveInSdl default device capabilities</returns>
        /// <remarks>This function is available since SDL 2.24.0</remarks>
        public static WaveInSdlCapabilities GetDefaultDeviceCapabilities()
        {
            try
            {
                SdlBindingWrapper.Initialize();
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
                    Size = deviceAudioSpec.size,
                    IsAudioCapabilitiesValid = true
                };
            }
            finally
            {
                SdlBindingWrapper.Terminate();
            }
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
        /// Capture State
        /// </summary>
        public CaptureState CaptureState => captureState;

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
                try
                {
                    if (captureState != CaptureState.Stopped)
                    {
                        StopRecording();
                    }
                }
                catch
                {
                }
            }
            try
            {
                SdlBindingWrapper.Terminate();
            }
            catch
            {
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
            var openDeviceNumber = SdlBindingWrapper.OpenRecordingDevice(deviceName, ref desiredSpec, out var obtainedSpec);
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
                            PeakLevel = GetPeakLevel(buffer, (int)recordedSize);
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
        /// <param name="bytesRecorded">Number of bytes recorded</param>
        /// <returns>Peak level</returns>
        private float GetPeakLevel(byte[] buffer, int bytesRecorded)
        {
            // Is this correct at least for 4 bytes aligned buffer bound?
            float max = 0;
            var waveBuffer = new WaveBuffer(buffer);
            if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int i = 4; i < bytesRecorded / 4; i += 4)
                {
                    var sample1 = waveBuffer.FloatBuffer[i - 4];
                    var sample2 = waveBuffer.FloatBuffer[i - 3];
                    var sample3 = waveBuffer.FloatBuffer[i - 2];
                    var sample4 = waveBuffer.FloatBuffer[i - 1];
                    var sample = Math.Max(Math.Abs(sample1), Math.Max(Math.Abs(sample2), Math.Max(Math.Abs(sample3), Math.Abs(sample4))));
                    max = Math.Max(sample, max);
                }
            }
            else
            {
                for (int i = 2; i < bytesRecorded / 2; i += 2)
                {
                    var sample1 = waveBuffer.ShortBuffer[i - 2] / 32768f;
                    var sample2 = waveBuffer.ShortBuffer[i - 1] / 32768f;
                    var sample = Math.Max(Math.Abs(sample1), Math.Abs(sample2));
                    max = Math.Max(sample, max);
                }
            }
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

