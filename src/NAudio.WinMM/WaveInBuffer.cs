using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// A buffer of Wave samples
/// </summary>
internal class WaveInBuffer : IDisposable
{
    private readonly Int32 bufferSize; // allocated bytes, may not be the same as bytes read
    private readonly byte[] buffer;
    private readonly int headerSize;
    private GCHandle hBuffer;
    private IntPtr waveInHandle;
    // The WAVEHDR lives in unmanaged memory rather than as a pinned managed object.
    // waveInAddBuffer hands this exact address to the driver, which keeps it queued and
    // writes dwFlags / dwBytesRecorded into it from its own thread long after the call
    // returns, so the address has to stay valid and stable. It used to be a pinned
    // [StructLayout] class passed by value, which worked only because CoreCLR pins blittable
    // class arguments in place; NativeAOT copies them into a per-call temporary instead, so
    // WHDR_PREPARED never made it back and every waveInAddBuffer failed with
    // WAVERR_UNPREPARED. See https://github.com/naudio/NAudio/issues/1425.
    private IntPtr headerPtr;

    /// <summary>
    /// creates a new wavebuffer
    /// </summary>
    /// <param name="waveInHandle">WaveIn device to write to</param>
    /// <param name="bufferSize">Buffer size in bytes</param>
    public WaveInBuffer(IntPtr waveInHandle, Int32 bufferSize)
    {
        this.bufferSize = bufferSize;
        this.buffer = new byte[bufferSize];
        this.hBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        this.waveInHandle = waveInHandle;

        headerSize = Marshal.SizeOf<WaveHeader>();
        headerPtr = Marshal.AllocHGlobal(headerSize);
        Header = default; // AllocHGlobal does not zero the block
        Header.dataBuffer = hBuffer.AddrOfPinnedObject();
        Header.bufferLength = bufferSize;
        Header.loops = 1;

        MmException.Try(WaveInterop.waveInPrepareHeader(waveInHandle, headerPtr, headerSize), "waveInPrepareHeader");
    }

    /// <summary>
    /// The WAVEHDR itself, accessed in place so the driver's asynchronous updates to
    /// dwFlags and dwBytesRecorded are visible without copying the block back and forth.
    /// </summary>
    private unsafe ref WaveHeader Header => ref Unsafe.AsRef<WaveHeader>((void*)headerPtr);

    /// <summary>
    /// The header's flags, or none once disposed. WaveIn disposes its buffers without joining
    /// the recording thread, so this can be read after Dispose has freed the block; reporting
    /// "no flags set" keeps that benign, as it was when the header was a managed object.
    /// </summary>
    private WaveHeaderFlags Flags => headerPtr == IntPtr.Zero ? 0 : Header.flags;

    /// <summary>
    /// Place this buffer back to record more audio
    /// </summary>
    public void Reuse()
    {
        MmException.Try(WaveInterop.waveInAddBuffer(waveInHandle, headerPtr, headerSize), "waveInAddBuffer");
    }

    #region Dispose Pattern

    /// <summary>
    /// Finalizer for this wave buffer
    /// </summary>
    ~WaveInBuffer()
    {
        Dispose(false);
    }

    /// <summary>
    /// Releases resources held by this WaveBuffer
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>
    /// Releases resources held by this WaveBuffer
    /// </summary>
    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            // free managed resources
        }
        // free unmanaged resources
        if (waveInHandle != IntPtr.Zero)
        {
            WaveInterop.waveInUnprepareHeader(waveInHandle, headerPtr, headerSize);
            waveInHandle = IntPtr.Zero;
        }
        // only after unpreparing, while the driver could still be holding the address.
        // Clear the field before freeing so a concurrent reader of Done/InQueue/BytesRecorded
        // sees "disposed" rather than briefly dereferencing released memory.
        if (headerPtr != IntPtr.Zero)
        {
            var toFree = headerPtr;
            headerPtr = IntPtr.Zero;
            Marshal.FreeHGlobal(toFree);
        }
        if (hBuffer.IsAllocated)
            hBuffer.Free();

    }

    #endregion

    /// <summary>
    /// Provides access to the actual record buffer (for reading only)
    /// </summary>
    public byte[] Data
    {
        get
        {
            return buffer;
        }
    }

    /// <summary>
    /// Indicates whether the Done flag is set on this buffer
    /// </summary>
    public bool Done
    {
        get
        {
            return (Flags & WaveHeaderFlags.Done) == WaveHeaderFlags.Done;
        }
    }


    /// <summary>
    /// Indicates whether the InQueue flag is set on this buffer
    /// </summary>
    public bool InQueue
    {
        get
        {
            return (Flags & WaveHeaderFlags.InQueue) == WaveHeaderFlags.InQueue;
        }
    }

    /// <summary>
    /// Number of bytes recorded
    /// </summary>
    public int BytesRecorded
    {
        get
        {
            return headerPtr == IntPtr.Zero ? 0 : Header.bytesRecorded;
        }
    }

    /// <summary>
    /// The buffer size in bytes
    /// </summary>
    public Int32 BufferSize
    {
        get
        {
            return bufferSize;
        }
    }
}
