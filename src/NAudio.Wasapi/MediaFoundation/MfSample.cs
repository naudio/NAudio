using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi;

namespace NAudio.MediaFoundation;

/// <summary>
/// Wrapper for IMFSample providing managed access to media samples.
/// Internal until the high-level API solidifies (see MODERNIZATION.md Phase 3).
/// </summary>
internal class MfSample : IDisposable
{
    private Interfaces.IMFSample? sampleInterface;

    private Interfaces.IMFSample Sample => sampleInterface ?? throw new ObjectDisposedException(nameof(MfSample));
    private IntPtr nativePointer;

    internal MfSample(Interfaces.IMFSample sampleInterface, IntPtr nativePointer)
    {
        this.sampleInterface = sampleInterface;
        this.nativePointer = nativePointer;
    }

    /// <summary>
    /// Gets the native COM pointer for this sample (for passing to other COM methods).
    /// </summary>
    internal IntPtr NativePointer => nativePointer;

    /// <summary>
    /// Gets or sets the presentation time of the sample, in 100-nanosecond units.
    /// </summary>
    public long SampleTime
    {
        get
        {
            MediaFoundationException.ThrowIfFailed(Sample.GetSampleTime(out var time));
            return time;
        }
        set => MediaFoundationException.ThrowIfFailed(Sample.SetSampleTime(value));
    }

    /// <summary>
    /// Gets or sets the duration of the sample, in 100-nanosecond units.
    /// </summary>
    public long SampleDuration
    {
        get
        {
            MediaFoundationException.ThrowIfFailed(Sample.GetSampleDuration(out var duration));
            return duration;
        }
        set => MediaFoundationException.ThrowIfFailed(Sample.SetSampleDuration(value));
    }

    /// <summary>
    /// Gets or sets flags associated with the sample.
    /// </summary>
    public int SampleFlags
    {
        get
        {
            MediaFoundationException.ThrowIfFailed(Sample.GetSampleFlags(out var flags));
            return flags;
        }
        set => MediaFoundationException.ThrowIfFailed(Sample.SetSampleFlags(value));
    }

    /// <summary>
    /// Gets the number of buffers in the sample.
    /// </summary>
    public int BufferCount
    {
        get
        {
            MediaFoundationException.ThrowIfFailed(Sample.GetBufferCount(out var count));
            return count;
        }
    }

    /// <summary>
    /// Gets the total length of valid data in all buffers.
    /// </summary>
    public int TotalLength
    {
        get
        {
            MediaFoundationException.ThrowIfFailed(Sample.GetTotalLength(out var length));
            return length;
        }
    }

    /// <summary>
    /// Gets a buffer by index.
    /// </summary>
    /// <param name="index">Zero-based index of the buffer.</param>
    /// <returns>The buffer at the specified index.</returns>
    public MfMediaBuffer GetBufferByIndex(int index)
    {
        MediaFoundationException.ThrowIfFailed(Sample.GetBufferByIndex(index, out var bufferPtr));
        var bufferInterface = (Interfaces.IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
            bufferPtr, CreateObjectFlags.UniqueInstance);
        return new MfMediaBuffer(bufferInterface, bufferPtr);
    }

    /// <summary>
    /// Adds a buffer to the sample.
    /// </summary>
    /// <param name="buffer">The buffer to add.</param>
    public void AddBuffer(MfMediaBuffer buffer)
    {
        MediaFoundationException.ThrowIfFailed(Sample.AddBuffer(buffer.NativePointer));
    }

    /// <summary>
    /// Converts the sample to a single contiguous buffer.
    /// </summary>
    /// <returns>A contiguous buffer containing all sample data.</returns>
    public MfMediaBuffer ConvertToContiguousBuffer()
    {
        MediaFoundationException.ThrowIfFailed(Sample.ConvertToContiguousBuffer(out var bufferPtr));
        var bufferInterface = (Interfaces.IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
            bufferPtr, CreateObjectFlags.UniqueInstance);
        return new MfMediaBuffer(bufferInterface, bufferPtr);
    }

    /// <summary>
    /// Copies the sample data to a buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    public void CopyToBuffer(MfMediaBuffer buffer)
    {
        MediaFoundationException.ThrowIfFailed(Sample.CopyToBuffer(buffer.NativePointer));
    }

    /// <summary>
    /// Removes a buffer by index.
    /// </summary>
    /// <param name="index">Zero-based index of the buffer to remove.</param>
    public void RemoveBufferByIndex(int index)
    {
        MediaFoundationException.ThrowIfFailed(Sample.RemoveBufferByIndex(index));
    }

    /// <summary>
    /// Removes all buffers from the sample.
    /// </summary>
    public void RemoveAllBuffers()
    {
        MediaFoundationException.ThrowIfFailed(Sample.RemoveAllBuffers());
    }

    /// <summary>
    /// Finalizer — runs only if Dispose was not called. Releases the native IntPtr ref;
    /// the source-generated RCW has its own ComObject finalizer that releases its ref
    /// independently.
    /// </summary>
    ~MfSample() => Dispose(disposing: false);

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
        if (disposing && sampleInterface != null)
        {
            ((ComObject)(object)sampleInterface).FinalRelease();
            sampleInterface = null;
        }
    }
}
