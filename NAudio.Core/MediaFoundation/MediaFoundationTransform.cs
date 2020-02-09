using System;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// An abstract base class for simplifying working with Media Foundation Transforms
    /// You need to override the method that actually creates and configures the transform
    /// </summary>
    public abstract class MediaFoundationTransform : IWaveProvider, IDisposable
    {
        /// <summary>
        /// The Source Provider
        /// </summary>
        protected readonly IWaveProvider sourceProvider;
        /// <summary>
        /// The Output WaveFormat
        /// </summary>
        protected readonly WaveFormat outputWaveFormat;
        private readonly byte[] sourceBuffer;
        
        private byte[] outputBuffer;
        private int outputBufferOffset;
        private int outputBufferCount;

        private IMFTransform transform;
        private bool disposed;
        private long inputPosition; // in ref-time, so we can timestamp the input samples
        private long outputPosition; // also in ref-time
        private bool initializedForStreaming;

        /// <summary>
        /// Constructs a new MediaFoundationTransform wrapper
        /// Will read one second at a time
        /// </summary>
        /// <param name="sourceProvider">The source provider for input data to the transform</param>
        /// <param name="outputFormat">The desired output format</param>
        public MediaFoundationTransform(IWaveProvider sourceProvider, WaveFormat outputFormat)
        {
            this.outputWaveFormat = outputFormat;
            this.sourceProvider = sourceProvider;
            sourceBuffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond];
            outputBuffer = new byte[outputWaveFormat.AverageBytesPerSecond + outputWaveFormat.BlockAlign]; // we will grow this buffer if needed, but try to make something big enough
        }

        private void InitializeTransformForStreaming()
        {
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_COMMAND_FLUSH, IntPtr.Zero);
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_BEGIN_STREAMING, IntPtr.Zero);
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_START_OF_STREAM, IntPtr.Zero);
            initializedForStreaming = true;
        }

        /// <summary>
        /// To be implemented by overriding classes. Create the transform object, set up its input and output types,
        /// and configure any custom properties in here
        /// </summary>
        /// <returns>An object implementing IMFTrasform</returns>
        protected abstract IMFTransform CreateTransform();

        /// <summary>
        /// Disposes this MediaFoundation transform
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (transform != null)
            {
                Marshal.ReleaseComObject(transform);
            }
        }

        /// <summary>
        /// Disposes this Media Foundation Transform
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MediaFoundationTransform()
        {
            Dispose(false);
        }

        /// <summary>
        /// The output WaveFormat of this Media Foundation Transform
        /// </summary>
        public WaveFormat WaveFormat { get { return outputWaveFormat; } }

        /// <summary>
        /// Reads data out of the source, passing it through the transform
        /// </summary>
        /// <param name="buffer">Output buffer</param>
        /// <param name="offset">Offset within buffer to write to</param>
        /// <param name="count">Desired byte count</param>
        /// <returns>Number of bytes read</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (transform == null)
            {
                transform = CreateTransform();
                InitializeTransformForStreaming();
            }

            // strategy will be to always read 1 second from the source, and give it to the resampler
            int bytesWritten = 0;
            
            // read in any leftovers from last time
            if (outputBufferCount > 0)
            {
                bytesWritten += ReadFromOutputBuffer(buffer, offset, count - bytesWritten);
            }

            while (bytesWritten < count)
            {
                var sample = ReadFromSource();
                if (sample == null) // reached the end of our input
                {
                    // be good citizens and send some end messages:
                    EndStreamAndDrain();
                    // resampler might have given us a little bit more to return
                    bytesWritten += ReadFromOutputBuffer(buffer, offset + bytesWritten, count - bytesWritten);
                    break;
                }

                // might need to resurrect the stream if the user has read all the way to the end,
                // and then repositioned the input backwards
                if (!initializedForStreaming)
                {
                    InitializeTransformForStreaming();
                }

                // give the input to the resampler
                // can get MF_E_NOTACCEPTING if we didn't drain the buffer properly
                transform.ProcessInput(0, sample, 0);

                Marshal.ReleaseComObject(sample);

                int readFromTransform;
                // n.b. in theory we ought to loop here, although we'd need to be careful as the next time into ReadFromTransform there could
                // still be some leftover bytes in outputBuffer, which would get overwritten. Only introduce this if we find a transform that 
                // needs it. For most transforms, alternating read/write should be OK
                //do
                //{
                // keep reading from transform
                readFromTransform = ReadFromTransform();
                bytesWritten += ReadFromOutputBuffer(buffer, offset + bytesWritten, count - bytesWritten);
                //} while (readFromTransform > 0);
            }

            return bytesWritten;
        }

        private void EndStreamAndDrain()
        {
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_END_OF_STREAM, IntPtr.Zero);
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_COMMAND_DRAIN, IntPtr.Zero);
            int read;
            do
            {
                read = ReadFromTransform();
            } while (read > 0);
            outputBufferCount = 0;
            outputBufferOffset = 0;
            inputPosition = 0;
            outputPosition = 0;
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_END_STREAMING, IntPtr.Zero);
            initializedForStreaming = false;
        }

        /// <summary>
        /// Attempts to read from the transform
        /// Some useful info here:
        /// http://msdn.microsoft.com/en-gb/library/windows/desktop/aa965264%28v=vs.85%29.aspx#process_data
        /// </summary>
        /// <returns></returns>
        private int ReadFromTransform()
        {
            var outputDataBuffer = new MFT_OUTPUT_DATA_BUFFER[1];
            // we have to create our own for
            var sample = MediaFoundationApi.CreateSample();
            var pBuffer = MediaFoundationApi.CreateMemoryBuffer(outputBuffer.Length);
            sample.AddBuffer(pBuffer);
            sample.SetSampleTime(outputPosition); // hopefully this is not needed
            outputDataBuffer[0].pSample = sample;
            
            _MFT_PROCESS_OUTPUT_STATUS status;
            var hr = transform.ProcessOutput(_MFT_PROCESS_OUTPUT_FLAGS.None, 
                                             1, outputDataBuffer, out status);
            if (hr == MediaFoundationErrors.MF_E_TRANSFORM_NEED_MORE_INPUT)
            {
                Marshal.ReleaseComObject(pBuffer);
                Marshal.ReleaseComObject(sample);
                // nothing to read
                return 0;
            }
            else if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            IMFMediaBuffer outputMediaBuffer;
            outputDataBuffer[0].pSample.ConvertToContiguousBuffer(out outputMediaBuffer);
            IntPtr pOutputBuffer;
            int outputBufferLength;
            int maxSize;
            outputMediaBuffer.Lock(out pOutputBuffer, out maxSize, out outputBufferLength);
            outputBuffer = BufferHelpers.Ensure(outputBuffer, outputBufferLength);
            Marshal.Copy(pOutputBuffer, outputBuffer, 0, outputBufferLength);
            outputBufferOffset = 0;
            outputBufferCount = outputBufferLength;
            outputMediaBuffer.Unlock();
            outputPosition += BytesToNsPosition(outputBufferCount, WaveFormat); // hopefully not needed
            Marshal.ReleaseComObject(pBuffer);
            sample.RemoveAllBuffers(); // needed to fix memory leak in some cases
            Marshal.ReleaseComObject(sample);
            Marshal.ReleaseComObject(outputMediaBuffer);
            return outputBufferLength;
        }
        
        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        private IMFSample ReadFromSource()
        {
            // we always read a full second
            int bytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBuffer.Length);
            if (bytesRead == 0) return null;

            var mediaBuffer = MediaFoundationApi.CreateMemoryBuffer(bytesRead);
            IntPtr pBuffer;
            int maxLength, currentLength;
            mediaBuffer.Lock(out pBuffer, out maxLength, out currentLength);
            Marshal.Copy(sourceBuffer, 0, pBuffer, bytesRead);
            mediaBuffer.Unlock();
            mediaBuffer.SetCurrentLength(bytesRead);

            var sample = MediaFoundationApi.CreateSample();
            sample.AddBuffer(mediaBuffer);
            // we'll set the time, I don't think it is needed for Resampler, but other MFTs might need it
            sample.SetSampleTime(inputPosition);
            long duration = BytesToNsPosition(bytesRead, sourceProvider.WaveFormat);
            sample.SetSampleDuration(duration);
            inputPosition += duration;
            Marshal.ReleaseComObject(mediaBuffer);
            return sample;
        }

        private int ReadFromOutputBuffer(byte[] buffer, int offset, int needed)
        {
            int bytesFromOutputBuffer = Math.Min(needed, outputBufferCount);
            Array.Copy(outputBuffer, outputBufferOffset, buffer, offset, bytesFromOutputBuffer);
            outputBufferOffset += bytesFromOutputBuffer;
            outputBufferCount -= bytesFromOutputBuffer;
            if (outputBufferCount == 0)
            {
                outputBufferOffset = 0;
            }
            return bytesFromOutputBuffer;
        }

        /// <summary>
        /// Indicate that the source has been repositioned and completely drain out the transforms buffers
        /// </summary>
        public void Reposition()
        {
            if (initializedForStreaming)
            {
                EndStreamAndDrain();
                InitializeTransformForStreaming();
            }
        }
    }
}