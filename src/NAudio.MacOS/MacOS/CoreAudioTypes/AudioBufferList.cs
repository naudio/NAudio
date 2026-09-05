// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// A variable length array of AudioBuffer structures.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioBufferList
{
    /// <summary>
    /// The number of <see cref="AudioBuffer"/>s in the mBuffers array.
    /// </summary>
    public UInt32 mNumberBuffers;
    /// <summary>
    /// A variable length array of AudioBuffers.
    /// </summary>
    public AudioBuffer mBuffers; // this is a variable length array of mNumberBuffers elements

    public AudioBufferList(AudioBuffer buffer)
    {
        mNumberBuffers = 1U;
        mBuffers = buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AudioBufferList FromSingleBuffer(IntPtr data, uint dataSize, uint numberOfChannels) => new(new(data, dataSize, numberOfChannels));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint GetNumberOfBuffersFromPointer(IntPtr ptr)
        => ptr == IntPtr.Zero ? 0U : ((AudioBufferList*)ptr)->mNumberBuffers;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SetNumberOfBuffersFromPointer(IntPtr ptr, uint nBuffers)
        => ((AudioBufferList*)ptr)->mNumberBuffers = nBuffers;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe AudioBuffer GetAudioBufferFromPointer(IntPtr ptr, uint index)
        => (&((AudioBufferList*)ptr)->mBuffers)[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SetAudioBufferFromPointer(IntPtr ptr, uint index, AudioBuffer buffer)
        => (&((AudioBufferList*)ptr)->mBuffers)[index] = buffer;
}