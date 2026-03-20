using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFSourceReader providing managed access to media source reading.
    /// </summary>
    public class MfSourceReader : IDisposable
    {
        private Interfaces.IMFSourceReader readerInterface;
        private IntPtr nativePointer;

        internal MfSourceReader(Interfaces.IMFSourceReader readerInterface, IntPtr nativePointer)
        {
            this.readerInterface = readerInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Queries whether a stream is selected.
        /// </summary>
        /// <param name="streamIndex">The stream index, or a MF_SOURCE_READER constant.</param>
        /// <returns>True if the stream is selected.</returns>
        public bool GetStreamSelection(int streamIndex)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.GetStreamSelection(streamIndex, out var selected));
            return selected != 0;
        }

        /// <summary>
        /// Selects or deselects a stream.
        /// </summary>
        /// <param name="streamIndex">The stream index, or a MF_SOURCE_READER constant.</param>
        /// <param name="selected">True to select the stream, false to deselect.</param>
        public void SetStreamSelection(int streamIndex, bool selected)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.SetStreamSelection(streamIndex, selected ? 1 : 0));
        }

        /// <summary>
        /// Gets a format that is supported natively by the media source.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        /// <param name="mediaTypeIndex">The media type index.</param>
        /// <returns>The native media type.</returns>
        public MfMediaType GetNativeMediaType(int streamIndex, int mediaTypeIndex)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.GetNativeMediaType(streamIndex, mediaTypeIndex, out var mediaTypePtr));
            var mediaTypeInterface = (Interfaces.IMFMediaType)Marshal.GetObjectForIUnknown(mediaTypePtr);
            return new MfMediaType(mediaTypeInterface, mediaTypePtr);
        }

        /// <summary>
        /// Gets the current media type for a stream.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        /// <returns>The current media type.</returns>
        public MfMediaType GetCurrentMediaType(int streamIndex)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.GetCurrentMediaType(streamIndex, out var mediaTypePtr));
            var mediaTypeInterface = (Interfaces.IMFMediaType)Marshal.GetObjectForIUnknown(mediaTypePtr);
            return new MfMediaType(mediaTypeInterface, mediaTypePtr);
        }

        /// <summary>
        /// Sets the media type for a stream.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        /// <param name="mediaType">The media type to set.</param>
        public void SetCurrentMediaType(int streamIndex, MfMediaType mediaType)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.SetCurrentMediaType(streamIndex, IntPtr.Zero, mediaType.NativePointer));
        }

        /// <summary>
        /// Seeks to a new position in the media source.
        /// </summary>
        /// <param name="position">The position in 100-nanosecond units.</param>
        public void SetCurrentPosition(long position)
        {
            var propVariant = Marshal.AllocCoTaskMem(16);
            try
            {
                // Write a VT_I8 PROPVARIANT
                Marshal.WriteInt16(propVariant, 0, 20); // VT_I8
                Marshal.WriteInt16(propVariant, 2, 0);
                Marshal.WriteInt32(propVariant, 4, 0);
                Marshal.WriteInt64(propVariant, 8, position);
                MediaFoundationException.ThrowIfFailed(readerInterface.SetCurrentPosition(Guid.Empty, propVariant));
            }
            finally
            {
                Marshal.FreeCoTaskMem(propVariant);
            }
        }

        /// <summary>
        /// Reads the next sample from the media source.
        /// </summary>
        /// <param name="streamIndex">The stream index, or a MF_SOURCE_READER constant.</param>
        /// <param name="actualStreamIndex">Receives the actual stream index.</param>
        /// <param name="streamFlags">Receives status flags.</param>
        /// <param name="timestamp">Receives the timestamp in 100-nanosecond units.</param>
        /// <returns>The sample, or null if no sample was returned (e.g. end of stream).</returns>
        public MfSample ReadSample(int streamIndex, out int actualStreamIndex, out SourceReaderFlags streamFlags, out long timestamp)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.ReadSample(streamIndex, 0, out actualStreamIndex, out var flags, out timestamp, out var samplePtr));
            streamFlags = (SourceReaderFlags)flags;
            if (samplePtr == IntPtr.Zero)
                return null;
            var sampleInterface = (Interfaces.IMFSample)Marshal.GetObjectForIUnknown(samplePtr);
            return new MfSample(sampleInterface, samplePtr);
        }

        /// <summary>
        /// Flushes one or more streams.
        /// </summary>
        /// <param name="streamIndex">The stream index, or a MF_SOURCE_READER constant.</param>
        public void Flush(int streamIndex)
        {
            MediaFoundationException.ThrowIfFailed(readerInterface.Flush(streamIndex));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            readerInterface = null;
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
