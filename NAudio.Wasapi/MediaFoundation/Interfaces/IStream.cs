using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    /// Source-generated COM projection of <c>IStream</c> (<c>0000000C-0000-0000-C000-000000000046</c>),
    /// implemented by <c>NAudio.Wave.ComStream</c> and handed to native MF via
    /// <c>MFCreateMFByteStreamOnStream</c>.
    /// </summary>
    /// <remarks>
    /// We declare our own source-generated <c>IStream</c> rather than reusing
    /// <see cref="System.Runtime.InteropServices.ComTypes.IStream"/> because the latter is a
    /// classic <c>[ComImport]</c> interface. Under <c>BuiltInComInteropSupport=false</c>, classic
    /// COM marshalling for byte[] / string / STATSTG fields is unavailable; this interface uses
    /// raw <see cref="IntPtr"/> for buffers and STATSTG so the source-generated CCW dispatches
    /// directly without runtime fallback.
    ///
    /// All methods return <see cref="int"/> HRESULT (no PreserveSig=false / void).
    ///
    /// **CCW handoff hazard (Phase 2f H3):** ComWrappers CCWs return distinct vtable pointers per
    /// interface, including for single-interface <c>[GeneratedComClass]</c> types. Callers that
    /// hand a <c>ComStream</c> CCW pointer to native typed as <c>IStream*</c> MUST
    /// <see cref="Marshal.QueryInterface(IntPtr, in Guid, out IntPtr)"/> for <c>IID_IStream</c>
    /// first. The IUnknown returned by
    /// <c>ComWrappers.GetOrCreateComInterfaceForObject</c> is NOT the IStream vtable — passing
    /// it directly will AV on first invocation. See
    /// <c>NAudio.Wasapi/Dmo/MediaObject.cs:ProcessInput</c> for the canonical pattern.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("0000000C-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IStream
    {
        // ISequentialStream methods (flattened vtable)

        [PreserveSig]
        int Read(IntPtr pv, int cb, IntPtr pcbRead);

        [PreserveSig]
        int Write(IntPtr pv, int cb, IntPtr pcbWritten);

        // IStream own methods

        [PreserveSig]
        int Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition);

        [PreserveSig]
        int SetSize(long libNewSize);

        [PreserveSig]
        int CopyTo(IntPtr pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten);

        [PreserveSig]
        int Commit(int grfCommitFlags);

        [PreserveSig]
        int Revert();

        [PreserveSig]
        int LockRegion(long libOffset, long cb, int dwLockType);

        [PreserveSig]
        int UnlockRegion(long libOffset, long cb, int dwLockType);

        [PreserveSig]
        int Stat(IntPtr pstatstg, int grfStatFlag);

        [PreserveSig]
        int Clone(out IntPtr ppstm);
    }

    /// <summary>
    /// Binary mirror of <see cref="System.Runtime.InteropServices.ComTypes.STATSTG"/> with
    /// <see cref="IntPtr"/> for the <c>pwcsName</c> field, so the struct is fully blittable
    /// and source-generated marshalling can copy it without LPWStr fallback. ComStream writes
    /// <see cref="IntPtr.Zero"/> for the name (no storage name).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct StorageStat
    {
        public IntPtr pwcsName; // LPWSTR — IntPtr.Zero for unnamed
        public int type; // STGTY_STREAM = 2
        public long cbSize;
        public long mtime; // FILETIME
        public long ctime; // FILETIME
        public long atime; // FILETIME
        public int grfMode;
        public int grfLocksSupported;
        public Guid clsid;
        public int grfStateBits;
        public int reserved;
    }
}
