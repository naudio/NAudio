/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// A structure to hold a buffer of audio data.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioBuffer
{
    /// <summary>
    /// The number of interleaved channels in the buffer.
    /// </summary>
    public readonly UInt32 mNumberChannels;
    /// <summary>
    /// The number of bytes in the buffer pointed at by mData.
    /// </summary>
    public readonly UInt32 mDataByteSize;
    /// <summary>
    /// A pointer to the buffer of audio data.
    /// </summary>
    [AllowNull]
    public readonly IntPtr mData;

    /// <summary>
    /// Convenience method for reading/writing data directly to the buffer.
    /// </summary>
    public unsafe Span<byte> GetSpan() => new(mData.ToPointer(), (int)mDataByteSize);
}