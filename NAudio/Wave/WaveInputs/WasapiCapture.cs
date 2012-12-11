using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

// for consistency this should be in NAudio.Wave namespace, but left as it is for backwards compatibility
namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    public class WasapiCapture : IWaveIn
    {
        private const long REFTIMES_PER_SEC = 10000000;
        private const long REFTIMES_PER_MILLISEC = 10000;        
        private volatile bool stop;
        private byte[] recordBuffer;
        private Thread captureThread;
        private AudioClient audioClient;
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
        public WasapiCapture() : 
            this(GetDefaultCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        public WasapiCapture(MMDevice captureDevice)
        {
            this.audioClient = captureDevice.AudioClient;
            this.waveFormat = audioClient.MixFormat;
        }

        /// <summary>
        /// Recording wave format
        /// </summary>
        public virtual WaveFormat WaveFormat 
        {
            get { return this.waveFormat; }
            set { this.waveFormat = value; }
        }

        /// <summary>
        /// Gets the default audio capture device
        /// </summary>
        /// <returns>The default audio capture device</returns>
        public static MMDevice GetDefaultCaptureDevice()
        {
            MMDeviceEnumerator devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }

        private void InitializeCaptureDevice()
        {
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
        public void StartRecording()
        {
            InitializeCaptureDevice();
            ThreadStart start = delegate { this.CaptureThread(this.audioClient); };
            this.captureThread = new Thread(start);

            Debug.WriteLine("Thread starting...");
            this.stop = false;
            this.captureThread.Start();	
        }

        /// <summary>
        /// Stop Recording
        /// </summary>
        public void StopRecording()
        {
            if (this.captureThread != null)
            {
                this.stop = true;

                Debug.WriteLine("Thread ending...");

                // wait for thread to end
                this.captureThread.Join();
                this.captureThread = null;

                Debug.WriteLine("Done.");

                this.stop = false;
            }
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
                // don't dispose - the AudioClient only gets disposed when WasapiCapture is disposed
            }

            RaiseRecordingStopped(exception);
            System.Diagnostics.Debug.WriteLine("stop wasapi");
        }

        private void DoRecording(AudioClient client)
        {
            Debug.WriteLine(client.BufferSize);
            int bufferFrameCount = audioClient.BufferSize;

            // Calculate the actual duration of the allocated buffer.
            long actualDuration = (long)((double)REFTIMES_PER_SEC *
                             bufferFrameCount / WaveFormat.SampleRate);
            int sleepMilliseconds = (int)(actualDuration / REFTIMES_PER_MILLISEC / 2);

            AudioCaptureClient capture = client.AudioCaptureClient;
            client.Start();
            Debug.WriteLine(string.Format("sleep: {0} ms", sleepMilliseconds));
            while (!this.stop)
            {
                Thread.Sleep(sleepMilliseconds);
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
            if (audioClient != null)
            {
                audioClient.Dispose();
                audioClient = null;
            }
        }
    }
}
