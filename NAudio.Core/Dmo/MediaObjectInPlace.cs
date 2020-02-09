using System;
using System.Runtime.InteropServices;

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
        /// Processes a block of data.
        /// The application supplies a pointer to a block of input data. The DMO processes the data in place.
        /// </summary>
        /// <param name="size">Size of the data, in bytes.</param>
        /// <param name="offset">offset into buffer</param>
        /// <param name="data">In/Out Data Buffer</param>
        /// <param name="timeStart">Start time of the data.</param>
        /// <param name="inPlaceFlag">DmoInplaceProcessFlags</param>
        /// <returns>Return value when Process is executed with IMediaObjectInPlace</returns>
        public DmoInPlaceProcessReturn Process(int size, int offset, byte[] data, long timeStart, DmoInPlaceProcessFlags inPlaceFlag)
        {
            var pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, offset, pointer, size);

            var result = mediaObjectInPlace.Process(size, pointer, timeStart, inPlaceFlag);
            Marshal.ThrowExceptionForHR(result);

            Marshal.Copy(pointer, data, offset, size);
            Marshal.FreeHGlobal(pointer);

            return (DmoInPlaceProcessReturn) result;
        }

        /// <summary>
        /// Creates a copy of the DMO in its current state.
        /// </summary>
        /// <returns>Copyed MediaObjectInPlace</returns>
        public MediaObjectInPlace Clone()
        {
            Marshal.ThrowExceptionForHR(this.mediaObjectInPlace.Clone(out var cloneObj));
            return new MediaObjectInPlace(cloneObj);
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
            return new MediaObject((IMediaObject) mediaObjectInPlace);
        }

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose()
        {
            if (mediaObjectInPlace != null)
            {
                Marshal.ReleaseComObject(mediaObjectInPlace);
                mediaObjectInPlace = null;
            }
        }
    }
}