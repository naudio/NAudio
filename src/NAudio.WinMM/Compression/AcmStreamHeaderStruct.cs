using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression;

/// <summary>
/// Interop structure for ACM stream headers.
/// ACMSTREAMHEADER 
/// http://msdn.microsoft.com/en-us/library/dd742926%28VS.85%29.aspx
/// </summary>
/// <remarks>
/// A struct rather than a [StructLayout] class, and always reached through a caller-owned
/// unmanaged block - see <see cref="AcmStreamHeader"/>. acmStreamPrepareHeader writes the
/// codec's private state into the reserved area below, which acmStreamConvert and
/// acmStreamUnprepareHeader then read back. Passing the header as a class only preserved
/// that because CoreCLR pins blittable class arguments in place; NativeAOT marshals them
/// through a per-call temporary that round-trips the declared fields alone, so the reserved
/// area came back zeroed and conversion failed. See
/// https://github.com/naudio/NAudio/issues/1425.
/// </remarks>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 128)] // explicit size to make it work for x64
internal struct AcmStreamHeaderStruct
{
    public int cbStruct;
    public AcmStreamHeaderStatusFlags fdwStatus;
    public IntPtr userData;
    public IntPtr sourceBufferPointer;
    public int sourceBufferLength;
    public int sourceBufferLengthUsed;
    public IntPtr sourceUserData;
    public IntPtr destBufferPointer;
    public int destBufferLength;
    public int destBufferLengthUsed;
    public IntPtr destUserData;

    // 10 reserved values follow this, we don't need to declare them
    // since we have set the struct size explicitly and don't
    // need to access them in client code (thanks Brian). The codec
    // does use them, which is why the block has to be stable.
}
