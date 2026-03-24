using System;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// An abstract base class for simplifying working with Media Foundation Transforms.
    /// You need to override the method that actually creates and configures the transform.
    /// </summary>
    public abstract class MediaFoundationTransform : IAudioSource, IDisposable
    {
        private const int DefaultInputChunkDurationMs = 100;

        /// <summary>
        /// The Source Provider
        /// </summary>
        protected readonly IAudioSource sourceProvider;
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
        /// Constructs a new MediaFoundationTransform wrapper.
        /// Uses a short input chunk size to balance latency and throughput.
        /// </summary>
        /// <param name="sourceProvider">The source provider for input data to the transform</param>
        /// <param name="outputFormat">The desired output format</param>
        public MediaFoundationTransform(IAudioSource sourceProvider, WaveFormat outputFormat)
        {
            this.outputWaveFormat = outputFormat;
            this.sourceProvider = sourceProvider;
            sourceBuffer = new byte[GetInputChunkSize(sourceProvider.WaveFormat)];
            outputBuffer = new byte[outputWaveFormat.AverageBytesPerSecond + outputWaveFormat.BlockAlign];
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
            transform.ProcessMessage(MftMessageType.Flush, IntPtr.Zero);
            transform.ProcessMessage(MftMessageType.NotifyBeginStreaming, IntPtr.Zero);
            transform.ProcessMessage(MftMessageType.NotifyStartOfStream, IntPtr.Zero);
            initializedForStreaming = true;
        }

        /// <summary>
        /// To be implemented by overriding classes. Create the transform object, set up its input and output types,
        /// and configure any custom properties in here.
        /// </summary>
        /// <returns>An object implementing IMFTransform</returns>
        private protected abstract IMFTransform CreateTransform();

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
        /// The output WaveFormat of this Media Foundation Transform
        /// </summary>
        public WaveFormat WaveFormat => outputWaveFormat;

        /// <summary>
        /// Reads data out of the source, passing it through the transform
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (transform == null)
            {
                transform = CreateTransform();
                InitializeTransformForStreaming();
            }

            int bytesWritten = 0;

            if (outputBufferCount > 0)
            {
                bytesWritten += ReadFromOutputBuffer(buffer);
            }

            while (bytesWritten < buffer.Length)
            {
                var sample = ReadFromSource();
                if (sample == null)
                {
                    EndStreamAndDrain();
                    bytesWritten += ReadFromOutputBuffer(buffer.Slice(bytesWritten));
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
                bytesWritten += ReadFromOutputBuffer(buffer.Slice(bytesWritten));
            }

            return bytesWritten;
        }

        private void EndStreamAndDrain()
        {
            transform.ProcessMessage(MftMessageType.NotifyEndOfStream, IntPtr.Zero);
            transform.ProcessMessage(MftMessageType.Drain, IntPtr.Zero);
            int read;
            do
            {
                read = ReadFromTransform();
            } while (read > 0);
            inputPosition = 0;
            outputPosition = 0;
            transform.ProcessMessage(MftMessageType.NotifyEndStreaming, IntPtr.Zero);
            initializedForStreaming = false;
        }

        private void ClearOutputBuffer()
        {
            outputBufferCount = 0;
            outputBufferOffset = 0;
        }

        private int ReadFromTransform()
        {
            var outputDataBuffer = new MftOutputDataBuffer[1];
            var sample = MediaFoundationApi.CreateSample();
            var pBuffer = MediaFoundationApi.CreateMemoryBuffer(outputBuffer.Length);
            IMFMediaBuffer outputMediaBuffer = null;
            bool outputBufferLocked = false;
            try
            {
                sample.AddBuffer(pBuffer);
                sample.SetSampleTime(outputPosition);
                outputDataBuffer[0].Sample = sample;

                var hr = transform.ProcessOutput(MftProcessOutputFlags.None,
                                                 1, outputDataBuffer, out MftProcessOutputStatus status);
                if (hr == MediaFoundationErrors.MF_E_TRANSFORM_NEED_MORE_INPUT)
                {
                    return 0;
                }
                MediaFoundationException.ThrowIfFailed(hr);

                outputDataBuffer[0].Sample.ConvertToContiguousBuffer(out outputMediaBuffer);
                outputMediaBuffer.Lock(out IntPtr pOutputBuffer, out _, out int outputBufferLength);
                outputBufferLocked = true;
                outputBuffer = BufferHelpers.Ensure(outputBuffer, outputBufferLength);
                Marshal.Copy(pOutputBuffer, outputBuffer, 0, outputBufferLength);
                outputBufferOffset = 0;
                outputBufferCount = outputBufferLength;
                outputPosition += BytesToNsPosition(outputBufferCount, WaveFormat);
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

                if (outputDataBuffer[0].Events != null)
                {
                    Marshal.ReleaseComObject(outputDataBuffer[0].Events);
                }

                var returnedSample = outputDataBuffer[0].Sample;
                if (returnedSample != null && !ReferenceEquals(returnedSample, sample))
                {
                    Marshal.ReleaseComObject(returnedSample);
                }

                sample.RemoveAllBuffers();
                Marshal.ReleaseComObject(sample);
                Marshal.ReleaseComObject(pBuffer);
            }
        }

        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            return (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
        }

        private IMFSample ReadFromSource()
        {
            int bytesRead = sourceProvider.Read(sourceBuffer.AsSpan());
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

        private int ReadFromOutputBuffer(Span<byte> destination)
        {
            int bytesFromOutputBuffer = Math.Min(destination.Length, outputBufferCount);
            outputBuffer.AsSpan(outputBufferOffset, bytesFromOutputBuffer).CopyTo(destination);
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
