// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

using System.Runtime.InteropServices;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// AudioQueueBuffer <br />
/// Defines a buffer of audio data to be managed by an audio queue. <br />
/// Each audio queue has an associated set of audio queue buffers. You can request that a
/// queue allocate buffers using the AudioQueueAllocateBuffer function and dispose of them
/// using the AudioQueueFreeBuffer function.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AudioQueueBuffer
{
    /// <summary>The size of the buffer, in bytes. This size is set when the buffer is allocated and cannot be changed.</summary>
    public readonly uint mAudioDataBytesCapacity;
    /// <summary>A pointer to the audio data in the buffer. Although you can modify the contents of the buffer, you cannot modify this pointer.</summary>
    public readonly void* mAudioData;
    /// <summary>The number of bytes of valid audio data in the buffer.</summary>
    public uint mAudioDataByteSize;
    /// <summary>A value you may specify to identify the buffer when it is passed back in recording or playback callback functions.</summary>
    public void* mUserData;
    /// <summary>The maximum number of packet descriptions that can be stored in <see cref="mPacketDescriptions"/>.</summary>
    public readonly uint mPacketDescriptionCapacity;
    /// <summary>An array of AudioStreamPacketDescriptions associated with the buffer.</summary>
    public readonly AudioStreamPacketDescription* mPacketDescriptions;
    /// <summary>The number of valid packet descriptions in the buffer.</summary>
    public uint mPacketDescriptionCount;
}
