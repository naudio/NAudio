using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFTransform providing managed access to Media Foundation transforms.
    /// </summary>
    public class MfTransform : IDisposable
    {
        private Interfaces.IMFTransform transformInterface;
        private IntPtr nativePointer;

        internal MfTransform(Interfaces.IMFTransform transformInterface, IntPtr nativePointer)
        {
            this.transformInterface = transformInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Gets the native COM pointer for this transform (for passing to other COM methods).
        /// </summary>
        internal IntPtr NativePointer => nativePointer;

        /// <summary>
        /// Retrieves the minimum and maximum number of input and output streams.
        /// </summary>
        /// <param name="inputMinimum">Minimum number of input streams.</param>
        /// <param name="inputMaximum">Maximum number of input streams.</param>
        /// <param name="outputMinimum">Minimum number of output streams.</param>
        /// <param name="outputMaximum">Maximum number of output streams.</param>
        public void GetStreamLimits(out int inputMinimum, out int inputMaximum, out int outputMinimum, out int outputMaximum)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetStreamLimits(out inputMinimum, out inputMaximum, out outputMinimum, out outputMaximum));
        }

        /// <summary>
        /// Retrieves the current number of input and output streams.
        /// </summary>
        /// <param name="inputStreams">Number of input streams.</param>
        /// <param name="outputStreams">Number of output streams.</param>
        public void GetStreamCount(out int inputStreams, out int outputStreams)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetStreamCount(out inputStreams, out outputStreams));
        }

        /// <summary>
        /// Gets the buffer requirements and other information for an input stream.
        /// </summary>
        /// <param name="inputStreamId">The input stream identifier.</param>
        /// <returns>The input stream info.</returns>
        public MftInputStreamInfo GetInputStreamInfo(int inputStreamId)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetInputStreamInfo(inputStreamId, out var streamInfo));
            return streamInfo;
        }

        /// <summary>
        /// Gets the buffer requirements and other information for an output stream.
        /// </summary>
        /// <param name="outputStreamId">The output stream identifier.</param>
        /// <returns>The output stream info.</returns>
        public MftOutputStreamInfo GetOutputStreamInfo(int outputStreamId)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetOutputStreamInfo(outputStreamId, out var streamInfo));
            return streamInfo;
        }

        /// <summary>
        /// Gets an available media type for an input stream.
        /// </summary>
        /// <param name="inputStreamId">The input stream identifier.</param>
        /// <param name="typeIndex">The media type index.</param>
        /// <returns>The available media type.</returns>
        public MfMediaType GetInputAvailableType(int inputStreamId, int typeIndex)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetInputAvailableType(inputStreamId, typeIndex, out var mediaTypePtr));
            var mediaTypeInterface = (Interfaces.IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(mediaTypePtr, CreateObjectFlags.UniqueInstance);
            return new MfMediaType(mediaTypeInterface, mediaTypePtr);
        }

        /// <summary>
        /// Gets an available media type for an output stream.
        /// </summary>
        /// <param name="outputStreamId">The output stream identifier.</param>
        /// <param name="typeIndex">The media type index.</param>
        /// <returns>The available media type.</returns>
        public MfMediaType GetOutputAvailableType(int outputStreamId, int typeIndex)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetOutputAvailableType(outputStreamId, typeIndex, out var mediaTypePtr));
            var mediaTypeInterface = (Interfaces.IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(mediaTypePtr, CreateObjectFlags.UniqueInstance);
            return new MfMediaType(mediaTypeInterface, mediaTypePtr);
        }

        /// <summary>
        /// Sets the media type for an input stream.
        /// </summary>
        /// <param name="inputStreamId">The input stream identifier.</param>
        /// <param name="mediaType">The media type to set.</param>
        /// <param name="flags">Optional flags.</param>
        public void SetInputType(int inputStreamId, MfMediaType mediaType, MftSetTypeFlags flags = 0)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.SetInputType(inputStreamId, mediaType.NativePointer, (int)flags));
        }

        /// <summary>
        /// Sets the media type for an output stream.
        /// </summary>
        /// <param name="outputStreamId">The output stream identifier.</param>
        /// <param name="mediaType">The media type to set.</param>
        /// <param name="flags">Optional flags.</param>
        public void SetOutputType(int outputStreamId, MfMediaType mediaType, MftSetTypeFlags flags = 0)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.SetOutputType(outputStreamId, mediaType.NativePointer, (int)flags));
        }

        /// <summary>
        /// Gets the current media type for an input stream.
        /// </summary>
        /// <param name="inputStreamId">The input stream identifier.</param>
        /// <returns>The current input media type.</returns>
        public MfMediaType GetInputCurrentType(int inputStreamId)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetInputCurrentType(inputStreamId, out var mediaTypePtr));
            var mediaTypeInterface = (Interfaces.IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(mediaTypePtr, CreateObjectFlags.UniqueInstance);
            return new MfMediaType(mediaTypeInterface, mediaTypePtr);
        }

        /// <summary>
        /// Gets the current media type for an output stream.
        /// </summary>
        /// <param name="outputStreamId">The output stream identifier.</param>
        /// <returns>The current output media type.</returns>
        public MfMediaType GetOutputCurrentType(int outputStreamId)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetOutputCurrentType(outputStreamId, out var mediaTypePtr));
            var mediaTypeInterface = (Interfaces.IMFMediaType)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(mediaTypePtr, CreateObjectFlags.UniqueInstance);
            return new MfMediaType(mediaTypeInterface, mediaTypePtr);
        }

        /// <summary>
        /// Queries whether an input stream can accept more data.
        /// </summary>
        /// <param name="inputStreamId">The input stream identifier.</param>
        /// <returns>The input status flags.</returns>
        public MftInputStatusFlags GetInputStatus(int inputStreamId)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetInputStatus(inputStreamId, out var flags));
            return (MftInputStatusFlags)flags;
        }

        /// <summary>
        /// Queries whether the MFT is ready to produce output data.
        /// </summary>
        /// <returns>The output status flags.</returns>
        public MftOutputStatusFlags GetOutputStatus()
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.GetOutputStatus(out var flags));
            return (MftOutputStatusFlags)flags;
        }

        /// <summary>
        /// Sends a message to the MFT.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="param">Message parameter.</param>
        public void ProcessMessage(MftMessageType messageType, IntPtr param)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.ProcessMessage((int)messageType, param));
        }

        /// <summary>
        /// Delivers data to an input stream.
        /// </summary>
        /// <param name="inputStreamId">The input stream identifier.</param>
        /// <param name="sample">The input sample.</param>
        public void ProcessInput(int inputStreamId, MfSample sample)
        {
            MediaFoundationException.ThrowIfFailed(transformInterface.ProcessInput(inputStreamId, sample.NativePointer, 0));
        }

        /// <summary>
        /// Generates output from the current input data.
        /// </summary>
        /// <param name="flags">Process output flags.</param>
        /// <param name="outputDataBuffer">Pointer to an array of MFT_OUTPUT_DATA_BUFFER structures, allocated by the caller.</param>
        /// <param name="outputBufferCount">Number of elements in the output buffer array.</param>
        /// <param name="status">Receives status flags.</param>
        /// <returns>The HRESULT, since callers often need to check for MF_E_TRANSFORM_NEED_MORE_INPUT.</returns>
        public int ProcessOutput(MftProcessOutputFlags flags, int outputBufferCount, IntPtr outputDataBuffer, out int status)
        {
            return transformInterface.ProcessOutput((int)flags, outputBufferCount, outputDataBuffer, out status);
        }

        /// <summary>
        /// Finalizer — runs only if Dispose was not called. Releases the native IntPtr ref;
        /// the source-generated RCW has its own ComObject finalizer that releases its ref
        /// independently.
        /// </summary>
        ~MfTransform() => Dispose(disposing: false);

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the native IntPtr ref unconditionally. When called from
        /// <see cref="Dispose()"/> (disposing=true) also calls <c>FinalRelease</c> on the
        /// RCW; the finalizer path leaves the RCW alone because <c>ComObject</c> has its own
        /// finalizer with no defined ordering relative to ours.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            if (disposing && transformInterface != null)
            {
                ((ComObject)(object)transformInterface).FinalRelease();
                transformInterface = null;
            }
        }
    }
}
