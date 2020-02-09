using System;
using System.Collections.Generic;
using NAudio.Utils;
using System.Runtime.InteropServices;
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
        public int InputStreamCount
        {
            get { return inputStreams; }
        }

        /// <summary>
        /// Number of output streams
        /// </summary>
        public int OutputStreamCount
        {
            get { return outputStreams; }
        }
        #endregion

        #region Get Input and Output Types

        /// <summary>
        /// Gets the input media type for the specified input stream
        /// </summary>
        /// <param name="inputStream">Input stream index</param>
        /// <param name="inputTypeIndex">Input type index</param>
        /// <returns>DMO Media Type or null if there are no more input types</returns>
        public DmoMediaType? GetInputType(int inputStream, int inputTypeIndex)
        {
            try
            {
                DmoMediaType mediaType;
                int hresult = mediaObject.GetInputType(inputStream, inputTypeIndex, out mediaType);
                if (hresult == HResult.S_OK)
                {
                    // this frees the format (if present)
                    // we should therefore come up with a way of marshaling the format
                    // into a completely managed structure
                    DmoInterop.MoFreeMediaType(ref mediaType);
                    return mediaType;
                }
            }
            catch (COMException e)
            {
                if (e.GetHResult() != (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
                {
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the DMO Media Output type
        /// </summary>
        /// <param name="outputStream">The output stream</param>
        /// <param name="outputTypeIndex">Output type index</param>
        /// <returns>DMO Media Type or null if no more available</returns>
        public DmoMediaType? GetOutputType(int outputStream, int outputTypeIndex)
        {
            try
            {
                DmoMediaType mediaType;
                int hresult = mediaObject.GetOutputType(outputStream, outputTypeIndex, out mediaType);
                if (hresult == HResult.S_OK)
                {
                    // this frees the format (if present)
                    // we should therefore come up with a way of marshaling the format
                    // into a completely managed structure
                    DmoInterop.MoFreeMediaType(ref mediaType);
                    return mediaType;
                }
            }
            catch (COMException e)
            {
                if (e.GetHResult() != (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
                {
                    throw;
                }
            }
            return null;
        }

         /// <summary>
        /// retrieves the media type that was set for an output stream, if any
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <returns>DMO Media Type or null if no more available</returns>
        public DmoMediaType GetOutputCurrentType(int outputStreamIndex)
        {
            DmoMediaType mediaType;
            int hresult = mediaObject.GetOutputCurrentType(outputStreamIndex, out mediaType);
            if (hresult == HResult.S_OK)
            {
                // this frees the format (if present)
                // we should therefore come up with a way of marshaling the format
                // into a completely managed structure
                DmoInterop.MoFreeMediaType(ref mediaType);
                return mediaType;
            }
            else
            {
                if (hresult == (int)DmoHResults.DMO_E_TYPE_NOT_SET)
                {
                    throw new InvalidOperationException("Media type was not set.");
                }
                else
                {
                    throw Marshal.GetExceptionForHR(hresult);
                }
            }
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
            while ((mediaType = GetInputType(inputStreamIndex,typeIndex)) != null)
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
            return SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
        }

        /// <summary>
        /// Sets the input type helper method
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <param name="mediaType">Media type</param>
        /// <param name="flags">Flags (can be used to test rather than set)</param>
        private bool SetInputType(int inputStreamIndex, DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            int hResult = mediaObject.SetInputType(inputStreamIndex, ref mediaType, flags);
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
            if(!SetInputType(inputStreamIndex,mediaType,DmoSetTypeFlags.None))
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
            bool set = SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.None);
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
            bool supported = SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
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
            return SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
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
            bool supported = SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
            DmoInterop.MoFreeMediaType(ref mediaType);
            return supported;
        }

        /// <summary>
        /// Helper method to call SetOutputType
        /// </summary>
        private bool SetOutputType(int outputStreamIndex, DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            int hresult = mediaObject.SetOutputType(outputStreamIndex, ref mediaType, flags);
            if (hresult == (int)DmoHResults.DMO_E_TYPE_NOT_ACCEPTED)
            {
                return false;
            }
            else if (hresult == HResult.S_OK)
            {
                return true;
            }
            else
            {
                throw Marshal.GetExceptionForHR(hresult);
            }
        }

        /// <summary>
        /// Sets the output type
        /// n.b. may need to set the input type first
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <param name="mediaType">Media type to set</param>
        public void SetOutputType(int outputStreamIndex, DmoMediaType mediaType)
        {
            if (!SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.None))
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
            bool succeeded = SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.None);
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
            int size;
            int maxLookahead;
            int alignment;
            Marshal.ThrowExceptionForHR(mediaObject.GetInputSizeInfo(inputStreamIndex, out size, out maxLookahead, out alignment));
            return new MediaObjectSizeInfo(size, maxLookahead, alignment);
        }

        /// <summary>
        /// Get Output Size Info
        /// </summary>
        /// <param name="outputStreamIndex">Output Stream Index</param>
        /// <returns>Output Size Info</returns>
        public MediaObjectSizeInfo GetOutputSizeInfo(int outputStreamIndex)
        {
            int size;
            int alignment;
            Marshal.ThrowExceptionForHR(mediaObject.GetOutputSizeInfo(outputStreamIndex, out size, out alignment));
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
            Marshal.ThrowExceptionForHR(mediaObject.ProcessInput(inputStreamIndex, mediaBuffer, flags, timestamp, duration));
        }

        /// <summary>
        /// Process Output
        /// </summary>
        /// <param name="flags">Flags</param>
        /// <param name="outputBufferCount">Output buffer count</param>
        /// <param name="outputBuffers">Output buffers</param>
        public void ProcessOutput(DmoProcessOutputFlags flags, int outputBufferCount, DmoOutputDataBuffer[] outputBuffers)
        {
            int reserved;
            Marshal.ThrowExceptionForHR(mediaObject.ProcessOutput(flags, outputBufferCount, outputBuffers, out reserved));
        }
        #endregion

        /// <summary>
        /// Gives the DMO a chance to allocate any resources needed for streaming
        /// </summary>
        public void AllocateStreamingResources()
        {
            Marshal.ThrowExceptionForHR(mediaObject.AllocateStreamingResources());
        }

        /// <summary>
        /// Tells the DMO to free any resources needed for streaming
        /// </summary>
        public void FreeStreamingResources()
        {
            Marshal.ThrowExceptionForHR(mediaObject.FreeStreamingResources());
        }

        /// <summary>
        /// Gets maximum input latency
        /// </summary>
        /// <param name="inputStreamIndex">input stream index</param>
        /// <returns>Maximum input latency as a ref-time</returns>
        public long GetInputMaxLatency(int inputStreamIndex)
        {
            long maxLatency;
            Marshal.ThrowExceptionForHR(mediaObject.GetInputMaxLatency(inputStreamIndex, out maxLatency));
            return maxLatency;
        }

        /// <summary>
        /// Flushes all buffered data
        /// </summary>
        public void Flush()
        {
            Marshal.ThrowExceptionForHR(mediaObject.Flush());
        }

        /// <summary>
        /// Report a discontinuity on the specified input stream
        /// </summary>
        /// <param name="inputStreamIndex">Input Stream index</param>
        public void Discontinuity(int inputStreamIndex)
        {
            Marshal.ThrowExceptionForHR(mediaObject.Discontinuity(inputStreamIndex));
        }

        /// <summary>
        /// Is this input stream accepting data?
        /// </summary>
        /// <param name="inputStreamIndex">Input Stream index</param>
        /// <returns>true if accepting data</returns>
        public bool IsAcceptingData(int inputStreamIndex)
        {
            DmoInputStatusFlags flags;
            int hresult = mediaObject.GetInputStatus(inputStreamIndex, out flags);
            Marshal.ThrowExceptionForHR(hresult);
            return (flags & DmoInputStatusFlags.DMO_INPUT_STATUSF_ACCEPT_DATA) == DmoInputStatusFlags.DMO_INPUT_STATUSF_ACCEPT_DATA;
        }

        // TODO: there are still several IMediaObject functions to be wrapped

        #region IDisposable Members

        /// <summary>
        /// Experimental code, not currently being called
        /// Not sure if it is necessary anyway
        /// </summary>
        public void Dispose()
        {
            if (mediaObject != null)
            {
                Marshal.ReleaseComObject(mediaObject);
                mediaObject = null;
            }
        }

        #endregion
    }
}
