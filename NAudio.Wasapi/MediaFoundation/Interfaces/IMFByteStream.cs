using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    [GeneratedComInterface]
    [Guid("ad4c1b00-4bf7-422f-9175-756693d9130d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFByteStream
    {
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
        int BeginRead(IntPtr pb, int cb, IntPtr pCallback, IntPtr punkState);

        [PreserveSig]
        int EndRead(IntPtr pResult, out int pcbRead);

        [PreserveSig]
        int Write(IntPtr pb, int cb, out int pcbWritten);

        [PreserveSig]
        int BeginWrite(IntPtr pb, int cb, IntPtr pCallback, IntPtr punkState);

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
