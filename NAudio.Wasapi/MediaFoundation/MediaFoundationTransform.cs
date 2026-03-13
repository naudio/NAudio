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
        private const int DefaultInputChunkDurationMs = 100;

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
        /// Uses a short input chunk size to balance latency and throughput
        /// </summary>
        /// <param name="sourceProvider">The source provider for input data to the transform</param>
        /// <param name="outputFormat">The desired output format</param>
        public MediaFoundationTransform(IWaveProvider sourceProvider, WaveFormat outputFormat)
        {
            this.outputWaveFormat = outputFormat;
            this.sourceProvider = sourceProvider;
            sourceBuffer = new byte[GetInputChunkSize(sourceProvider.WaveFormat)];
            outputBuffer = new byte[outputWaveFormat.AverageBytesPerSecond + outputWaveFormat.BlockAlign]; // we will grow this buffer if needed, but try to make something big enough
        }

        private static int GetInputChunkSize(WaveFormat waveFormat)
        {
            var blockAlign = Math.Max(1, waveFormat.BlockAlign);
            var bytesPerChunk = (waveFormat.AverageBytesPerSecond * DefaultInputChunkDurationMs) / 1000;
            bytesPerChunk -= bytesPerChunk % blockAlign;
            if (bytesPerChunk < blockAlign)
            {
                bytesPerChunk = blockAlign;
            }
            return bytesPerChunk;
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
                transform = null;
                initializedForStreaming = false;
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
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (transform == null)
            {
                transform = CreateTransform();
                InitializeTransformForStreaming();
            }

            int bytesWritten = 0;

            if (outputBufferCount > 0)
            {
                bytesWritten += ReadFromOutputBuffer(buffer, offset, count - bytesWritten);
            }

            while (bytesWritten < count)
            {
                var sample = ReadFromSource();
                if (sample == null)
                {
                    EndStreamAndDrain();
                    bytesWritten += ReadFromOutputBuffer(buffer, offset + bytesWritten, count - bytesWritten);
                    ClearOutputBuffer();
                    break;
                }

                if (!initializedForStreaming)
                {
                    InitializeTransformForStreaming();
                }

                try
                {
                    transform.ProcessInput(0, sample, 0);
                }
                finally
                {
                    Marshal.ReleaseComObject(sample);
                }

                ReadFromTransform();
                bytesWritten += ReadFromOutputBuffer(buffer, offset + bytesWritten, count - bytesWritten);
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
            inputPosition = 0;
            outputPosition = 0;
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_END_STREAMING, IntPtr.Zero);
            initializedForStreaming = false;
        }

        private void ClearOutputBuffer()
        {
            outputBufferCount = 0;
            outputBufferOffset = 0;
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
            var sample = MediaFoundationApi.CreateSample();
            var pBuffer = MediaFoundationApi.CreateMemoryBuffer(outputBuffer.Length);
            IMFMediaBuffer outputMediaBuffer = null;
            bool outputBufferLocked = false;
            try
            {
                sample.AddBuffer(pBuffer);
                sample.SetSampleTime(outputPosition); // hopefully this is not needed
                outputDataBuffer[0].pSample = sample;

                var hr = transform.ProcessOutput(_MFT_PROCESS_OUTPUT_FLAGS.None,
                                                 1, outputDataBuffer, out _MFT_PROCESS_OUTPUT_STATUS status);
                if (hr == MediaFoundationErrors.MF_E_TRANSFORM_NEED_MORE_INPUT)
                {
                    return 0;
                }
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                outputDataBuffer[0].pSample.ConvertToContiguousBuffer(out outputMediaBuffer);
                outputMediaBuffer.Lock(out IntPtr pOutputBuffer, out _, out int outputBufferLength);
                outputBufferLocked = true;
                outputBuffer = BufferHelpers.Ensure(outputBuffer, outputBufferLength);
                Marshal.Copy(pOutputBuffer, outputBuffer, 0, outputBufferLength);
                outputBufferOffset = 0;
                outputBufferCount = outputBufferLength;
                outputPosition += BytesToNsPosition(outputBufferCount, WaveFormat); // hopefully not needed
                return outputBufferLength;
            }
            finally
            {
                if (outputMediaBuffer != null)
                {
                    if (outputBufferLocked)
                    {
                        outputMediaBuffer.Unlock();
                    }
                    Marshal.ReleaseComObject(outputMediaBuffer);
                }

                if (outputDataBuffer[0].pEvents != null)
                {
                    Marshal.ReleaseComObject(outputDataBuffer[0].pEvents);
                }

                var returnedSample = outputDataBuffer[0].pSample;
                if (returnedSample != null && !ReferenceEquals(returnedSample, sample))
                {
                    Marshal.ReleaseComObject(returnedSample);
                }

                sample.RemoveAllBuffers(); // needed to fix memory leak in some cases
                Marshal.ReleaseComObject(sample);
                Marshal.ReleaseComObject(pBuffer);
            }
        }
        
        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        private IMFSample ReadFromSource()
        {
            int bytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBuffer.Length);
            if (bytesRead == 0) return null;

            var mediaBuffer = MediaFoundationApi.CreateMemoryBuffer(bytesRead);
            bool bufferLocked = false;
            try
            {
                mediaBuffer.Lock(out IntPtr pBuffer, out _, out _);
                bufferLocked = true;
                Marshal.Copy(sourceBuffer, 0, pBuffer, bytesRead);
                mediaBuffer.Unlock();
                bufferLocked = false;
                mediaBuffer.SetCurrentLength(bytesRead);

                var sample = MediaFoundationApi.CreateSample();
                sample.AddBuffer(mediaBuffer);
                sample.SetSampleTime(inputPosition);
                long duration = BytesToNsPosition(bytesRead, sourceProvider.WaveFormat);
                sample.SetSampleDuration(duration);
                inputPosition += duration;
                return sample;
            }
            finally
            {
                if (bufferLocked)
                {
                    mediaBuffer.Unlock();
                }
                Marshal.ReleaseComObject(mediaBuffer);
            }
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
                ClearOutputBuffer();
                InitializeTransformForStreaming();
            }
        }
    }
}