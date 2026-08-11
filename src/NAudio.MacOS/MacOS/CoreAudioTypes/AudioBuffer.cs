// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// A structure to hold a buffer of audio data.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioBuffer
{
    /// <summary>
    /// The number of interleaved channels in the buffer.
    /// </summary>
    public UInt32 mNumberChannels;
    /// <summary>
    /// The number of bytes in the buffer pointed at by mData.
    /// </summary>
    public UInt32 mDataByteSize;
    /// <summary>
    /// A pointer to the buffer of audio data.
    /// </summary>
    [AllowNull]
    public IntPtr mData;

    public AudioBuffer(IntPtr data, uint dataSize, uint numberOfChannels)
    {
        mData = data;
        mDataByteSize = dataSize;
        mNumberChannels = numberOfChannels;
    }

    /// <summary>
    /// Convenience method for reading/writing data directly to the buffer.
    /// </summary>
    public unsafe Span<byte> GetSpan() => new(mData.ToPointer(), (int)mDataByteSize);
}