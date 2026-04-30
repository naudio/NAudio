using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFSinkWriter providing managed access to media sink writing.
    /// </summary>
    public class MfSinkWriter : IDisposable
    {
        private Interfaces.IMFSinkWriter writerInterface;
        private IntPtr nativePointer;

        internal MfSinkWriter(Interfaces.IMFSinkWriter writerInterface, IntPtr nativePointer)
        {
            this.writerInterface = writerInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Adds a stream to the sink writer.
        /// </summary>
        /// <param name="targetMediaType">The target media type for the stream.</param>
        /// <returns>The zero-based index of the new stream.</returns>
        public int AddStream(MfMediaType targetMediaType)
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.AddStream(targetMediaType.NativePointer, out var streamIndex));
            return streamIndex;
        }

        /// <summary>
        /// Sets the input format for a stream on the sink writer.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        /// <param name="inputMediaType">The input media type.</param>
        public void SetInputMediaType(int streamIndex, MfMediaType inputMediaType)
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.SetInputMediaType(streamIndex, inputMediaType.NativePointer, IntPtr.Zero));
        }

        /// <summary>
        /// Initializes the sink writer for writing.
        /// </summary>
        public void BeginWriting()
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.BeginWriting());
        }

        /// <summary>
        /// Delivers a sample to the sink writer.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        /// <param name="sample">The sample to write.</param>
        public void WriteSample(int streamIndex, MfSample sample)
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.WriteSample(streamIndex, sample.NativePointer));
        }

        /// <summary>
        /// Indicates a gap in an input stream.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        /// <param name="timestamp">The timestamp of the gap, in 100-nanosecond units.</param>
        public void SendStreamTick(int streamIndex, long timestamp)
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.SendStreamTick(streamIndex, timestamp));
        }

        /// <summary>
        /// Notifies the media sink that a stream has reached the end of a segment.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        public void NotifyEndOfSegment(int streamIndex)
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.NotifyEndOfSegment(streamIndex));
        }

        /// <summary>
        /// Flushes one or more streams.
        /// </summary>
        /// <param name="streamIndex">The stream index.</param>
        public void Flush(int streamIndex)
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.Flush(streamIndex));
        }

        /// <summary>
        /// Completes all writing operations on the sink writer.
        /// </summary>
        public void DoFinalize()
        {
            MediaFoundationException.ThrowIfFailed(writerInterface.DoFinalize());
        }

        /// <summary>
        /// Finalizer — runs only if Dispose was not called. Releases the native IntPtr ref;
        /// the source-generated RCW has its own ComObject finalizer that releases its ref
        /// independently.
        /// </summary>
        ~MfSinkWriter() => Dispose(disposing: false);

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
            if (disposing && writerInterface != null)
            {
                ((ComObject)(object)writerInterface).FinalRelease();
                writerInterface = null;
            }
        }
    }
}
