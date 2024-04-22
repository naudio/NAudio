using System;
using NAudio.Sdl2.Structures;
using SDL2;
using static SDL2.SDL;

namespace NAudio.Sdl2.Interop
{
    public static class Sdl2Interop
    {
        #region Recording Device

        /// <summary>
        /// Get the number of built-in audio recording devices
        /// </summary>
        /// <returns>Indexes of available devices</returns>
        public static int GetRecordingDevicesNumber() => GetDevicesNumber(Device.Capture);

        /// <summary>
        /// Get the human-readable name of a specific audio recording device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Device name</returns>
        public static string GetRecordingDeviceName(int deviceId) => GetDeviceName(deviceId, Device.Capture);

        /// <summary>
        /// Get the preferred audio format of a specific audio recording device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Audio spec</returns>
        /// <exception cref="SdlException"></exception>
        public static SDL_AudioSpec GetRecordingDeviceSpec(int deviceId) => GetDeviceSpec(deviceId, Device.Capture);

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
        public static uint OpenRecordingDevice(string deviceName, ref SDL_AudioSpec desiredSpec, out SDL_AudioSpec obtainedSpec, AudioConversion audioConversion) =>
            OpenDevice(deviceName, Device.Capture, ref desiredSpec, out obtainedSpec, audioConversion);

        /// <summary>
        /// <para>Shuts down audio processing and closes the audio device</para>
        /// <para>This function may block briefly while pending audio data is played by the hardware,
        /// so that applications don't drop the last buffer of data they supplied</para>
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static void CloseRecordingDevice(uint deviceNumber) => CloseDevice(deviceNumber);

        /// <summary>
        /// Starts the audio recording
        /// </summary>
        /// <param name="deviceNumber">Opened device number</param>
        public static SDL_AudioStatus StartRecordingDevice(uint deviceNumber) => PauseAudioDevice(deviceNumber, Pause.Off);
        
        /// <summary>
        /// Stop the audio recording
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static SDL_AudioStatus StopRecordingDevice(uint deviceNumber) => PauseAudioDevice(deviceNumber, Pause.On);

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
            return SDL_DequeueAudio(deviceNumber, dataBufferPtr, dataBufferLength);
        }

        #endregion

        #region Playback Device
        /// <summary>
        /// Get the number of built-in audio playback devices
        /// </summary>
        /// <returns>Indexes of available devices</returns>
        public static int GetPlaybackDevicesNumber() => GetDevicesNumber(Device.Playback);

        /// <summary>
        /// Get the human-readable name of a specific audio playback device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Device name</returns>
        public static string GetPlaybackDeviceName(int deviceId) => GetDeviceName(deviceId, Device.Playback);

        /// <summary>
        /// Get the preferred audio format of a specific audio playback device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <returns>Audio spec</returns>
        /// <exception cref="SdlException"></exception>
        public static SDL_AudioSpec GetPlaybackDeviceSpec(int deviceId) => GetDeviceSpec(deviceId, Device.Playback);

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
        public static uint OpenPlaybackDevice(string deviceName, ref SDL_AudioSpec desiredSpec, out SDL_AudioSpec obtainedSpec, AudioConversion audioConversion) =>
                        OpenDevice(deviceName, Device.Playback, ref desiredSpec, out obtainedSpec, audioConversion);

        /// <summary>
        /// <para>Shuts down audio processing and closes the audio device</para>
        /// <para>This function may block briefly while pending audio data is played by the hardware,
        /// so that applications don't drop the last buffer of data they supplied</para>
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static void ClosePlaybackDevice(uint deviceNumber) => CloseDevice(deviceNumber);

        /// <summary>
        /// Starts the audio playback
        /// </summary>
        /// <param name="deviceNumber">Opened device number</param>
        public static SDL_AudioStatus StartPlaybackDevice(uint deviceNumber) => PauseAudioDevice(deviceNumber, Pause.Off);

        /// <summary>
        /// Stop the audio playback
        /// </summary>
        /// <param name="deviceNumber">Device number</param>
        public static SDL_AudioStatus StopPlaybackDevice(uint deviceNumber) => PauseAudioDevice(deviceNumber, Pause.Off);

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
            var queue = SDL_QueueAudio(deviceNumber, dataPtr, length);
            if (queue == -1)
                ThrowSdlError();
            return queue;
        }
        #endregion

        #region Shared
        private static bool _isInitialized;

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

        public static SDL_AudioStatus GetDeviceStatus(uint deviceNumber)
        {
            InitSdl();
            return SDL_GetAudioDeviceStatus(deviceNumber);
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
        /// <returns>Audio spec</returns>
        /// <exception cref="SdlException"></exception>
        private static SDL_AudioSpec GetDeviceSpec(int deviceId, int isCapture)
        {
            InitSdl();
            var specResult = SDL_GetAudioDeviceSpec(deviceId, isCapture, out var spec);
            if (specResult != 0)
                ThrowSdlError();
            return spec;
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
        private static uint OpenDevice(string deviceName, int isCapture, ref SDL_AudioSpec desiredSpec, out SDL_AudioSpec obtainedSpec, AudioConversion audioConversion)
        {
            InitSdl();
            var deviceId = SDL_OpenAudioDevice(deviceName, isCapture, ref desiredSpec, out obtainedSpec, (int)audioConversion);
            if (deviceId <= 0)
                ThrowSdlError();
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

        private static void ThrowSdlError()
        {
            throw new SdlException(SDL_GetError());
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
                ThrowSdlError();
            _isInitialized = true;
        }
        #endregion
    }
}
