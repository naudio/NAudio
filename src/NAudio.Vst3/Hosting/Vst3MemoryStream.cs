using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side managed implementation of <see cref="IBStream"/> (plus the
/// <see cref="ISizeableStream"/> and <see cref="IStreamAttributes"/> extensions) backed by a
/// <see cref="MemoryStream"/>. Used for state save / load and for shuttling the component-state
/// blob into <c>IEditController::setComponentState</c>.
/// </summary>
/// <remarks>
/// <para>
/// Source-generated CCW via <c>[GeneratedComClass]</c>. The plug-in calls into the stream the
/// same way it would call into the SDK's <c>Steinberg::MemoryStream</c>: read/write are advisory
/// (the plug-in chooses the size), seek supports the three standard modes, tell returns the
/// current position.
/// </para>
/// <para>
/// The optional out-parameter pointers on <see cref="IBStream.Read"/>, <see cref="IBStream.Write"/>,
/// and <see cref="IBStream.Seek"/> may be null — the SDK defines them as defaulted-to-nullptr in
/// C++ and JUCE's wrapper exercises that path. We manually deref only when non-null.
/// </para>
/// <para>
/// Lifetime: keep the managed instance alive (rooted on the caller's stack or field) for as
/// long as the plug-in might still be using the native pointer. The CCW is released when no
/// native references remain. The lazily-created <see cref="IStreamAttributes.GetAttributes"/>
/// list is held by one native ref and released from the finalizer.
/// </para>
/// </remarks>
[GeneratedComClass]
internal sealed unsafe partial class Vst3MemoryStream : IBStream, ISizeableStream, IStreamAttributes, IDisposable
{
    private readonly MemoryStream _stream;
    private Vst3HostAttributeList? _attributes;
    private IntPtr _attributesPtr; // AddRef'd once on first GetAttributes; released in finalizer

    public Vst3MemoryStream() => _stream = new MemoryStream();

    public Vst3MemoryStream(byte[] initialBytes)
    {
        _stream = new MemoryStream();
        _stream.Write(initialBytes, 0, initialBytes.Length);
        _stream.Position = 0;
    }

    /// <summary>Snapshot of the bytes currently written to the stream.</summary>
    public byte[] ToArray() => _stream.ToArray();

    public int Read(IntPtr buffer, int numBytes, IntPtr numBytesRead)
    {
        if (buffer == IntPtr.Zero || numBytes < 0)
        {
            if (numBytesRead != IntPtr.Zero) *(int*)numBytesRead = 0;
            return TResultCodes.InvalidArgument;
        }
        var dst = new Span<byte>((void*)buffer, numBytes);
        var read = _stream.Read(dst);
        // If the plug-in over-reads (asks for more than is available), zero-fill the tail of
        // its buffer rather than leaving uninitialised memory there. Defensive: a plug-in that
        // honours numBytesRead won't notice, and a plug-in that dereferences past it sees zeros
        // instead of stack/heap garbage. numBytesRead still reports the actual count.
        if (read < numBytes)
        {
            dst[read..].Clear();
        }
        if (numBytesRead != IntPtr.Zero) *(int*)numBytesRead = read;
        return TResultCodes.Ok;
    }

    public int Write(IntPtr buffer, int numBytes, IntPtr numBytesWritten)
    {
        if (buffer == IntPtr.Zero || numBytes < 0)
        {
            if (numBytesWritten != IntPtr.Zero) *(int*)numBytesWritten = 0;
            return TResultCodes.InvalidArgument;
        }
        var src = new ReadOnlySpan<byte>((void*)buffer, numBytes);
        _stream.Write(src);
        if (numBytesWritten != IntPtr.Zero) *(int*)numBytesWritten = numBytes;
        return TResultCodes.Ok;
    }

    public int Seek(long pos, StreamSeekMode mode, IntPtr result)
    {
        var origin = mode switch
        {
            StreamSeekMode.Set => SeekOrigin.Begin,
            StreamSeekMode.Cur => SeekOrigin.Current,
            StreamSeekMode.End => SeekOrigin.End,
            _ => SeekOrigin.Begin,
        };
        try
        {
            var newPos = _stream.Seek(pos, origin);
            if (result != IntPtr.Zero) *(long*)result = newPos;
            return TResultCodes.Ok;
        }
        catch
        {
            if (result != IntPtr.Zero) *(long*)result = _stream.Position;
            return TResultCodes.InvalidArgument;
        }
    }

    public int Tell(IntPtr pos)
    {
        if (pos != IntPtr.Zero)
        {
            *(long*)pos = _stream.Position;
        }
        return TResultCodes.Ok;
    }

    public int GetStreamSize(out long size)
    {
        size = _stream.Length;
        return TResultCodes.Ok;
    }

    public int SetStreamSize(long size)
    {
        if (size < 0)
        {
            return TResultCodes.InvalidArgument;
        }
        try
        {
            _stream.SetLength(size);
            return TResultCodes.Ok;
        }
        catch
        {
            return TResultCodes.InternalError;
        }
    }

    public int GetFileName(IntPtr name)
    {
        // We don't have a filename to advertise. Null-terminate the caller's String128 buffer and
        // signal "no value" — matches how the SDK reference BStream handles an unset filename.
        if (name != IntPtr.Zero)
        {
            *(char*)name = '\0';
        }
        return TResultCodes.False;
    }

    public IntPtr GetAttributes()
    {
        if (_attributesPtr == IntPtr.Zero)
        {
            _attributes = new Vst3HostAttributeList();
            var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
                _attributes, CreateComInterfaceFlags.None);
            try
            {
                var iid = Vst3StandardInterfaceIds.IAttributeList;
                var hr = Marshal.QueryInterface(unk, in iid, out _attributesPtr);
                if (hr != 0 || _attributesPtr == IntPtr.Zero)
                {
                    _attributesPtr = IntPtr.Zero;
                    return IntPtr.Zero;
                }
            }
            finally
            {
                Marshal.Release(unk);
            }
        }
        return _attributesPtr;
    }

    public void Dispose()
    {
        ReleaseAttributesPtr();
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }

    // Fallback if a caller forgets to dispose. The MemoryStream buffer is managed (a byte[]) so
    // GC handles it; we only chase the unmanaged CCW ref we own on _attributesPtr.
    ~Vst3MemoryStream() => ReleaseAttributesPtr();

    private void ReleaseAttributesPtr()
    {
        if (_attributesPtr != IntPtr.Zero)
        {
            Marshal.Release(_attributesPtr);
            _attributesPtr = IntPtr.Zero;
        }
        _attributes = null;
    }
}
