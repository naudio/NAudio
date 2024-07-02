using System;
using NAudio.Sdl2.Structures;
using static NAudio.Sdl2.Interop.SDL;

namespace NAudio.Sdl2.Interop
{
    public static class SdlBindingWrapper
    {
        #region Recording Device

        /// <summary>
        /// Gets the name and preferred format of the default audio recording device
        /// </summary>
        /// <param name="deviceName">Device name</param>
        /// <param name="audioSpec">Audio spec</param>
        /// <returns></returns>
        public static int GetRecordingDeviceDefaultAudioInfo(out string deviceName, out SDL_AudioSpec audioSpec)
        {
            return GetDefaultAudioInfo(out deviceName, out audioSpec, Device.Capture);
        }

        /// <summary>
        /// Get the number of built-in audio recording devices
        /// </summary>
        /// <returns>Indexes of available devices</returns>
        public static int GetRecordingDevicesNumber()
        {
            return GetDevicesNumber(Device.Capture);
        }

        /// <summary>
        /// Get the human-readable name of a specific audio recording device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Device name</returns>
        public static string GetRecordingDeviceName(int deviceId)
        {
            return GetDeviceName(deviceId, Device.Capture);
        }

        /// <summary>
        /// Get the preferred audio format of a specific audio recording device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Audio spec</returns>
        /// <exception cref="SdlException"></exception>
        public static SDL_AudioSpec GetRecordingDeviceAudioSpec(int deviceId)
        {
            return GetDeviceAudioSpec(deviceId, Device.Capture);
        }

        /// <summary>
        /// Open a specific audio device
        /// </summary>
        /// <param name="deviceName">Device name</param>
        /// <param name="desiredSpec">Desired output format</param>
        /// <param name="obtainedSpec">Actual output format</param>
        /// <param name="audioConversion">Enabled conversion features</param>
        /// <returns>Device number
        /// <para>This DeviceNumber and DeviceId is not interchangeable</para>
        /// </returns>
        /// <exception cref="SdlException"></exception>
        public static uint OpenRecordingDevice(
            string deviceName,
            ref SDL_AudioSpec desiredSpec,
            out SDL_AudioSpec obtainedSpec,
            AudioConversion audioConversion)
        {
            return OpenDevice(deviceName, Device.Capture, ref desiredSpec, out obtainedSpec, audioConversion);
        }

        /// <summary>
        /// <para>Shuts down audio processing and closes the audio device</para>
        /// <para>This function may block briefly while pending audio data is played by the hardware,
        /// so that applications don't drop the last buffer of data they supplied</para>
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static void CloseRecordingDevice(uint deviceNumber)
        {
            CloseDevice(deviceNumber);
        }

