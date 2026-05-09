using System;
using NAudio.CoreAudioApi;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WASAPI Loopback Capture
    /// based on a contribution from "Pygmy" - http://naudio.codeplex.com/discussions/203605
    /// </summary>
    public class WasapiLoopbackCapture : WasapiCapture
    {
        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiLoopbackCapture() :
            this(GetDefaultLoopbackCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        public WasapiLoopbackCapture(MMDevice captureDevice) :
            base(captureDevice)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiLoopbackCapture"/> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        public WasapiLoopbackCapture(MMDevice captureDevice, bool useEventSync) :
            base(captureDevice, useEventSync)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiLoopbackCapture"/> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
        public WasapiLoopbackCapture(MMDevice captureDevice, bool useEventSync, int audioBufferMillisecondsLength) :
            base(captureDevice, useEventSync, audioBufferMillisecondsLength)
        {
        }

        /// <summary>
        /// Gets the default audio loopback capture device
        /// </summary>
        /// <returns>The default audio loopback capture device</returns>
        public static MMDevice GetDefaultLoopbackCaptureDevice()
        {
            MMDeviceEnumerator devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        
        /// <summary>
        /// Specify loopback
        /// </summary>
        protected override AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return AudioClientStreamFlags.Loopback | base.GetAudioClientStreamFlags();
        }        
    }
}
