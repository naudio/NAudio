using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave;

/// <summary>
/// WaveHeader interop structure (WAVEHDR)
/// http://msdn.microsoft.com/en-us/library/dd743837%28VS.85%29.aspx
/// </summary>
/// <remarks>
/// A struct rather than a class, and always passed to winmm as an IntPtr to a block of
/// unmanaged memory. A WAVEHDR outlives the call that submits it — the driver keeps the
/// address queued and writes dwFlags/dwBytesRecorded into it from its own thread — so it
/// needs a stable address the marshaller can't relocate or copy. Passing a
/// [StructLayout] class by value happened to work on CoreCLR, which pins blittable class
/// arguments in place, but NativeAOT copies them into a per-call temporary instead, which
/// silently discarded every update the driver made.
/// See https://github.com/naudio/NAudio/issues/1425.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct WaveHeader
{
    /// <summary>pointer to locked data buffer (lpData)</summary>
    public IntPtr dataBuffer;
    /// <summary>length of data buffer (dwBufferLength)</summary>
    public int bufferLength;
    /// <summary>used for input only (dwBytesRecorded)</summary>
    public int bytesRecorded;
    /// <summary>for client's use (dwUser)</summary>
    public IntPtr userData;
    /// <summary>assorted flags (dwFlags)</summary>
    public WaveHeaderFlags flags;
    /// <summary>loop control counter (dwLoops)</summary>
    public int loops;
    /// <summary>PWaveHdr, reserved for driver (lpNext)</summary>
    public IntPtr next;
    /// <summary>reserved for driver</summary>
    public IntPtr reserved;
}