        /// <summary>
        /// Starts the audio recording
        /// </summary>
        /// <param name="deviceNumber">Opened device number</param>
        public static SDL_AudioStatus StartRecordingDevice(uint deviceNumber)
        {
            var status = PauseAudioDevice(deviceNumber, Pause.Off);
            if (status != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("Failed to start recording device");
            return status;
        }

        /// <summary>
        /// Stop the audio recording
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static SDL_AudioStatus StopRecordingDevice(uint deviceNumber)
        {
            var status = PauseAudioDevice(deviceNumber, Pause.On);
            if (status == SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("Failed to stop recording device");
            return status;
        }

        /// <summary>
        /// Returns the number of bytes of queued audio
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        /// <returns>Number of bytes (not samples)</returns>
        public static uint GetQueuedAudioSize(uint deviceNumber)
        {
            InitSdl();
            return SDL_GetQueuedAudioSize(deviceNumber);
        }

        /// <summary>
        /// Dequeue more audio
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        /// <param name="dataBufferPtr">A pointer into where audio data should be copied</param>
        /// <param name="dataBufferLength">The number of bytes (not samples!) to which (data) points</param>
        /// <returns>Number of bytes dequeued, which could be less than requested</returns>
        public static uint DequeueAudio(uint deviceNumber, IntPtr dataBufferPtr, uint dataBufferLength)
        {
            InitSdl();
            var deviceStatus = GetDeviceStatus(deviceNumber);
            if (deviceStatus != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("The recording device stopped unexpectedly");
            return SDL_DequeueAudio(deviceNumber, dataBufferPtr, dataBufferLength);
        }

        #endregion

        #region Playback Device

        /// <summary>
        /// Adjust volume of the playback device
        /// </summary>
        /// <param name="destination">The destination for the mixed audio</param>
        /// <param name="source">The source audio buffer to be mixed</param>
        /// <param name="audioFormat">the SDL_AudioFormat structure representing the desired audio format</param>
        /// <param name="length">The length of the audio buffer in bytes</param>
        /// <param name="volumeFloat">Audio volume ranges from 0 to 128</param>
        /// <exception cref="SdlException"></exception>
        public static void ChangePlaybackDeviceVolume(byte[] destination, byte[] source, ushort audioFormat, uint length, float volumeFloat)
        {
            var volume = (int)(volumeFloat * 100);
            if (volume < 0 || volume > 128)
                throw new SdlException("The playback device volume must be between 0 and 1.28");
            InitSdl();
            SDL_MixAudioFormat(destination, source, audioFormat, length, volume);
        }

        /// <summary>
        /// Gets the name and preferred format of the default audio playback device
        /// </summary>
        /// <param name="deviceName">Device name</param>
        /// <param name="audioSpec">Audio spec</param>
        /// <returns></returns>
        public static int GetPlaybackDeviceDefaultAudioInfo(out string deviceName, out SDL_AudioSpec audioSpec)
        {
            return GetDefaultAudioInfo(out deviceName, out audioSpec, Device.Playback);
        }

        /// <summary>
        /// Get the number of built-in audio playback devices
        /// </summary>
        /// <returns>Indexes of available devices</returns>
        public static int GetPlaybackDevicesNumber()
        {
            return GetDevicesNumber(Device.Playback);
        }

        /// <summary>
        /// Get the human-readable name of a specific audio playback device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Device name</returns>
        public static string GetPlaybackDeviceName(int deviceId)
        {
            return GetDeviceName(deviceId, Device.Playback);
        }

        /// <summary>
        /// Get the preferred audio format of a specific audio playback device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Audio spec</returns>
        /// <exception cref="SdlException"></exception>
        public static SDL_AudioSpec GetPlaybackDeviceAudioSpec(int deviceId)
        {
            return GetDeviceAudioSpec(deviceId, Device.Playback);
        }

        /// <summary>
        /// Open a playback audio device
        /// </summary>
        /// <param name="deviceName">Device name</param>
        /// <param name="desiredSpec">Desired output format</param>
        /// <param name="obtainedSpec">Actual output format</param>
        /// <param name="audioConversion">Enabled conversion features</param>
        /// <returns>Device number
        /// <para>This DeviceNumber and DeviceId is not interchangeable</para>
        /// </returns>
        /// <exception cref="SdlException"></exception>
        public static uint OpenPlaybackDevice(
            string deviceName,
            ref SDL_AudioSpec desiredSpec,
            out SDL_AudioSpec obtainedSpec,
            AudioConversion audioConversion)
        {
            return OpenDevice(deviceName, Device.Playback, ref desiredSpec, out obtainedSpec, audioConversion);
        }
                        

        /// <summary>
        /// <para>Shuts down audio processing and closes the audio device</para>
        /// <para>This function may block briefly while pending audio data is played by the hardware,
        /// so that applications don't drop the last buffer of data they supplied</para>
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static void ClosePlaybackDevice(uint deviceNumber)
        {
            CloseDevice(deviceNumber);
        }

        /// <summary>
        /// Starts the audio playback
        /// </summary>
        /// <param name="deviceNumber">Opened device number</param>
        public static SDL_AudioStatus StartPlaybackDevice(uint deviceNumber)
        {
            var status = PauseAudioDevice(deviceNumber, Pause.Off);
            if (status != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("Failed to start playback device");
            return status;
        }

        /// <summary>
        /// Stop the audio playback
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static SDL_AudioStatus StopPlaybackDevice(uint deviceNumber)
        {
            var status = PauseAudioDevice(deviceNumber, Pause.On);
            if (status == SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("Failed to stop playback device");
            return status;
        }

        /// <summary>
        /// Queue more audio
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        /// <param name="dataPtr">Pointer to the data to queue</param>
        /// <param name="length">The number of bytes (not samples!) to which dataPtr points</param>
        /// <returns></returns>
        public static int QueueAudio(uint deviceNumber, IntPtr dataPtr, uint length)
        {
            InitSdl();
            var deviceStatus = GetDeviceStatus(deviceNumber);
            if (deviceStatus != SDL_AudioStatus.SDL_AUDIO_PLAYING)
                throw new SdlException("The playback device stopped unexpectedly");
            var queue = SDL_QueueAudio(deviceNumber, dataPtr, length);
            if (queue == -1) 
                throw new SdlException(SDL_GetError());
            return queue;
        }

        /// <summary>
        /// Drop any queued audio data waiting to be sent to the hardware
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static void ClearQueuedAudio(uint deviceNumber)
        {
            InitSdl();
            SDL_ClearQueuedAudio(deviceNumber);
        }
        #endregion

        #region Shared
        private static bool _isInitialized;

        /// <summary>
        /// Determines the version of compiled SDL
        /// </summary>
        /// <returns></returns>
        public static SDL_version GetCompiledSdlVersion()
        {
            InitSdl();
            SDL_VERSION(out var version);
            return version;
        }

        /// <summary>
        /// Determines the version of SDL at runtime
        /// </summary>
        /// <returns></returns>
        public static SDL_version GetRuntimeSdlVersion()
        {
            InitSdl();
            SDL_GetVersion(out var version);
            return version;
        }

        /// <summary>
        /// Get the bit size of specific audio format
        /// </summary>
        /// <param name="audioFormat">Audio format</param>
        /// <returns>Bit size</returns>
        public static ushort GetAudioFormatBitSize(ushort audioFormat)
        {
            InitSdl();
            return SDL_AUDIO_BITSIZE(audioFormat);
        }

        /// <summary>
        /// Get current audio state of an audio device
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <returns></returns>
        public static SDL_AudioStatus GetDeviceStatus(uint deviceNumber)
        {
            InitSdl();
            return SDL_GetAudioDeviceStatus(deviceNumber);
        }

        /// <summary>
        /// Get the name and preferred format of the default audio device
        /// </summary>
        /// <param name="deviceName">Device name</param>
        /// <param name="spec">Audio spec</param>
        /// <param name="isCapture">Is recording device</param>
        /// <returns></returns>
        private static int GetDefaultAudioInfo(out string deviceName, out SDL_AudioSpec spec, int isCapture)
        {
            InitSdl();
            var audioInfo = SDL_GetDefaultAudioInfo(out deviceName, out spec, isCapture);
            if (audioInfo != 0)
                throw new SdlException(SDL_GetError());
            return audioInfo;
        }

        /// <summary>
        /// Pause and unpause audio playback on a specified device
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        /// <param name="pause">Pause state</param>
        private static SDL_AudioStatus PauseAudioDevice(uint deviceNumber, int pause)
        {
            InitSdl();
            SDL_PauseAudioDevice(deviceNumber, pause);
            var status = GetDeviceStatus(deviceNumber);
            return status;
        }

        /// <summary>
        /// Get the number of built-in audio devices
        /// </summary>
        /// <returns>Indexes of available devices</returns>
        private static int GetDevicesNumber(int isCapture)
        {
            InitSdl();
            return SDL_GetNumAudioDevices(isCapture);
        }

        /// <summary>
        /// Get the human-readable name of a specific audio device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <param name="isCapture">Is recording device</param>
        /// <returns>Device name</returns>
        private static string GetDeviceName(int deviceId, int isCapture)
        {
            InitSdl();
            return SDL_GetAudioDeviceName(deviceId, isCapture);
        }

        /// <summary>
        /// Get the preferred audio format of a specific audio device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <param name="isCapture">Is recording device</param>
        /// <returns>Audio spec</returns>
        /// <exception cref="SdlException"></exception>
        private static SDL_AudioSpec GetDeviceAudioSpec(int deviceId, int isCapture)
        {
            InitSdl();
            var specResult = SDL_GetAudioDeviceSpec(deviceId, isCapture, out var spec);
            if (specResult != 0) 
                throw new SdlException(SDL_GetError());
            return spec;
        }

        /// <summary>
        /// Open a specific audio device
        /// </summary>
        /// <param name="deviceName">Device name</param>
        /// <param name="isCapture">Is recording device</param>
        /// <param name="desiredSpec">Desired output format</param>
        /// <param name="obtainedSpec">Actual output format</param>
        /// <param name="audioConversion">Enabled conversion features</param>
        /// <returns>Device number
        /// <para>This DeviceNumber and DeviceId is not interchangeable</para>
        /// </returns>
        /// <exception cref="SdlException"></exception>
        private static uint OpenDevice(string deviceName, int isCapture, ref SDL_AudioSpec desiredSpec, out SDL_AudioSpec obtainedSpec, AudioConversion audioConversion)
        {
            InitSdl();
            var deviceId = SDL_OpenAudioDevice(deviceName, isCapture, ref desiredSpec, out obtainedSpec, (int)audioConversion);
            if (deviceId <= 0) 
                throw new SdlException(SDL_GetError());
            return deviceId;
        }

        /// <summary>
        /// <para>Shuts down audio processing and closes the audio device</para>
        /// <para>This function may block briefly while pending audio data is played by the hardware,
        /// so that applications don't drop the last buffer of data they supplied</para>
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        private static void CloseDevice(uint deviceNumber)
        {
            InitSdl();
            SDL_CloseAudioDevice(deviceNumber);
        }

        /// <summary>
        /// Initializing SDL2
        /// </summary>
        /// <exception cref="SdlException"></exception>
        private static void InitSdl()
        {
            if (_isInitialized) 
                return;
            var init = SDL_Init(SDL_INIT_AUDIO | SDL_INIT_TIMER);
            if (init != 0) 
                throw new SdlException(SDL_GetError());
            _isInitialized = true;
        }
        #endregion
    }
}
