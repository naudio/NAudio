using System;
using System.Collections.Generic;
using NAudio.Dmo.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Dmo
{
    /// <summary>
    /// Media Object
    /// </summary>
    public class MediaObject : IDisposable
    {
        private IMediaObject mediaObject;
        private readonly int inputStreams;
        private readonly int outputStreams;

        // Mirror of DMO_OUTPUT_DATA_BUFFER with a raw IUnknown* in place of the
        // managed IMediaBuffer reference, so it can be pinned and passed by
        // pointer to the modern IMediaObject.ProcessOutput signature.
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct RawDmoOutputDataBuffer
        {
            public IntPtr pBuffer;
            public DmoOutputDataBufferFlags dwStatus;
            public long rtTimestamp;
            public long referenceTimeDuration;
        }

        #region Construction

        /// <summary>
        /// Creates a new Media Object
        /// </summary>
        /// <param name="mediaObject">Media Object COM interface</param>
        internal MediaObject(IMediaObject mediaObject)
        {
            this.mediaObject = mediaObject;
            mediaObject.GetStreamCount(out inputStreams, out outputStreams);
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Number of input streams
        /// </summary>
        public int InputStreamCount => inputStreams;

        /// <summary>
        /// Number of output streams
        /// </summary>
        public int OutputStreamCount => outputStreams;
        #endregion

        #region Get Input and Output Types

        /// <summary>
        /// Gets the input media type for the specified input stream
        /// </summary>
        /// <param name="inputStream">Input stream index</param>
        /// <param name="inputTypeIndex">Input type index</param>
        /// <returns>DMO Media Type or null if there are no more input types</returns>
        public unsafe DmoMediaType? GetInputType(int inputStream, int inputTypeIndex)
        {
            DmoMediaType mediaType = default;
            int hresult = mediaObject.GetInputType(inputStream, inputTypeIndex, (IntPtr)(&mediaType));
            if (hresult == HResult.S_OK)
            {
                DmoInterop.MoFreeMediaType(ref mediaType);
                return mediaType;
            }
            if (hresult == (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
            {
                return null;
            }
            MediaFoundationException.ThrowIfFailed(hresult);
            return null;
        }

        /// <summary>
        /// Gets the DMO Media Output type
        /// </summary>
        /// <param name="outputStream">The output stream</param>
        /// <param name="outputTypeIndex">Output type index</param>
        /// <returns>DMO Media Type or null if no more available</returns>
        public unsafe DmoMediaType? GetOutputType(int outputStream, int outputTypeIndex)
        {
            DmoMediaType mediaType = default;
            int hresult = mediaObject.GetOutputType(outputStream, outputTypeIndex, (IntPtr)(&mediaType));
            if (hresult == HResult.S_OK)
            {
                DmoInterop.MoFreeMediaType(ref mediaType);
                return mediaType;
            }
            if (hresult == (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
            {
                return null;
            }
            MediaFoundationException.ThrowIfFailed(hresult);
            return null;
        }

        /// <summary>
        /// retrieves the media type that was set for an output stream, if any
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <returns>DMO Media Type or null if no more available</returns>
        public unsafe DmoMediaType GetOutputCurrentType(int outputStreamIndex)
        {
            DmoMediaType mediaType = default;
            int hresult = mediaObject.GetOutputCurrentType(outputStreamIndex, (IntPtr)(&mediaType));
            if (hresult == HResult.S_OK)
            {
                DmoInterop.MoFreeMediaType(ref mediaType);
                return mediaType;
            }
            if (hresult == (int)DmoHResults.DMO_E_TYPE_NOT_SET)
            {
                throw new InvalidOperationException("Media type was not set.");
            }
            throw Marshal.GetExceptionForHR(hresult);
        }

        /// <summary>
        /// Enumerates the supported input types
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <returns>Enumeration of input types</returns>
        public IEnumerable<DmoMediaType> GetInputTypes(int inputStreamIndex)
        {
            int typeIndex = 0;
            DmoMediaType? mediaType;
            while ((mediaType = GetInputType(inputStreamIndex, typeIndex)) != null)
            {
                yield return mediaType.Value;
                typeIndex++;
            }
        }

        /// <summary>
        /// Enumerates the output types
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <returns>Enumeration of supported output types</returns>
        public IEnumerable<DmoMediaType> GetOutputTypes(int outputStreamIndex)
        {
            int typeIndex = 0;
            DmoMediaType? mediaType;
            while ((mediaType = GetOutputType(outputStreamIndex, typeIndex)) != null)
            {
                yield return mediaType.Value;
                typeIndex++;
            }
        }

        #endregion

        #region Set Input Type

        /// <summary>
        /// Querys whether a specified input type is supported
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <param name="mediaType">Media type to check</param>
        /// <returns>true if supports</returns>
        public bool SupportsInputType(int inputStreamIndex, DmoMediaType mediaType)
        {
            return SetInputType(inputStreamIndex, ref mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
        }

        private unsafe bool SetInputType(int inputStreamIndex, ref DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            int hResult;
            fixed (DmoMediaType* p = &mediaType)
            {
                hResult = mediaObject.SetInputType(inputStreamIndex, (IntPtr)p, (int)flags);
            }
            if (hResult != HResult.S_OK)
            {
                if (hResult == (int)DmoHResults.DMO_E_INVALIDSTREAMINDEX)
                {
                    throw new ArgumentException("Invalid stream index");
                }
                if (hResult == (int)DmoHResults.DMO_E_TYPE_NOT_ACCEPTED)
                {
                    Debug.WriteLine("Media type was not accepted");
                }

                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the input type
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <param name="mediaType">Media Type</param>
        public void SetInputType(int inputStreamIndex, DmoMediaType mediaType)
        {
            if (!SetInputType(inputStreamIndex, ref mediaType, DmoSetTypeFlags.None))
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Sets the input type to the specified Wave format
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <param name="waveFormat">Wave format</param>
        public void SetInputWaveFormat(int inputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool set = SetInputType(inputStreamIndex, ref mediaType, DmoSetTypeFlags.None);
            DmoInterop.MoFreeMediaType(ref mediaType);
            if (!set)
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Requests whether the specified Wave format is supported as an input
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <param name="waveFormat">Wave format</param>
        /// <returns>true if supported</returns>
        public bool SupportsInputWaveFormat(int inputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool supported = SetInputType(inputStreamIndex, ref mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
            DmoInterop.MoFreeMediaType(ref mediaType);
            return supported;
        }

        /// <summary>
        /// Helper function to make a DMO Media Type to represent a particular WaveFormat
        /// </summary>
        private DmoMediaType CreateDmoMediaTypeForWaveFormat(WaveFormat waveFormat)
        {
            DmoMediaType mediaType = new DmoMediaType();
            int waveFormatExSize = Marshal.SizeOf(waveFormat);  // 18 + waveFormat.ExtraSize;
            DmoInterop.MoInitMediaType(ref mediaType, waveFormatExSize);
            mediaType.SetWaveFormat(waveFormat);
            return mediaType;
        }

        #endregion

        #region Set Output Type

        /// <summary>
        /// Checks if a specified output type is supported
        /// n.b. you may need to set the input type first
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <param name="mediaType">Media type</param>
        /// <returns>True if supported</returns>
        public bool SupportsOutputType(int outputStreamIndex, DmoMediaType mediaType)
        {
            return SetOutputType(outputStreamIndex, ref mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
        }

        /// <summary>
        /// Tests if the specified Wave Format is supported for output
        /// n.b. may need to set the input type first
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <param name="waveFormat">Wave format</param>
        /// <returns>True if supported</returns>
        public bool SupportsOutputWaveFormat(int outputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool supported = SetOutputType(outputStreamIndex, ref mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
            DmoInterop.MoFreeMediaType(ref mediaType);
            return supported;
        }

        private unsafe bool SetOutputType(int outputStreamIndex, ref DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            int hresult;
            fixed (DmoMediaType* p = &mediaType)
            {
                hresult = mediaObject.SetOutputType(outputStreamIndex, (IntPtr)p, (int)flags);
            }
            if (hresult == (int)DmoHResults.DMO_E_TYPE_NOT_ACCEPTED)
            {
                return false;
            }
            if (hresult == HResult.S_OK)
            {
                return true;
            }
            throw Marshal.GetExceptionForHR(hresult);
        }

        /// <summary>
        /// Sets the output type
        /// n.b. may need to set the input type first
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <param name="mediaType">Media type to set</param>
        public void SetOutputType(int outputStreamIndex, DmoMediaType mediaType)
        {
            if (!SetOutputType(outputStreamIndex, ref mediaType, DmoSetTypeFlags.None))
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Set output type to the specified wave format
        /// n.b. may need to set input type first
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <param name="waveFormat">Wave format</param>
        public void SetOutputWaveFormat(int outputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool succeeded = SetOutputType(outputStreamIndex, ref mediaType, DmoSetTypeFlags.None);
            DmoInterop.MoFreeMediaType(ref mediaType);
            if (!succeeded)
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        #endregion

        #region Get Input and Output Size Info
        /// <summary>
        /// Get Input Size Info
        /// </summary>
        /// <param name="inputStreamIndex">Input Stream Index</param>
        /// <returns>Input Size Info</returns>
        public MediaObjectSizeInfo GetInputSizeInfo(int inputStreamIndex)
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.GetInputSizeInfo(inputStreamIndex, out int size, out int maxLookahead, out int alignment));
            return new MediaObjectSizeInfo(size, maxLookahead, alignment);
        }

        /// <summary>
        /// Get Output Size Info
        /// </summary>
        /// <param name="outputStreamIndex">Output Stream Index</param>
        /// <returns>Output Size Info</returns>
        public MediaObjectSizeInfo GetOutputSizeInfo(int outputStreamIndex)
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.GetOutputSizeInfo(outputStreamIndex, out int size, out int alignment));
            return new MediaObjectSizeInfo(size, 0, alignment);
        }

        #endregion

        #region Buffer Processing
        /// <summary>
        /// Process Input
        /// </summary>
        /// <param name="inputStreamIndex">Input Stream index</param>
        /// <param name="mediaBuffer">Media Buffer</param>
        /// <param name="flags">Flags</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="duration">Duration</param>
        public void ProcessInput(int inputStreamIndex, IMediaBuffer mediaBuffer, DmoInputDataBufferFlags flags,
            long timestamp, long duration)
        {
            // Source-generated CCWs use per-interface tearoffs, so the IUnknown*
            // returned by GetOrCreateComInterfaceForObject is NOT the same pointer
            // as the IMediaBuffer* the native DMO expects. We must QI explicitly
            // before passing to ProcessInput, otherwise native dereferences a
            // mismatched vtable and crashes the process.
            IntPtr unknown = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(
                mediaBuffer, CreateComInterfaceFlags.None);
            try
            {
                Marshal.ThrowExceptionForHR(
                    Marshal.QueryInterface(unknown, in IID_IMediaBuffer, out IntPtr mbPtr));
                try
                {
                    MediaFoundationException.ThrowIfFailed(
                        mediaObject.ProcessInput(inputStreamIndex, mbPtr, (int)flags, timestamp, duration));
                }
                finally
                {
                    Marshal.Release(mbPtr);
                }
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }

        // IID_IMediaBuffer (mediaobj.h)
        private static readonly Guid IID_IMediaBuffer = new Guid("59eff8b9-938c-4a26-82f2-95cb84cdc837");

        /// <summary>
        /// Process Output
        /// </summary>
        /// <param name="flags">Flags</param>
        /// <param name="outputBufferCount">Output buffer count</param>
        /// <param name="outputBuffers">Output buffers</param>
        public unsafe void ProcessOutput(DmoProcessOutputFlags flags, int outputBufferCount, DmoOutputDataBuffer[] outputBuffers)
        {
            if (outputBuffers is null) throw new ArgumentNullException(nameof(outputBuffers));
            if (outputBufferCount > outputBuffers.Length) throw new ArgumentOutOfRangeException(nameof(outputBufferCount));

            // Build a blittable mirror with raw IUnknown* pointers so we can pin
            // and pass it to the modern IntPtr-typed ProcessOutput signature.
            Span<RawDmoOutputDataBuffer> raw = outputBufferCount <= 4
                ? stackalloc RawDmoOutputDataBuffer[outputBufferCount]
                : new RawDmoOutputDataBuffer[outputBufferCount];

            Span<IntPtr> unknowns = outputBufferCount <= 4
                ? stackalloc IntPtr[outputBufferCount]
                : new IntPtr[outputBufferCount];

            try
            {
                for (int i = 0; i < outputBufferCount; i++)
                {
                    IntPtr u = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(
                        outputBuffers[i].MediaBuffer, CreateComInterfaceFlags.None);
                    try
                    {
                        Marshal.ThrowExceptionForHR(
                            Marshal.QueryInterface(u, in IID_IMediaBuffer, out unknowns[i]));
                    }
                    finally
                    {
                        Marshal.Release(u);
                    }
                    raw[i].pBuffer = unknowns[i];
                    raw[i].dwStatus = outputBuffers[i].StatusFlags;
                    raw[i].rtTimestamp = outputBuffers[i].Timestamp;
                    raw[i].referenceTimeDuration = outputBuffers[i].Duration;
                }

                int hr;
                fixed (RawDmoOutputDataBuffer* p = raw)
                {
                    hr = mediaObject.ProcessOutput((int)flags, outputBufferCount, (IntPtr)p, out _);
                }
                MediaFoundationException.ThrowIfFailed(hr);

                for (int i = 0; i < outputBufferCount; i++)
                {
                    outputBuffers[i].StatusFlags = raw[i].dwStatus;
                    outputBuffers[i].Timestamp = raw[i].rtTimestamp;
                    outputBuffers[i].Duration = raw[i].referenceTimeDuration;
                }
            }
            finally
            {
                for (int i = 0; i < outputBufferCount; i++)
                {
                    if (unknowns[i] != IntPtr.Zero) Marshal.Release(unknowns[i]);
                }
            }
        }
        #endregion

        /// <summary>
        /// Gives the DMO a chance to allocate any resources needed for streaming
        /// </summary>
        public void AllocateStreamingResources()
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.AllocateStreamingResources());
        }

        /// <summary>
        /// Tells the DMO to free any resources needed for streaming
        /// </summary>
        public void FreeStreamingResources()
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.FreeStreamingResources());
        }

        /// <summary>
        /// Gets maximum input latency
        /// </summary>
        /// <param name="inputStreamIndex">input stream index</param>
        /// <returns>Maximum input latency as a ref-time</returns>
        public long GetInputMaxLatency(int inputStreamIndex)
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.GetInputMaxLatency(inputStreamIndex, out long maxLatency));
            return maxLatency;
        }

        /// <summary>
        /// Flushes all buffered data
        /// </summary>
        public void Flush()
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.Flush());
        }

        /// <summary>
        /// Report a discontinuity on the specified input stream
        /// </summary>
        /// <param name="inputStreamIndex">Input Stream index</param>
        public void Discontinuity(int inputStreamIndex)
        {
            MediaFoundationException.ThrowIfFailed(mediaObject.Discontinuity(inputStreamIndex));
        }

        /// <summary>
        /// Is this input stream accepting data?
        /// </summary>
        /// <param name="inputStreamIndex">Input Stream index</param>
        /// <returns>true if accepting data</returns>
        public bool IsAcceptingData(int inputStreamIndex)
        {
            int hresult = mediaObject.GetInputStatus(inputStreamIndex, out int flags);
            MediaFoundationException.ThrowIfFailed(hresult);
            return ((DmoInputStatusFlags)flags & DmoInputStatusFlags.AcceptData) == DmoInputStatusFlags.AcceptData;
        }

        // TODO: there are still several IMediaObject functions to be wrapped

        #region IDisposable Members

        /// <summary>
        /// Releases the underlying COM object.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (mediaObject != null)
            {
                if ((object)mediaObject is ComObject comObject)
                {
                    comObject.FinalRelease();
                }
                mediaObject = null;
            }
        }

        #endregion
    }
}
