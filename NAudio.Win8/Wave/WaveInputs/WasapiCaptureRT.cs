using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.Win8.Wave.WaveOutputs;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace NAudio.Wave
{
    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    public class WasapiCaptureRT : IWaveIn
    {
        private const long REFTIMES_PER_SEC = 10000000;
        private const long REFTIMES_PER_MILLISEC = 10000;
        private volatile bool stop;
        private byte[] recordBuffer;
        private readonly string device;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private bool initialized;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiCaptureRT() : 
            this(GetDefaultCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="device">Capture device to use</param>
        public WasapiCaptureRT(string device)
        {
            this.device = device;
            //this.waveFormat = audioClient.MixFormat;
        }

        /// <summary>
        /// Recording wave format
        /// </summary>
        public virtual WaveFormat WaveFormat 
        {
            get { return waveFormat; }
            set { waveFormat = value; }
        }

        /// <summary>
        /// Way of enumerating all the audio capture devices available on the system
        /// </summary>
        /// <returns></returns>
        public async static Task<IEnumerable<DeviceInformation>> GetCaptureDevices()
        {
            var audioCaptureSelector = MediaDevice.GetAudioCaptureSelector();

            // (a PropertyKey)
            var supportsEventDrivenMode = "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e} 7";

            var captureDevices = await DeviceInformation.FindAllAsync(audioCaptureSelector, new[] { supportsEventDrivenMode } );
            return captureDevices;
        }

        /// <summary>
        /// Gets the default audio capture device
        /// </summary>
        /// <returns>The default audio capture device</returns>
        public static string GetDefaultCaptureDevice()
        {
            var defaultCaptureDeviceId = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default);
            return defaultCaptureDeviceId;
        }

        private async Task<AudioClient> Activate()
        {
            var icbh = new ActivateAudioInterfaceCompletionHandler(
                ac2 =>
                    {
                        InitializeCaptureDevice((IAudioClient)ac2);
                        /*var wfx = new WaveFormat(44100, 16, 2);
                    int hr = ac2.Initialize(AudioClientShareMode.Shared,
                                AudioClientStreamFlags.None, 
                                //AudioClientStreamFlags.EventCallback | AudioClientStreamFlags.NoPersist,
                                10000000, 0, wfx, IntPtr.Zero);
                    Marshal.ThrowExceptionForHR(hr);*/
                    });
            var IID_IAudioClient2 = new Guid("726778CD-F60A-4eda-82DE-E47610CD78AA");
            IActivateAudioInterfaceAsyncOperation activationOperation;
            NativeMethods.ActivateAudioInterfaceAsync(device, IID_IAudioClient2, IntPtr.Zero, icbh, out activationOperation);
            var audioClient2 = await icbh;
            return new AudioClient((IAudioClient)audioClient2);
        }

        private void InitializeCaptureDevice(IAudioClient audioClientInterface)
        {
            var audioClient = new AudioClient((IAudioClient)audioClientInterface);
            this.waveFormat = audioClient.MixFormat;

            if (initialized)
                return;

            long requestedDuration = REFTIMES_PER_MILLISEC * 100;

            
            if (!audioClient.IsFormatSupported(AudioClientShareMode.Shared, WaveFormat))
            {
                throw new ArgumentException("Unsupported Wave Format");
            }
            
            var streamFlags = GetAudioClientStreamFlags();

            audioClient.Initialize(AudioClientShareMode.Shared,
                streamFlags,
                requestedDuration,
                0,
                this.waveFormat,
                Guid.Empty);

            int bufferFrameCount = audioClient.BufferSize;
            this.bytesPerFrame = this.waveFormat.Channels * this.waveFormat.BitsPerSample / 8;
            this.recordBuffer = new byte[bufferFrameCount * bytesPerFrame];
            Debug.WriteLine(string.Format("record buffer size = {0}", this.recordBuffer.Length));

            initialized = true;
        }

        /// <summary>
        /// To allow overrides to specify different flags (e.g. loopback)
        /// </summary>
        protected virtual AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return AudioClientStreamFlags.None;
        }

        /// <summary>
        /// Start Recording
        /// </summary>
        public async void StartRecording()
        {
            this.stop = false;
            var audioClient = await Activate();
            Task.Run(() => CaptureThread(audioClient));
            
            /*Task.Run(
                async () =>
                    {
                        var audioClient = await Activate();
                        //InitializeCaptureDevice(audioClient); - now done in the activate callback
                        CaptureThread(audioClient);
                    });*/

            Debug.WriteLine("Thread starting...");

        }

        /// <summary>
        /// Stop Recording
        /// </summary>
        public void StopRecording()
        {
            this.stop = true;
            // todo: wait for thread to end
            // todo: could signal the event
        }

        private void CaptureThread(AudioClient client)
        {
            Exception exception = null;
            try
            {
                DoRecording(client);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                client.Stop();
                client.Dispose();
            }

            RaiseRecordingStopped(exception);
            Debug.WriteLine("stop wasapi");
        }

        private void DoRecording(AudioClient client)
        {
            Debug.WriteLine(client.BufferSize);
            int bufferFrameCount = client.BufferSize;

            // Calculate the actual duration of the allocated buffer.
            long actualDuration = (long)((double)REFTIMES_PER_SEC *
                             bufferFrameCount / WaveFormat.SampleRate);
            int sleepMilliseconds = (int)(actualDuration / REFTIMES_PER_MILLISEC / 2);

            AudioCaptureClient capture = client.AudioCaptureClient;
            client.Start();
            Debug.WriteLine(string.Format("sleep: {0} ms", sleepMilliseconds));
            while (!this.stop)
            {
                Task.Delay(sleepMilliseconds);
                ReadNextPacket(capture);
            }
        }

        private void RaiseRecordingStopped(Exception exception)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                handler(this, new StoppedEventArgs(exception));
            }
        }

        private void ReadNextPacket(AudioCaptureClient capture)
        {
            IntPtr buffer;
            int framesAvailable;
            AudioClientBufferFlags flags;
            int packetSize = capture.GetNextPacketSize();
            int recordBufferOffset = 0;
            //Debug.WriteLine(string.Format("packet size: {0} samples", packetSize / 4));

            while (packetSize != 0)
            {
                buffer = capture.GetBuffer(out framesAvailable, out flags);

                int bytesAvailable = framesAvailable * bytesPerFrame;

                // apparently it is sometimes possible to read more frames than we were expecting?
                // fix suggested by Michael Feld:
                int spaceRemaining = Math.Max(0, recordBuffer.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable && recordBufferOffset > 0)
                {
                    if (DataAvailable != null) DataAvailable(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
                    recordBufferOffset = 0;
                }

                // if not silence...
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);
                }
                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }
            if (DataAvailable != null)
            {
                DataAvailable(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            StopRecording();
        }
    }
}
