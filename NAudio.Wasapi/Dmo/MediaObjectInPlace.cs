using System;
using NAudio.Dmo.Interfaces;
using NAudio.Wasapi.CoreAudioApi;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo
{
    /// <summary>
    /// Media Object InPlace
    /// </summary>
    public class MediaObjectInPlace : IDisposable
    {
        private IMediaObjectInPlace mediaObjectInPlace;

        /// <summary>
        /// Creates a new Media Object InPlace
        /// </summary>
        /// <param name="mediaObjectInPlace">Media Object InPlace COM Interface</param>
        internal MediaObjectInPlace(IMediaObjectInPlace mediaObjectInPlace)
        {
            this.mediaObjectInPlace = mediaObjectInPlace;
        }

        /// <summary>
        /// Processes a block of data in place using a span.
        /// The data is pinned and passed directly to the DMO, avoiding intermediate copies.
        /// </summary>
        /// <param name="data">In/Out Data buffer</param>
        /// <param name="timeStart">Start time of the data.</param>
        /// <param name="inPlaceFlag">DmoInplaceProcessFlags</param>
        /// <returns>Return value when Process is executed with IMediaObjectInPlace</returns>
        public unsafe DmoInPlaceProcessReturn Process(Span<byte> data, long timeStart, DmoInPlaceProcessFlags inPlaceFlag)
        {
            fixed (byte* pData = data)
            {
                var result = mediaObjectInPlace.Process(data.Length, (IntPtr)pData, timeStart, (int)inPlaceFlag);
                Marshal.ThrowExceptionForHR(result);
                return (DmoInPlaceProcessReturn)result;
            }
        }

        /// <summary>
        /// Creates a copy of the DMO in its current state.
        /// </summary>
        /// <returns>Copyed MediaObjectInPlace</returns>
        public MediaObjectInPlace Clone()
        {
            Marshal.ThrowExceptionForHR(this.mediaObjectInPlace.Clone(out IntPtr clonePtr));
            try
            {
                var clone = (IMediaObjectInPlace)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    clonePtr, CreateObjectFlags.UniqueInstance);
                return new MediaObjectInPlace(clone);
            }
            finally
            {
                Marshal.Release(clonePtr);
            }
        }



        /// <summary>
        /// Retrieves the latency introduced by this DMO.
        /// </summary>
        /// <returns>The latency, in 100-nanosecond units</returns>
        public long GetLatency()
        {
            Marshal.ThrowExceptionForHR(this.mediaObjectInPlace.GetLatency(out var latencyTime));
            return latencyTime;
        }

        /// <summary>
        /// Get Media Object
        /// </summary>
        /// <returns>Media Object</returns>
        public MediaObject GetMediaObject()
        {
            return new MediaObject((IMediaObject)mediaObjectInPlace);
        }

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (mediaObjectInPlace != null)
            {
                if ((object)mediaObjectInPlace is ComObject comObject)
                {
                    comObject.FinalRelease();
                }
                mediaObjectInPlace = null;
            }
        }
    }
}
