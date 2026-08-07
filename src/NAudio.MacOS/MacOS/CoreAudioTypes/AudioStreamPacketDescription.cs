// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioStreamPacketDescription <br />
/// This structure describes the packet layout of a buffer of data where the size of
/// each packet may not be the same or where there is extraneous data between
/// packets.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioStreamPacketDescription
{
    /// <summary>
    /// The number of bytes from the start of the
    /// buffer to the beginning of the packet.
    /// </summary>
    public Int64 mStartOffset;
    /// <summary>
    /// The number of sample frames of data in the packet. 
    /// For formats with a constant number of frames per packet, this field is set to 0.
    /// </summary>
    public UInt32 mVariableFramesInPacket;
    /// <summary>
    /// The number of bytes in the packet.
    /// </summary>
    public UInt32 mDataByteSize;
}