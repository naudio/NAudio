using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression;

internal class AcmStreamHeader : IDisposable
{
    private readonly int headerSize;
    private GCHandle hSourceBuffer;
    private GCHandle hDestBuffer;
    private readonly IntPtr streamHandle;
    private bool firstTime;
    // The ACMSTREAMHEADER lives in unmanaged memory rather than as a [StructLayout] class.
    // acmStreamPrepareHeader writes the codec's private state into the header's reserved
    // tail, and acmStreamConvert and acmStreamUnprepareHeader read it back, so the block has
    // to survive unchanged across all three calls. Passing it as a class worked only because
    // CoreCLR pins blittable class arguments in place; NativeAOT copies them into a per-call
    // temporary that round-trips the declared fields alone, which zeroed the reserved tail
    // and failed the conversion. See https://github.com/naudio/NAudio/issues/1425.
    private IntPtr headerPtr;

    public AcmStreamHeader(IntPtr streamHandle, int sourceBufferLength, int destBufferLength)
    {
        SourceBuffer = new byte[sourceBufferLength];
        hSourceBuffer = GCHandle.Alloc(SourceBuffer, GCHandleType.Pinned);

        DestBuffer = new byte[destBufferLength];
        hDestBuffer = GCHandle.Alloc(DestBuffer, GCHandleType.Pinned);

        headerSize = Marshal.SizeOf<AcmStreamHeaderStruct>();
        headerPtr = Marshal.AllocHGlobal(headerSize);
        Header = default; // AllocHGlobal does not zero the block

        this.streamHandle = streamHandle;
        firstTime = true;
        //Prepare();
    }

    /// <summary>
    /// The ACMSTREAMHEADER itself, accessed in place so the codec's writes to the status
    /// flags, the used lengths and its own reserved fields survive between calls.
    /// </summary>
    private unsafe ref AcmStreamHeaderStruct Header => ref Unsafe.AsRef<AcmStreamHeaderStruct>((void*)headerPtr);

    private void Prepare()
    {
        Header.cbStruct = headerSize;
        Header.sourceBufferLength = SourceBuffer.Length;
        Header.sourceBufferPointer = hSourceBuffer.AddrOfPinnedObject();
        Header.destBufferLength = DestBuffer.Length;
        Header.destBufferPointer = hDestBuffer.AddrOfPinnedObject();
        MmException.Try(AcmInterop.acmStreamPrepareHeader(streamHandle, headerPtr, 0), "acmStreamPrepareHeader");
    }

    private void Unprepare()
    {
        Header.sourceBufferLength = SourceBuffer.Length;
        Header.sourceBufferPointer = hSourceBuffer.AddrOfPinnedObject();
        Header.destBufferLength = DestBuffer.Length;
        Header.destBufferPointer = hDestBuffer.AddrOfPinnedObject();

        MmResult result = AcmInterop.acmStreamUnprepareHeader(streamHandle, headerPtr, 0);
        if (result != MmResult.NoError)
        {
            //if (result == MmResult.AcmHeaderUnprepared)
            throw new MmException(result, "acmStreamUnprepareHeader");
        }
    }

    public void Reposition()
    {
        firstTime = true;
    }

    public int Convert(int bytesToConvert, out int sourceBytesConverted)
    {
        // A call arriving after Dispose would otherwise write through a null ref into the
        // freed block. Racing Dispose from another thread can still slip past this.
        ObjectDisposedException.ThrowIf(headerPtr == IntPtr.Zero, this);

        Prepare();
        try
        {
            Header.sourceBufferLength = bytesToConvert;
            Header.sourceBufferLengthUsed = bytesToConvert;
            AcmStreamConvertFlags flags = firstTime ? (AcmStreamConvertFlags.Start | AcmStreamConvertFlags.BlockAlign) : AcmStreamConvertFlags.BlockAlign;
            MmException.Try(AcmInterop.acmStreamConvert(streamHandle, headerPtr, flags), "acmStreamConvert");
            firstTime = false;
            System.Diagnostics.Debug.Assert(Header.destBufferLength == DestBuffer.Length, "Codecs should not change dest buffer length");
            sourceBytesConverted = Header.sourceBufferLengthUsed;
        }
        finally
        {
            Unprepare();
        }

        return Header.destBufferLengthUsed;
    }

    public byte[] SourceBuffer { get; private set; }

    public byte[] DestBuffer { get; private set; }

    #region IDisposable Members

    private bool disposed = false;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            //Unprepare();
            SourceBuffer = null;
            DestBuffer = null;
            if (hSourceBuffer.IsAllocated)
            {
                hSourceBuffer.Free();
            }
            if (hDestBuffer.IsAllocated)
            {
                hDestBuffer.Free();
            }
            if (headerPtr != IntPtr.Zero)
            {
                // Clear the field before freeing so nothing can reach the released
                // block through Header.
                IntPtr block = headerPtr;
                headerPtr = IntPtr.Zero;
                Marshal.FreeHGlobal(block);
            }
        }
        disposed = true;
    }

    ~AcmStreamHeader()
    {
        System.Diagnostics.Debug.Assert(false, "AcmStreamHeader dispose was not called");
        Dispose(false);
    }
    #endregion
}
