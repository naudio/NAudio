using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// An abstract base class for simplifying working with Media Foundation Transforms.
    /// You need to override the method that actually creates and configures the transform.
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

        private Interfaces.IMFTransform transform;
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
        public MediaFoundationTransform(IWaveProvider sourceProvider, WaveFormat outputFormat)
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
            MediaFoundationException.ThrowIfFailed(transform.ProcessMessage((int)MftMessageType.Flush, IntPtr.Zero));
            MediaFoundationException.ThrowIfFailed(transform.ProcessMessage((int)MftMessageType.NotifyBeginStreaming, IntPtr.Zero));
            MediaFoundationException.ThrowIfFailed(transform.ProcessMessage((int)MftMessageType.NotifyStartOfStream, IntPtr.Zero));
            initializedForStreaming = true;
        }

        /// <summary>
        /// To be implemented by overriding classes. Create the transform object, set up its input and output types,
        /// and configure any custom properties in here.
        /// </summary>
        /// <returns>An object implementing IMFTransform</returns>
        private protected abstract Interfaces.IMFTransform CreateTransform();

        /// <summary>
        /// Disposes this MediaFoundation transform
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (transform != null)
            {
                ((ComObject)(object)transform).FinalRelease();
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
                var (samplePtr, sample) = ReadFromSource();
                if (samplePtr == IntPtr.Zero)
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
                    MediaFoundationException.ThrowIfFailed(transform.ProcessInput(0, samplePtr, 0));
                }
                finally
                {
                    ((ComObject)(object)sample).FinalRelease();
                    Marshal.Release(samplePtr);
                }

                ReadFromTransform();
                bytesWritten += ReadFromOutputBuffer(buffer.Slice(bytesWritten));
            }

            return bytesWritten;
        }

        private void EndStreamAndDrain()
        {
            MediaFoundationException.ThrowIfFailed(transform.ProcessMessage((int)MftMessageType.NotifyEndOfStream, IntPtr.Zero));
            MediaFoundationException.ThrowIfFailed(transform.ProcessMessage((int)MftMessageType.Drain, IntPtr.Zero));
            int read;
            do
            {
                read = ReadFromTransform();
            } while (read > 0);
            inputPosition = 0;
            outputPosition = 0;
            MediaFoundationException.ThrowIfFailed(transform.ProcessMessage((int)MftMessageType.NotifyEndStreaming, IntPtr.Zero));
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
            var (samplePtr, sample) = MediaFoundationApi.CreateSample();
            var (pBufferPtr, pBuffer) = MediaFoundationApi.CreateMemoryBuffer(outputBuffer.Length);
            IntPtr returnedSamplePtr = IntPtr.Zero;
            Interfaces.IMFSample returnedSample = null;
            bool returnedSampleIsReplacement = false;
            IntPtr outputMediaBufferPtr = IntPtr.Zero;
            Interfaces.IMFMediaBuffer outputMediaBuffer = null;
            bool outputBufferLocked = false;
            try
            {
                MediaFoundationException.ThrowIfFailed(sample.AddBuffer(pBufferPtr));
                MediaFoundationException.ThrowIfFailed(sample.SetSampleTime(outputPosition));
                outputDataBuffer[0].Sample = samplePtr;

                int hr;
                GCHandle handle = GCHandle.Alloc(outputDataBuffer, GCHandleType.Pinned);
                try
                {
                    // Fourth out param is MFT_PROCESS_OUTPUT_STATUS (NewStreams flag); unused here.
                    // The per-buffer outputDataBuffer[0].Status is updated by the MFT through the
                    // pinned pointer.
                    hr = transform.ProcessOutput((int)MftProcessOutputFlags.None,
                                                 1, handle.AddrOfPinnedObject(), out _);
                }
                finally
                {
                    handle.Free();
                }

                returnedSamplePtr = outputDataBuffer[0].Sample;
                returnedSampleIsReplacement = (returnedSamplePtr != IntPtr.Zero && returnedSamplePtr != samplePtr);
                returnedSample = returnedSampleIsReplacement
                    ? (Interfaces.IMFSample)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        returnedSamplePtr, CreateObjectFlags.UniqueInstance)
                    : sample;

                if (hr == MediaFoundationErrors.MF_E_TRANSFORM_NEED_MORE_INPUT)
                {
                    return 0;
                }
                MediaFoundationException.ThrowIfFailed(hr);

                MediaFoundationException.ThrowIfFailed(returnedSample.ConvertToContiguousBuffer(out outputMediaBufferPtr));
                outputMediaBuffer = (Interfaces.IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    outputMediaBufferPtr, CreateObjectFlags.UniqueInstance);
                MediaFoundationException.ThrowIfFailed(outputMediaBuffer.Lock(out IntPtr pOutputBuffer, out _, out int outputBufferLength));
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
                        MediaFoundationException.ThrowIfFailed(outputMediaBuffer.Unlock());
                    }
                    ((ComObject)(object)outputMediaBuffer).FinalRelease();
                }
                if (outputMediaBufferPtr != IntPtr.Zero)
                {
                    Marshal.Release(outputMediaBufferPtr);
                }

                IntPtr eventsPtr = outputDataBuffer[0].Events;
                if (eventsPtr != IntPtr.Zero)
                {
                    Marshal.Release(eventsPtr);
                }

                if (returnedSampleIsReplacement && returnedSample != null)
                {
                    ((ComObject)(object)returnedSample).FinalRelease();
                    Marshal.Release(returnedSamplePtr);
                }

                MediaFoundationException.ThrowIfFailed(sample.RemoveAllBuffers());
                ((ComObject)(object)sample).FinalRelease();
                Marshal.Release(samplePtr);
                ((ComObject)(object)pBuffer).FinalRelease();
                Marshal.Release(pBufferPtr);
            }
        }

        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            return (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
        }

        private (IntPtr Ptr, Interfaces.IMFSample Rcw) ReadFromSource()
        {
            int bytesRead = sourceProvider.Read(sourceBuffer.AsSpan());
            if (bytesRead == 0) return (IntPtr.Zero, null);

            var (mediaBufferPtr, mediaBuffer) = MediaFoundationApi.CreateMemoryBuffer(bytesRead);
            bool bufferLocked = false;
            try
            {
                MediaFoundationException.ThrowIfFailed(mediaBuffer.Lock(out IntPtr pBuffer, out _, out _));
                bufferLocked = true;
                Marshal.Copy(sourceBuffer, 0, pBuffer, bytesRead);
                MediaFoundationException.ThrowIfFailed(mediaBuffer.Unlock());
                bufferLocked = false;
                MediaFoundationException.ThrowIfFailed(mediaBuffer.SetCurrentLength(bytesRead));

                var (samplePtr, sample) = MediaFoundationApi.CreateSample();
                MediaFoundationException.ThrowIfFailed(sample.AddBuffer(mediaBufferPtr));
                MediaFoundationException.ThrowIfFailed(sample.SetSampleTime(inputPosition));
                long duration = BytesToNsPosition(bytesRead, sourceProvider.WaveFormat);
                MediaFoundationException.ThrowIfFailed(sample.SetSampleDuration(duration));
                inputPosition += duration;
                return (samplePtr, sample);
            }
            finally
            {
                if (bufferLocked)
                {
                    MediaFoundationException.ThrowIfFailed(mediaBuffer.Unlock());
                }
                ((ComObject)(object)mediaBuffer).FinalRelease();
                Marshal.Release(mediaBufferPtr);
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
