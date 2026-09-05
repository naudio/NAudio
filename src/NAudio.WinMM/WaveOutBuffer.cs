using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace NAudio.Wave;

/// <summary>
/// A buffer of Wave samples for streaming to a Wave Output device
/// </summary>
internal class WaveOutBuffer : IDisposable
{
    private readonly Int32 bufferSize; // allocated bytes, may not be the same as bytes read
    private readonly byte[] buffer;
    private readonly IWaveProvider waveStream;
    private readonly Lock waveOutLock;
    private readonly Lock waveStreamLock;
    private readonly int headerSize;
    private GCHandle hBuffer;
    private IntPtr hWaveOut;
    // The WAVEHDR lives in unmanaged memory rather than as a pinned managed object, because
    // waveOutWrite hands this exact address to the driver, which keeps it queued and updates
    // dwFlags from its own thread. See WaveInBuffer and
    // https://github.com/naudio/NAudio/issues/1425 for why a pinned class isn't enough.
    private IntPtr headerPtr;
    // Stopwatch ticks recorded when this buffer was last submitted to the device. Used by
    // IWaveLatency.CurrentLatency to estimate how stale the audio at the play head is.
    // long.MinValue means "never written" so the consumer can ignore this buffer.
    private long filledTimestamp = long.MinValue;

    /// <summary>
    /// creates a new wavebuffer
    /// </summary>
    /// <param name="hWaveOut">WaveOut device to write to</param>
    /// <param name="bufferSize">Buffer size in bytes</param>
    /// <param name="bufferFillStream">Stream to provide more data</param>
    /// <param name="waveOutLock">Lock to protect WaveOut API's from being called on >1 thread</param>
    public WaveOutBuffer(IntPtr hWaveOut, Int32 bufferSize, IWaveProvider bufferFillStream, Lock waveOutLock)
    {
        this.bufferSize = bufferSize;
        buffer = new byte[bufferSize];
        hBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        this.hWaveOut = hWaveOut;
        waveStream = bufferFillStream;
        this.waveOutLock = waveOutLock;
        waveStreamLock = new Lock();

        headerSize = Marshal.SizeOf<WaveHeader>();
        headerPtr = Marshal.AllocHGlobal(headerSize);
        Header = default; // AllocHGlobal does not zero the block
        Header.dataBuffer = hBuffer.AddrOfPinnedObject();
        Header.bufferLength = bufferSize;
        Header.loops = 1;
        lock (waveOutLock)
        {
            MmException.Try(WaveInterop.waveOutPrepareHeader(hWaveOut, headerPtr, headerSize), "waveOutPrepareHeader");
        }
    }

    /// <summary>
    /// The WAVEHDR itself, accessed in place so the driver's asynchronous updates to
    /// dwFlags are visible without copying the block back and forth.
    /// </summary>
    private unsafe ref WaveHeader Header => ref Unsafe.AsRef<WaveHeader>((void*)headerPtr);

    #region Dispose Pattern

    /// <summary>
    /// Finalizer for this wave buffer
    /// </summary>
    ~WaveOutBuffer()
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
        // free unmanaged resources. WriteToWaveOut holds waveOutLock across waveOutWrite, so
        // unpreparing, clearing the pointer and freeing all happen under that same lock: a
        // concurrent write either completes first or sees IntPtr.Zero, never a freed address.
        // The condition keeps a partially constructed buffer — where waveOutLock is still
        // null — out of the lock; it has nothing to release anyway.
        if (hWaveOut != IntPtr.Zero || headerPtr != IntPtr.Zero)
        {
            lock (waveOutLock)
            {
                if (hWaveOut != IntPtr.Zero)
                {
                    WaveInterop.waveOutUnprepareHeader(hWaveOut, headerPtr, headerSize);
                    hWaveOut = IntPtr.Zero;
                }
                // Only after unpreparing, while the driver could still be holding the address.
                // Clear the field before freeing so a concurrent reader of Done/InQueue/
                // BytesRecorded sees "disposed" rather than briefly dereferencing released memory.
                if (headerPtr != IntPtr.Zero)
                {
                    var toFree = headerPtr;
                    headerPtr = IntPtr.Zero;
                    Marshal.FreeHGlobal(toFree);
                }
            }
        }
        if (hBuffer.IsAllocated)
            hBuffer.Free();
    }

    #endregion

    /// this is called by the WAVE callback and should be used to refill the buffer
    public bool OnDone()
    {
        int bytes;
        lock (waveStreamLock)
        {
            bytes = waveStream.Read(buffer.AsSpan());
        }
        if (bytes == 0)
        {
            return false;
        }
        if (bytes < buffer.Length)
        {
            Array.Clear(buffer, bytes, buffer.Length - bytes);
        }
        // Stamp BEFORE handing the buffer to the driver: WriteToWaveOut sets InQueue via the
        // driver, and a concurrent reader of CurrentLatency must never see the pair
        // (InQueue = true, stale filledTimestamp from a previous cycle).
        Volatile.Write(ref filledTimestamp, Stopwatch.GetTimestamp());
        WriteToWaveOut();
        return true;
    }

    /// <summary>
    /// Stopwatch timestamp recorded when this buffer was last submitted to the device, or
    /// <see cref="long.MinValue"/> if it has never been submitted.
    /// </summary>
    public long FilledTimestamp => Volatile.Read(ref filledTimestamp);

    /// <summary>
    /// Whether the header's in queue flag is set
    /// </summary>
    public bool InQueue
    {
        get
        {
            // WaveOut disposes its buffers without joining the playback thread, and the
            // public IWaveLatency.CurrentLatency reads this from any thread, so the block may
            // already be freed. Report "not queued" rather than dereferencing released memory.
            if (headerPtr == IntPtr.Zero) return false;
            return (Header.flags & WaveHeaderFlags.InQueue) == WaveHeaderFlags.InQueue;
        }
    }

    /// <summary>
    /// The buffer size in bytes
    /// </summary>
    public int BufferSize => bufferSize;

    private void WriteToWaveOut()
    {
        MmResult result;

        lock (waveOutLock)
        {
            result = WaveInterop.waveOutWrite(hWaveOut, headerPtr, headerSize);
        }
        if (result != MmResult.NoError)
        {
            throw new MmException(result, "waveOutWrite");
        }

        GC.KeepAlive(this);
    }

}
