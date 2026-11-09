// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// AudioQueueChannelAssignment <br />
/// Specifies an audio device channel to which the queue will play or from which it will record.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioQueueChannelAssignment
{
    /// <summary>On iOS, this is a port UID obtained from AVAudioSession. On macOS, this is the UID obtained from an AudioDeviceID.</summary>
    public IntPtr mDeviceUID;
    /// <summary>The 1-based index of the channel.</summary>
    public uint mChannelNumber;
}
