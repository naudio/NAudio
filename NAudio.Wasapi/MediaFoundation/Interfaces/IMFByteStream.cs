using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    /// Represents a byte stream from some data source, which might be a local file, a network file, or some other source.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMFByteStream (mfobjects.h).
    /// https://learn.microsoft.com/windows/win32/api/mfobjects/nn-mfobjects-imfbytestream
    /// </remarks>
    [GeneratedComInterface]
    [Guid("ad4c1b00-4bf7-422f-9175-756693d9130d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFByteStream
    {

        /// <summary>
        ///     This bit indicates that the byte stream can be read from.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_IS_READABLE = 0x00000001;
        /// <summary>
        ///     This bit indicates that the byte stream can be written to.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_IS_WRITABLE = 0x00000002;
        /// <summary>
        ///     This bit indicates that the byte stream can be sought.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_IS_SEEKABLE = 0x00000004;
        /// <summary>
        ///     This bit indicates that the byte stream is based on a remote (network)
        ///     drive.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_IS_REMOTE = 0x00000008;
        /// <summary>
        ///     This bit indicates that the byte stream is a directory.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_IS_DIRECTORY = 0x00000080;
        /// <summary>
        ///     This bit indicates that a read operation following a seek operation
        ///     may take a long time to complete depending on whether the byte stream
        ///     has to wait for the data to become available from a remote server or not.
        ///     This capability is exposed by byte streams that pre-cache the data
        ///     sequentially from a remote server. This bit goes away when the data is
        ///     fully cached.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_HAS_SLOW_SEEK = 0x00000100;
        /// <summary>
        ///     This bit indicates that the byte stream is downloading the data
        ///     in the background to a local cache. In this case a read operation may
        ///     take longer to complete depending on whether the byte stream has the
        ///     data in cache or not. If MFBYTESTREAM_HAS_SLOW_SEEK is not present,
        ///     then the byte stream can pre-cache the data sparsely instead of
        ///     sequentially, and a read operation that misses the local cache
        ///     will cause a reconnection to the remote server instead of waiting
        ///     for the sequential download to catch up. This bit goes away when the
        ///     data is fully cached.
        /// </summary>
        const int MFBYTESTREAM_CAPABILITY_IS_PARTIALLY_DOWNLOADED = 0x00000200;
        /// <summary>
        ///     This bit indicates that the byte stream data is opened for write by
        ///     another thread or process.  This means that the length of the
        ///     bytestream could change and special care must be taken to handle
        ///     situations where only part of a file may be written.  Only byte
        ///     stream plugins with the attribute MF_BYTESTREAMPLUGIN_ACCEPTS_SHARE_WRITE
        ///     will be considered by the source resolver for byte streams that have this
        ///     characteristic.
        /// </summary>
        [SupportedOSPlatform("windows6.1")] // Windows 7
        const int MFBYTESTREAM_CAPABILITY_SHARE_WRITE = 0x00000400;
        /// <summary>
        ///     This bit should be set if the byte stream is not currently
        ///     using the network to receive the content.  Networking hardware
        ///     may enter a power saving state when this bit is set.
        /// </summary>
        [SupportedOSPlatform("windows6.3")] // Windows 8.1
        const int MFBYTESTREAM_CAPABILITY_DOES_NOT_USE_NETWORK = 0x00000800;

        [PreserveSig]
        int GetCapabilities(out int pdwCapabilities);

        [PreserveSig]
        int GetLength(out long pqwLength);

        [PreserveSig]
        int SetLength(long qwLength);

        [PreserveSig]
        int GetCurrentPosition(out long pqwPosition);

        [PreserveSig]
        int SetCurrentPosition(long qwPosition);

        [PreserveSig]
        int IsEndOfStream(out int pfEndOfStream);

        [PreserveSig]
        int Read(IntPtr pb, int cb, out int pcbRead);

        [PreserveSig]
        int BeginRead(IntPtr pb, int cb, IMFAsyncCallback pCallback, IntPtr punkState);

        [PreserveSig]
        int EndRead(IntPtr pResult, out int pcbRead);

        [PreserveSig]
        int Write(IntPtr pb, int cb, out int pcbWritten);

        [PreserveSig]
        int BeginWrite(IntPtr pb, int cb, IMFAsyncCallback pCallback, IntPtr punkState);

        [PreserveSig]
        int EndWrite(IntPtr pResult, out int pcbWritten);

        [PreserveSig]
        int Seek(int seekOrigin, long llSeekOffset, int dwSeekFlags, out long pqwCurrentPosition);

        [PreserveSig]
        int Flush();

        [PreserveSig]
        int Close();
    }
}
