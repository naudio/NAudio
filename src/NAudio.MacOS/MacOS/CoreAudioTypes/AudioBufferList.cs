
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// A variable length array of AudioBuffer structures.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioBufferList
{
    /// <summary>
    /// The number of <see cref="AudioBuffer"/>s in the mBuffers array.
    /// </summary>
    public readonly UInt32 mNumberBuffers;
    /// <summary>
    /// A variable length array of AudioBuffers.
    /// </summary>
    public readonly AudioBuffer mBuffers; // this is a variable length array of mNumberBuffers elements

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe UInt32 GetNumberOfBuffersFromPointer(IntPtr ptr)
        => ((AudioBufferList*)ptr)->mNumberBuffers;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AudioBuffer GetAudioBufferFromPointer(IntPtr ptr, int index)
        => (&((AudioBufferList*)ptr)->mBuffers)[index];
}