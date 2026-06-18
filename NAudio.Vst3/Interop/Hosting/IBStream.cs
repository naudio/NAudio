using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Stream seek modes used by <see cref="IBStream.Seek"/> (<c>IBStream::IStreamSeekMode</c>).
/// </summary>
internal enum StreamSeekMode
{
    Set = 0,
    Cur = 1,
    End = 2,
}

/// <summary>
/// VST 3 binary stream abstraction (<c>Steinberg::IBStream</c>). Used for state save/load and
/// any host&lt;-&gt;plug-in blob transfer.
/// </summary>
/// <remarks>
/// Defined in <c>pluginterfaces/base/ibstream.h</c>. Host-side implementations typically wrap
/// a managed <see cref="System.IO.MemoryStream"/>.
/// </remarks>
[GeneratedComInterface]
[Guid("C3BF6EA2-3099-4752-9B6B-F9901EE33E9B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IBStream
{
    // The four "count" / "result" pointers below: three are declared optional (default nullptr) in
    // pluginterfaces/base/ibstream.h (read/write/seek); tell's pos is not officially defaulted but
    // some plug-ins still pass nullptr. All four are raw <see cref="IntPtr"/> we manually deref —
    // a source-generated `out long` would emit `ref long = ref *param` on the CCW entry and NRE on
    // null. The Read/Write/Seek shape was confirmed needed in Phase 4 (JUCE wrappers); Tell is
    // included defensively against the same failure mode.
    [PreserveSig]
    int Read(IntPtr buffer, int numBytes, IntPtr numBytesRead);

    [PreserveSig]
    int Write(IntPtr buffer, int numBytes, IntPtr numBytesWritten);

    [PreserveSig]
    int Seek(long pos, StreamSeekMode mode, IntPtr result);

    [PreserveSig]
    int Tell(IntPtr pos);
}

/// <summary>
/// Optional extension on top of <see cref="IBStream"/> that lets a plug-in query and resize the
/// underlying stream (<c>Steinberg::ISizeableStream</c>, also in <c>pluginterfaces/base/ibstream.h</c>).
/// </summary>
/// <remarks>
/// Hosts that supply a stream the plug-in can QI for this extension allow the plug-in to size
/// its read buffer in one shot rather than walking the stream to find the end. Some plug-ins
/// (notably ValhallaSupermassive's <c>setState</c>) dereference the QI result without checking
/// for failure, so a host CCW returning <see cref="TResultCodes.NoInterface"/> from QI can fault
/// — implementing the extension as a thin wrapper over the backing buffer's length is the safe
/// choice even when the plug-in could function without it.
/// </remarks>
[GeneratedComInterface]
[Guid("04F9549E-E02F-4E6E-87E8-6A8747F4E17F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ISizeableStream
{
    [PreserveSig]
    int GetStreamSize(out long size);

    [PreserveSig]
    int SetStreamSize(long size);
}
